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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Net;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Diagnostics
{
    [SkipCodeCoverage("For testing only")]
    [Service]
    public class DiagnosticsCommands
    {
        public DiagnosticsCommands(IProjectWorkspace workspace)
        {
            this.GenerateHtmlPage = new GenerateHtmlPageCommand(workspace);
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> GenerateHtmlPage { get; }

        //---------------------------------------------------------------------
        // Command classes.
        //---------------------------------------------------------------------

        /// <summary>
        /// Generate a HTML page that contains iap-rdp:// links for all
        /// Windows VMs in a project.
        /// </summary>
        private class GenerateHtmlPageCommand : MenuCommandBase<IProjectModelNode>
        {
            private readonly IProjectWorkspace workspace;

            public GenerateHtmlPageCommand(IProjectWorkspace workspace)
                : base("Generate &HTML page")
            {
                this.workspace = workspace;
            }

            //-----------------------------------------------------------------
            // Overrides.
            //-----------------------------------------------------------------

            protected override bool IsAvailable(IProjectModelNode context)
            {
                return context is IProjectModelProjectNode;
            }

            protected override bool IsEnabled(IProjectModelNode context)
            {
                return true;
            }

            public override async Task ExecuteAsync(IProjectModelNode context)
            {
                Debug.Assert(context is IProjectModelProjectNode);
                var projectNode = (IProjectModelProjectNode)context;

                var buffer = new StringBuilder();
                buffer.AppendLine("<html>");
                buffer.AppendLine("<head>");
                buffer.AppendLine(@"<style>
                    body {
                        font-family: Arial, Helvetica
                    }
                    label {
                        font-size: 9px;
                    }
                    .input-wrapper {
                        display: block;
                        text-align: left;
                        padding: 3px;
                    }
                    .button {
                        background: none !important;
                        border: none;
                        padding: 0 !important;
                        /*optional*/
                        font-family: arial, sans-serif;
                        color: #069;
                        text-decoration: underline;
                        cursor: pointer;
                    }
                    </style>");
                buffer.AppendLine("</head>");
                buffer.AppendLine("<body>");

                buffer.Append($"<h1>{HttpUtility.HtmlEncode(projectNode.Project.ProjectId)}</h1>");

                buffer.AppendLine("<form method='GET'>");

                WriteTextbox("Username");
                WriteTextbox("Domain");
                WriteTextbox("RdpPort");
                WriteTextbox("CredentialCallbackUrl");
                WriteCombobox<RdpConnectionBarState>("ConnectionBar");
                WriteCombobox<RdpColorDepth>("ColorDepth");
                WriteCombobox<RdpAudioPlayback>("AudioMode");
                WriteCombobox<RdpRedirectClipboard>("RedirectClipboard");
                WriteCombobox<RdpRedirectPrinter>("RdpRedirectPrinter");
                WriteCombobox<RdpRedirectSmartCard>("RdpRedirectSmartCard");
                WriteCombobox<RdpRedirectPort>("RdpRedirectPort");
                WriteCombobox<RdpRedirectDrive>("RdpRedirectDrive");
                WriteCombobox<RdpRedirectDevice>("RdpRedirectDevice");
                WriteCombobox<RdpHookWindowsKeys>("RdpHookWindowsKeys");
                WriteCombobox<SessionTransportType>("TransportType");
                WriteCombobox<RdpCredentialGenerationBehavior>("CredentialGenerationBehavior");

                var zones = await this.workspace
                    .GetZoneNodesAsync(
                        projectNode.Project,
                        false,
                        CancellationToken.None)
                    .ConfigureAwait(true);

                foreach (var zone in zones)
                {
                    buffer.Append($"<p>{HttpUtility.HtmlEncode(zone.Zone.Name)}</p>");
                    buffer.Append($"<ul>");

                    foreach (var vmNode in zone.Instances.Where(i => i.IsWindowsInstance()))
                    {
                        WriteInstance(vmNode);
                    }

                    buffer.Append($"</ul>");
                }

                buffer.AppendLine("</form>");
                buffer.AppendLine("</body></html>");

                var tempFile = Path.GetTempFileName() + ".html";
                File.WriteAllText(tempFile, buffer.ToString());

                Browser.Default.Navigate(tempFile);

                void WriteInstance(IProjectModelInstanceNode node)
                {
                    var name = HttpUtility.HtmlEncode(node.Instance.Name);
                    var url = new IapRdpUrl(node.Instance, new NameValueCollection());

                    buffer.AppendLine("<li>");
                    buffer.AppendLine($"<input type='submit' formaction='{url}' value='{name}' class='button' /> ");
                    buffer.AppendLine($"<input type='submit' formaction='data:{url}' value='(URL)' class='button' />");
                    if (node.IsRunning)
                    {
                        buffer.AppendLine(" &#x25ba;");
                    }
                    else
                    {
                        buffer.AppendLine(" &#x25a0;");
                    }
                    buffer.AppendLine("</li>");
                }


                void WriteTextbox(string fieldName)
                {
                    buffer.AppendLine("<div class='input-wrapper'>");
                    buffer.AppendLine($"<label for='name'>{fieldName}</label>");
                    buffer.AppendLine("<br />");
                    buffer.AppendLine($"<input name='{fieldName}' size='40' />");
                    buffer.AppendLine("</div>");
                }

                void WriteCombobox<TEnum>(string fieldName) where TEnum : struct
                {
                    buffer.AppendLine("<div class='input-wrapper'>");
                    buffer.AppendLine($"<label for='{fieldName}'>{typeof(TEnum).Name}</label>");
                    buffer.AppendLine("<br />");
                    buffer.AppendLine($"<select name='{fieldName}'>");
                    buffer.AppendLine($"<option value=''></option>");

                    foreach (var name in Enum
                        .GetNames(typeof(TEnum))
                        .Where(n => n != "_Default"))
                    {
                        var value = (TEnum)Enum.Parse(typeof(TEnum), name);
                        buffer.AppendLine($"<option value='{(int)(object)value}'>{name}</option>");
                    }

                    buffer.AppendLine("</select>");
                    buffer.AppendLine("</div>");
                }
            }
        }
    }
}