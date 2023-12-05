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

using Google.Solutions.IapDesktop.Application.Host.Diagnostics;
using Google.Solutions.Platform.Net;
using System.Net;

namespace Google.Solutions.IapDesktop.Application.Client
{
    public class BugReportClient
    {
        public const string BaseUrl = "https://issuetracker.google.com";

        private void ReportBug(int component, int template, BugReport report)
        {
            var body = "NOTE: This issue report will be visible to the public. Make sure you don't include any confidential information.\n\n" +
                       "You can help us fix this issue by answering a few questions:\n\n" +
                       "+ What are the steps to reproduce this issue?\n\n" +
                       "+ What's the expected behavior?\n\n" +
                       "+ What's the observed behavior?\n\n" +
                       "+ Does the issue occur every time or only occasionally?\n\n" +
                       "\n" +
                       "```\n" + report + "\n```";

            Browser.Default.Navigate(
                $"{BaseUrl}/issues/new?component={component}&template={template}&description={WebUtility.UrlEncode(body)}&format=MARKDOWN");
        }

        public void ReportPrivateBug(BugReport report)
            => ReportBug(953762, 1561579, report);

        public void ReportBug(BugReport report)
            => ReportBug(1172073, 1670021, report);
    }
}
