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
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Windows
{
    public class HtmlPageGenerator
    {
        public HtmlPageGenerator(IServiceProvider serviceProvider)
        {
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();
            projectExplorer.ContextMenuCommands.AddCommand(
                new GenerateHtmlPageCommand());
        }

        private class GenerateHtmlPageCommand : ICommand<IProjectExplorerNode>
        {
            public string Text => "Generate HTML page";
            public Image Image => null;
            public Keys ShortcutKeys => Keys.None;

            public void Execute(IProjectExplorerNode context)
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
                        buffer.Append($"<li>");
                        buffer.Append($"<a href='{new IapRdpUrl(vmNode.Reference, vmNode.CreateConnectionSettings())}'>");
                        buffer.Append($"{HttpUtility.HtmlEncode(vmNode.InstanceName)}</a>");
                        buffer.Append($"</li>");
                    }

                    buffer.Append($"</ul>");
                }

                buffer.Append("</body></html>");

                var tempFile = Path.GetTempFileName() + ".html";
                File.WriteAllText(tempFile, buffer.ToString());

                Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    Verb = "open",
                    FileName = tempFile
                });

            }

            public CommandState QueryState(IProjectExplorerNode context)
            {
                return context is IProjectExplorerProjectNode
                    ? CommandState.Enabled
                    : CommandState.Unavailable;
            }
        }
    }
}
