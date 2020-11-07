//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Diagnostics;
using System;
using System.Diagnostics;
using System.Net;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public class EmailAdapter
    {
        private const string GroupAlias = "iap-desktop@google.com";

        public void SendFeedback()
        {
            var version = typeof(GithubAdapter).Assembly.GetName().Version;
            var body =
                "Have feedback? We'd love to hear it, but please don't share " +
                "sensitive information.\n" +
                "\n\n\n\n\n" +
                $"I am currently using IAP Desktop {version} " +
                $"and .NET version {ClrVersion.Version} " +
                $"on {Environment.OSVersion}";
            var url =
                $"mailto:{GroupAlias}?" +
                $"subject={WebUtility.UrlEncode("IAP Desktop feedback")}&" +
                $"body={WebUtility.UrlEncode(body)}";

            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = url
            }))
            { };
        }
    }
}
