//
// Copyright 2023 Google LLC
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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.Mvvm.Binding;
using System.Collections.Generic;
using System.Text;

namespace Google.Solutions.IapDesktop.Application.Views.ReleaseNotes
{
    public class ReleaseNotesViewModel : ViewModelBase
    {
        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public ICollection<IGitHubRelease> Releases { get; set; }

        //---------------------------------------------------------------------
        // "Output" properties.
        //---------------------------------------------------------------------

        public string MarkdownSummary
        {
            get
            {
                //
                // Create a summary of all releases.
                //
                var summary = new StringBuilder();
                foreach (var release in this.Releases.EnsureNotNull())
                {
                    summary.AppendFormat("# Release {0}", release.TagVersion);
                    summary.AppendLine();
                    summary.Append(release.Description);
                    summary.AppendLine();
                    summary.AppendFormat("[Details]({0})", release.DetailsUrl);
                    summary.AppendLine();
                }

                return summary.ToString();
            }
        }
    }
}
