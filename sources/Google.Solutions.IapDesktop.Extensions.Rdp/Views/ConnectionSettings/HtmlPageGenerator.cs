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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Views.ConnectionSettings
{
    [Service(ServiceLifetime.Singleton)]
    public class HtmlPageGenerator
    {
        private readonly IRdpSettingsService settingsService;

        public HtmlPageGenerator(IServiceProvider serviceProvider)
        {
#if DEBUG
            this.settingsService = serviceProvider.GetService<IRdpSettingsService>();

            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Generate HTML page",
                    context => context is IProjectExplorerProjectNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => GenerateHtmlPage((IProjectExplorerProjectNode)context)));
#endif
        }

        private void GenerateHtmlPage(IProjectExplorerProjectNode context)
        {
            Debug.Assert(context is IProjectExplorerProjectNode);
            var projectNode = (IProjectExplorerProjectNode)context;

            var buffer = new StringBuilder();
            buffer.Append("<html><body>");

            buffer.Append($"<h1>{HttpUtility.HtmlEncode(projectNode.ProjectId)}</h1>");

            foreach (var zoneNode in projectNode.Zones)
            {
                buffer.Append($"<h2>{HttpUtility.HtmlEncode(zoneNode.ZoneId)}</h2>");

                buffer.Append($"<ul>");

                foreach (var vmNode in zoneNode.Instances.Cast<VmInstanceNode>())
                {
                    var settings = (RdpInstanceSettings)this.settingsService
                        .GetConnectionSettings(vmNode);

                    buffer.Append($"<li>");
                    buffer.Append($"<a href='{new IapRdpUrl(vmNode.Reference, settings.ToUrlQuery())}'>");
                    buffer.Append($"{HttpUtility.HtmlEncode(vmNode.InstanceName)}</a>");
                    buffer.Append($"</li>");
                }

                buffer.Append($"</ul>");
            }

            buffer.Append("</body></html>");

            var tempFile = Path.GetTempFileName() + ".html";
            File.WriteAllText(tempFile, buffer.ToString());

            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = tempFile
            }))
            { }
        }
    }
}
