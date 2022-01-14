//
// Copyright 2019 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Apis.Auth.OAuth2;
using Google.Apis.Requests;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GcsObject = Google.Apis.Storage.v1.Data.Object;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters
{
    public interface IStorageAdapter
    {
        Task<Stream> DownloadObjectToMemoryAsync(
            StorageObjectLocator locator,
            CancellationToken cancellationToken);

        Task<IEnumerable<Bucket>> ListBucketsAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<GcsObject>> ListObjectsAsync(
            string bucket,
            string prefix,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IStorageAdapter))]
    public class StorageAdapter : IStorageAdapter
    {
        private const string MtlsBaseUri = "https://storage.mtls.googleapis.com/storage/v1/";

        private readonly StorageService service;

        public bool IsDeviceCertiticateAuthenticationEnabled
            => this.service.IsMtlsEnabled() && this.service.IsClientCertificateProvided();

        public StorageAdapter(
            ICredential credential,
            IDeviceEnrollment deviceEnrollment)
        {
            this.service = new StorageService(
                ClientServiceFactory.ForMtlsEndpoint(
                    credential,
                    deviceEnrollment,
                    MtlsBaseUri));

            Debug.Assert(
                (deviceEnrollment?.Certificate != null &&
                    HttpClientHandlerExtensions.IsClientCertificateSupported)
                    == IsDeviceCertiticateAuthenticationEnabled);
        }

        public StorageAdapter(ICredential credential)
            : this(credential, null)
        {
            // This constructor should only be used for test cases
            Debug.Assert(Globals.IsTestCase);
        }

        public StorageAdapter(IAuthorizationSource authService)
            : this(
                  authService.Authorization.Credential,
                  authService.DeviceEnrollment)
        {
        }

        public StorageAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationSource>())
        {
        }

        //---------------------------------------------------------------------
        // IStorageAdapter.
        //---------------------------------------------------------------------

        public async Task<IEnumerable<Bucket>> ListBucketsAsync(
            string projectId,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(projectId))
            {
                try
                {
                    var buckets = await this.service.Buckets.List(projectId)
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                    return buckets.Items.EnsureNotNull();
                }
                catch (GoogleApiException e)
                    when (e.Error != null && (e.Error.Code == 403))
                {
                    throw new ResourceAccessDeniedException(
                        $"Permission to list buckets in project {projectId} has been denied", e);
                }
            }
        }

        public async Task<IEnumerable<GcsObject>> ListObjectsAsync(
            string bucket,
            string prefix,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(bucket))
            {
                try
                {
                    var request = this.service.Objects.List(bucket);
                    request.Prefix = prefix;

                    var objects = await new PageStreamer<
                        GcsObject,
                        ObjectsResource.ListRequest,
                        Objects,
                        string>(
                            (req, token) => req.PageToken = token,
                            response => response.NextPageToken,
                            response => response.Items)
                        .FetchAllAsync(
                            request,
                            cancellationToken)
                        .ConfigureAwait(false);

                    ApplicationTraceSources.Default.TraceVerbose("Found {0} instances", objects.Count());

                    return objects;
                }
                catch (GoogleApiException e)
                    when (e.Error != null && (e.Error.Code == 403 || e.Error.Code == 404))
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to storage bucket {bucket} has been denied", e);
                }
            }
        }

        public async Task<Stream> DownloadObjectToMemoryAsync(
            StorageObjectLocator locator,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(locator))
            {
                try
                {
                    var buffer = new MemoryStream();
                    var result = await this.service.Objects
                        .Get(locator.Bucket, locator.ObjectName)
                        .DownloadAsync(buffer)
                        .ConfigureAwait(false);

                    if (result.Exception != null)
                    {
                        throw result.Exception;
                    }

                    buffer.Seek(0, SeekOrigin.Begin);
                    return buffer;
                }
                catch (GoogleApiException e)
                    when ((e.Error != null && (e.Error.Code == 403 || e.Error.Code == 404)) ||
                          e.Message == "Not Found" ||
                          e.Message.Contains("storage.objects.get access"))
                {
                    throw new ResourceAccessDeniedException(
                        $"Access to storage bucket {locator.Bucket} has been denied", e);
                }
            }
        }
    }
}