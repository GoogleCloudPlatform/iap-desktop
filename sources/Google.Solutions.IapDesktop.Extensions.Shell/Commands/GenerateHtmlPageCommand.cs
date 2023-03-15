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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Commands
{
    [SkipCodeCoverage("For testing only")]
    [Service]
    public class GenerateHtmlPageCommand : CommandBase, IContextCommand<IProjectModelNode>
    {
        private readonly IConnectionSettingsService settingsService;
        private readonly IProjectModelService projectModelService;

        public GenerateHtmlPageCommand(
            IConnectionSettingsService settingsService, 
            IProjectModelService projectModelService)
        {
            this.settingsService = settingsService;
            this.projectModelService = projectModelService;
            this.Text = "Generate HTML page";
        }

        public CommandState QueryState(IProjectModelNode context)
        {
            return context is IProjectModelProjectNode
                ? CommandState.Enabled
                : CommandState.Unavailable;
        }

        public async Task ExecuteAsync(IProjectModelNode context)
        {
            Debug.Assert(context is IProjectModelProjectNode);
            var projectNode = (IProjectModelProjectNode)context;

            var buffer = new StringBuilder();
            buffer.Append("<html>");
            buffer.Append("<head><style>body { font-family: Arial, Helvetica }</style></head>");
            buffer.Append("<body>");

            buffer.Append($"<h1>{HttpUtility.HtmlEncode(projectNode.Project.ProjectId)}</h1>");

            var zones = await this.projectModelService
                .GetZoneNodesAsync(
                    projectNode.Project,
                    false,
                    CancellationToken.None)
                .ConfigureAwait(true);

            foreach (var zone in zones)
            {
                buffer.Append($"<h2>{HttpUtility.HtmlEncode(zone.Zone.Name)}</h2>");

                buffer.Append($"<ul>");

                foreach (var vmNode in zone.Instances.Where(i => i.IsWindowsInstance()))
                {
                    var settings = (InstanceConnectionSettings)this.settingsService
                        .GetConnectionSettings(vmNode)
                        .TypedCollection;

                    buffer.Append($"<li>");
                    buffer.Append($"<a href='{new IapRdpUrl(vmNode.Instance, settings.ToUrlQuery())}'>");
                    buffer.Append($"{HttpUtility.HtmlEncode(vmNode.Instance.Name)}</a>");
                    buffer.Append($"</li>");
                }

                buffer.Append($"</ul>");
            }

            buffer.Append("</body></html>");

            var tempFile = Path.GetTempFileName() + ".html";
            File.WriteAllText(tempFile, buffer.ToString());

            Browser.Default.Navigate(tempFile);
        }
    }
}