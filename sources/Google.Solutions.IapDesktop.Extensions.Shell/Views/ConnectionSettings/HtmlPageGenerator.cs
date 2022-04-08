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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.ConnectionSettings
{
    [SkipCodeCoverage("For testing only")]
    [Service(ServiceLifetime.Singleton)]
    public class HtmlPageGenerator
    {
        private readonly IConnectionSettingsService settingsService;
        private readonly IProjectModelService projectModelService;

        public HtmlPageGenerator(IServiceProvider serviceProvider)
        {
#if DEBUG
            this.settingsService = serviceProvider.GetService<IConnectionSettingsService>();
            this.projectModelService = serviceProvider.GetService<IProjectModelService>();

            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Generate HTML page",
                    context => context is IProjectModelProjectNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => GenerateHtmlPageAsync((IProjectModelProjectNode)context)
                        .ContinueWith(_ => { })));
#endif
        }

        private async Task GenerateHtmlPageAsync(IProjectModelProjectNode context)
        {
            Debug.Assert(context is IProjectModelProjectNode);
            var projectNode = (IProjectModelProjectNode)context;

            var buffer = new StringBuilder();
            buffer.Append("<html>");
            buffer.Append("<head><style>body { font-family: Arial, Helvetica }</style></head>");
            buffer.Append("<body>");

            buffer.Append($"<h1>{HttpUtility.HtmlEncode(projectNode.Project.ProjectId)}</h1>");

            var zones = await this.projectModelService.GetZoneNodesAsync(
                    ((IProjectModelProjectNode)context).Project,
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
