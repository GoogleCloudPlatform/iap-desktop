//
// Copyright 2022 Google LLC
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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Mvvm.Shell;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter
{
    public interface IQuarantineAdapter
    {
        Task ScanAsync(
            IntPtr owner,
            FileInfo filePath);
    }

    [Service(typeof(IQuarantineAdapter))]
    public class QuarantineAdapter : IQuarantineAdapter
    {
        private static readonly Guid quarantineClientGuid =
            new Guid("79ab36ca-bdae-4c10-86ac-a0025a9c0a2d");

        public Task ScanAsync(IntPtr owner, FileInfo filePath)
        {
            return Quarantine.ScanAsync(
                owner,
                filePath,
                Quarantine.DefaultSource,
                quarantineClientGuid);
        }
    }
}
