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
    public class BuganizerAdapter
    {
        public const string BaseUrl = "https://issuetracker.google.com";

        private void ReportBug(int component, int template, BugReport report)
        {
            var body = "NOTE: This issue report will be visible to the public. Make sure you don't include any confidential information.\n\n" + 
                       "Steps:\n" +
                       "* Step 1\n" +
                       "* Step 2\n" +
                       "* ...\n" +
                       "\n" +
                       "Expected behavior:\n" +
                       "\n" +
                       "Observed behavior:\n" +
                       "\n" +
                       "```\n" + report + "```";

            Browser.Default.Navigate(
                $"{BaseUrl}/issues/new?component={component}&template={template}&description={WebUtility.UrlEncode(body)}&format=MARKDOWN");
        }

        public void ReportPrivateBug(BugReport report)
            => ReportBug(953762, 1561579, report);

        public void ReportBug(BugReport report)
            => ReportBug(1172073, 1670021, report);
    }
}
