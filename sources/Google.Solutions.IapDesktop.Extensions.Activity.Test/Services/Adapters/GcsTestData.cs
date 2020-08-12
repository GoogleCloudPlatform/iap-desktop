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

using Google.Apis.Storage.v1;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using System.IO;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.Adapters
{
    internal static class GcsTestData
    {
        public static readonly string Bucket = TestProject.ProjectId + "-testdata";

        internal static StorageService CreateStorageService()
        {
            var service = TestProject.CreateService<StorageService>();

            // Ensure the test bucket exists.
            try
            {
                service.Buckets.Get(Bucket).Execute();
            }
            catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 404)
            {
                service.Buckets.Insert(new Apis.Storage.v1.Data.Bucket()
                {
                    Name = Bucket
                },
                TestProject.ProjectId).Execute();
            }

            return service;
        }

        internal static MemoryStream CreateTextStream(string text)
        {
            var stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();

            _ = stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        internal static void CreateObjectIfNotExist(
            StorageObjectLocator locator,
            string content)
        {
            var service = CreateStorageService();
            try
            {
                service.Objects.Insert(
                    new Apis.Storage.v1.Data.Object()
                    {
                        Name = locator.ObjectName
                    },
                    locator.Bucket,
                    CreateTextStream(content),
                    "application/octet-stream").Upload();
            }
            catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 409)
            {
                // Already exists -> ok.
            }
        }
    }
}
