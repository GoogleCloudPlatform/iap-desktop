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
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestStorageAdapter : FixtureBase
    {
        private static readonly StorageObjectLocator SampleLocator = new StorageObjectLocator(
            GcsTestData.Bucket, 
            typeof(TestStorageAdapter).Name + ".dat");
        private const string SampleData = "test data";

        [SetUp]
        public void SetUpTestBucket()
        {
            GcsTestData.CreateObjectIfNotExist(SampleLocator, SampleData);
        }

        [Test]
        public async Task WhenNoEnrollmentProvided_ThenDeviceCertiticateAuthenticationIsOff(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new StorageAdapter(await credential);
            Assert.IsFalse(adapter.IsDeviceCertiticateAuthenticationEnabled);
        }

        //---------------------------------------------------------------------
        // ListBucketsAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenListBucketsAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new StorageAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListBucketsAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenBucketExists_ThenListBucketsAsyncReturnsObject(
            [Credential(Role = PredefinedRole.StorageAdmin)] ResourceTask<ICredential> credential)
        {
            var adapter = new StorageAdapter(await credential);

            var buckets = await adapter.ListBucketsAsync(
                TestProject.ProjectId,
                CancellationToken.None);

            Assert.IsNotNull(buckets);
            CollectionAssert.Contains(
                buckets.Select(o => o.Name).ToList(), 
                GcsTestData.Bucket);
        }

        //---------------------------------------------------------------------
        // ListObjectsAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenListObjectsAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new StorageAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ListObjectsAsync(
                    GcsTestData.Bucket,
                    null,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenObjectExists_ThenListObjectsAsyncReturnsObject(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new StorageAdapter(await credential);

            var objects = await adapter.ListObjectsAsync(
                GcsTestData.Bucket,
                null,
                CancellationToken.None);

            var objectNames = objects.Select(o => o.Name).ToList();

            CollectionAssert.Contains(objectNames, SampleLocator.ObjectName);
        }

        //---------------------------------------------------------------------
        // DownloadObjectToMemoryAsync
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenDownloadObjectToMemoryAsyncThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new StorageAdapter(await credential);

            AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.DownloadObjectToMemoryAsync(
                    SampleLocator,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenObjectExists_ThenDownloadObjectToMemoryAsyncReturnsObject(
            [Credential(Role = PredefinedRole.StorageObjectViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new StorageAdapter(await credential);

            var stream = await adapter.DownloadObjectToMemoryAsync(
                SampleLocator,
                CancellationToken.None);

            Assert.AreEqual(
                SampleData,
                new StreamReader(stream).ReadToEnd());
        }
    }
}
