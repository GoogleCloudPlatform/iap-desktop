//
// Copyright 2019 Google LLC
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

using Google.Solutions.CloudIap.Plugin.Configuration;
using Google.Solutions.CloudIap.Plugin.Integration;
using Google.Solutions.CloudIap.Plugin.Util;
using RdcMan;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.CloudIap.Plugin.Gui
{
    /// <summary>
    /// Main plugin class. RDCMan auto-discovers this class based on
    /// DLL filename (it has to start with "Plugin") and the Export 
    /// attribute.
    /// </summary>
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        private const int RemoteDesktopPort = 3389;

        private static readonly TimeSpan DefaultTunnelConnectionTimeout = TimeSpan.FromSeconds(10);

        private readonly PluginConfigurationStore configurationStore;
        private readonly PluginConfiguration configuration;
        private readonly IapTunnelManager tunnelManager;
        private readonly WindowsPasswordManager windowsPasswordManager;
        private readonly LazyWithRetry<ProjectCollection> projects = new LazyWithRetry<ProjectCollection>(
            () => new ProjectCollection(ComputeEngineAdapter.Create(GcloudAccountConfiguration.ActiveAccount.Credential)));

        private Form mainForm = null;
        private MenuStrip mainMenu = null;

        public Plugin()
        {
            this.configurationStore = PluginConfigurationStore.ForCurrentWindowsUser;
            this.configuration = this.configurationStore.Configuration;
            this.tunnelManager = new IapTunnelManager(this.configuration);
            windowsPasswordManager = new WindowsPasswordManager(this.configuration);
        }

        private void PostInitialize(RdcTreeNode anyNode)
        {
            if (this.mainForm != null)
            {
                // Initialized already.
                return;
            }

            // Find the main form based on the node. It does not matter which node
            // it is, they are all part of the same TreeView anyways.
            this.mainForm = anyNode.TreeView.FindForm();
        }

        //---------------------------------------------------------------------
        // Plugin event handlers.
        //---------------------------------------------------------------------

        public void OnContextMenu(System.Windows.Forms.ContextMenuStrip contextMenuStrip, RdcTreeNode node)
        {
            PostInitialize(node);
            
            // If node refers to a node in a virtual group like "Connected Servers",
            // perform a dereference first.
            if (node is ServerRef serverRef)
            {
                node = serverRef.ServerNode;
            }

            if (node is FileGroup fileGroup)
            {
                ToolStripMenuItem loadServers = new ToolStripMenuItem(
                    $"Add GCE &instances from {node.Text}", 
                    Resources.DownloadWebSetting.WithMagentaAsTransparent());
                loadServers.Click += (sender, args) => this.OnLoadServersClick(fileGroup);

                contextMenuStrip.Items.Insert(contextMenuStrip.Items.Count - 1, loadServers);
                contextMenuStrip.Items.Insert(contextMenuStrip.Items.Count - 1, new ToolStripSeparator());
            }
            else if (node is Server server && node.Parent != null && node.Parent is Group)
            {
                ToolStripMenuItem iapConnect = new ToolStripMenuItem(
                   $"Connect server via Cloud &IAP",
                   Resources.RemoteDesktop);
                iapConnect.Click += (sender, args) => OnIapConnectClick(server, false);
                iapConnect.Enabled = !server.IsConnected;

                ToolStripMenuItem iapConnectAs = new ToolStripMenuItem(
                   $"Connect server via Cloud &IAP as...",
                   Resources.RemoteDesktop);
                iapConnectAs.Click += (sender, args) => OnIapConnectClick(server, true);
                iapConnectAs.Enabled = !server.IsConnected;

                ToolStripMenuItem resetPassword = new ToolStripMenuItem(
                   $"Generate &Windows logon credentials...",
                   Resources.ChangePassword.WithMagentaAsTransparent());
                resetPassword.Click += (sender, args) => OnResetPasswordClick(server);

                ToolStripMenuItem showSerialPortOutput = new ToolStripMenuItem(
                   $"Show serial port &output...",
                   Resources.ActionLog.WithMagentaAsTransparent());
                showSerialPortOutput.Click += (sender, args) => OnShowSerialPortOutputClick(server);

                ToolStripMenuItem openCloudConsole = new ToolStripMenuItem(
                   $"Open in Cloud Consol&e...",
                   Resources.CloudConsole.ToBitmap());
                openCloudConsole.Click += (sender, args) => OnOpenCloudConsoleClick(server);

                ToolStripMenuItem openStackdriverLogs = new ToolStripMenuItem(
                   $"Show &Stackdriver logs...",
                   Resources.StackdriverLogging.ToBitmap());
                openStackdriverLogs.Click += (sender, args) => OnOpenStackdriverLogsClick(server);

                // Add custom context menu items.
                int index = 2;
                contextMenuStrip.Items.Insert(index++, new ToolStripSeparator());
                contextMenuStrip.Items.Insert(index++, iapConnect);
                contextMenuStrip.Items.Insert(index++, iapConnectAs);
                contextMenuStrip.Items.Insert(index++, resetPassword);
                contextMenuStrip.Items.Insert(index++, showSerialPortOutput);
                contextMenuStrip.Items.Insert(index++, openCloudConsole);
                contextMenuStrip.Items.Insert(index++, openStackdriverLogs);
                contextMenuStrip.Items.Insert(index++, new ToolStripSeparator());

                // Tweak existing context menu items.
                TweakMenuItemsForSelectedServer(
                    contextMenuStrip.Items
                        .Cast<object>()
                        .Where(i => i is ToolStripMenuItem)
                        .Cast<ToolStripMenuItem>(), 
                    server);
            }
        }

        private void TweakMenuItemsForSelectedServer(IEnumerable<ToolStripMenuItem> menuItems, Server server)
        {
            var connectedViaIap = this.tunnelManager.IsConnected(
                new IapTunnelEndpoint(
                    new VmInstanceReference(
                        server.FileGroup.Text,
                        server.Parent.Text,
                        server.DisplayName),
                    RemoteDesktopPort));

            // SessionConnect, SessionConnectAs, and SessionReconnect do not make sense if we are 
            // connected via IAP as they would cause a direct connection attempt.
            menuItems.First(i => i.Name == "SessionConnect").Enabled &= !connectedViaIap;
            menuItems.First(i => i.Name == "SessionConnectAs").Enabled &= !connectedViaIap;
            menuItems.First(i => i.Name == "SessionReconnect").Enabled &= !connectedViaIap;

            // SessionLogOff and SessionListSessions do not work over a tunnel. Clicking these
            // menu items would cause the action to be performed on the host machine.
            menuItems.First(i => i.Name == "SessionLogOff").Enabled &= !connectedViaIap;
            menuItems.First(i => i.Name == "SessionListSessions").Enabled &= !connectedViaIap;
        }


        public void OnDockServer(ServerBase server)
        {
        }

        public void OnUndockServer(IUndockedServerForm form)
        {
        }

        public void PostLoad(IPluginContext context)
        {
            this.mainMenu = context.MainForm.MainMenuStrip;

            // Add Tools menu items
            var configMenuItem = new ToolStripMenuItem("Cloud IAP &Settings...");
            configMenuItem.Click += (sender, args) =>
            {
                if (ConfigurationDialog.ShowDialog(this.mainForm, this.configuration)
                    == DialogResult.OK)
                {
                    // Write back updated configuration
                    this.configurationStore.Configuration = this.configuration;
                }
            };

            var tunnelsMenuItem = new ToolStripMenuItem("Cloud IAP Tunnels...");
            tunnelsMenuItem.Click += (sender, args) =>
            {
                TunnelsWindow.ShowDialog(this.mainForm, this.tunnelManager);
            };

            this.mainMenu.Items
                .Cast<ToolStripMenuItem>()
                .First(i => i.Name == "Tools")
                .DropDownItems
                .AddRange(new ToolStripMenuItem[] { configMenuItem, tunnelsMenuItem });

            // Adjust states of Session > xxx menu items depending on selected server.
            this.mainMenu.MenuActivate += (sender, args) =>
            {
                // The plugin API does not provide access to the currently selected node
                // in the server tree, so we need to access an internal member to get hold
                // of this information.
                var mainForm = ((MenuStrip)sender).FindForm();
                var selectedNode = (RdcTreeNode)mainForm.GetType().GetMethod(
                    "GetSelectedNode",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(
                        mainForm, 
                        new object[0]);

                if (selectedNode is Server server)
                {
                    var sessionsMenuStrip = this.mainMenu.Items
                        .Cast<ToolStripMenuItem>()
                        .First(i => i.Name == "Session");

                    TweakMenuItemsForSelectedServer(
                        sessionsMenuStrip.DropDown.Items
                            .Cast<object>()
                            .Where(i => i is ToolStripMenuItem)
                            .Cast<ToolStripMenuItem>(),
                        server);
                }
            };
        }

        public void PreLoad(IPluginContext context, System.Xml.XmlNode xmlNode)
        {
        }

        public System.Xml.XmlNode SaveSettings()
        {
            return null;
        }

        public void Shutdown()
        {
            try
            {
                this.tunnelManager.CloseTunnels();
            }
            catch (Exception e)
            {
                // We are shutting down, so there is not too much we can do here.
                // It is pretty likely that some python.exe process will be leaked
                // though.
                Debug.WriteLine($"Failed to close tunnels: {e.Message}");
            }

            this.configurationStore.Dispose();
        }

        //---------------------------------------------------------------------
        // Custom event handlers
        //---------------------------------------------------------------------

        private void OnLoadServersClick(FileGroup fileGroup)
        {
            var projectId = fileGroup.Text;

            Func<string, string> longZoneToShortZone = 
                (string zone) => zone.Substring(zone.LastIndexOf("/") + 1);

            WaitDialog.RunWithDialog(
                this.mainForm,
                "Loading instances...",
                () => this.projects.Value
                            .GetProjectAsync(projectId)
                            .Result
                            .QueryInstancesAcrossZones(),
                allInstances =>
                {
                    // Narrow the list down to Windows instances - there is no point 
                    // of adding Linux instanes to the list of servers.
                    var instances = allInstances.Where(i => ComputeEngineAdapter.IsWindowsInstance(i));

                    // Consolidate zones.
                    var currentZones = fileGroup.Nodes
                        .Cast<RdcTreeNode>()
                        .Where(n => n is Group)
                        .Cast<Group>().Select(s => s.Text).ToHashSet();
                    var cloudZones = instances.Select(i => longZoneToShortZone(i.Zone)).ToHashSet();

                    var missingZones = cloudZones.Subtract(currentZones);
                    var junkZones = currentZones.Subtract(cloudZones);

                    foreach (var zone in junkZones)
                    {
                        fileGroup.Nodes.Remove(fileGroup.Nodes.Cast<Group>().First(s => s.Text == zone));
                    }

                    foreach (var zone in missingZones)
                    {
                        Group.Create(zone, fileGroup);
                    }

                    fileGroup.Expand();

                    // Consolidate instances per zone.
                    foreach (var zone in cloudZones)
                    {
                        var zoneGroup = fileGroup.Nodes
                            .Cast<RdcTreeNode>()
                            .Where(n => n is Group)
                            .Cast<Group>().First(g => g.Text == zone);

                        var currentServersInZone = zoneGroup.Nodes
                            .Cast<RdcTreeNode>()
                            .Where(n => n is Server)
                            .Cast<Server>().Select(s => s.DisplayName).ToHashSet();
                        var cloudServersInZone = instances
                            .Where(i => longZoneToShortZone(i.Zone) == zone)
                            .Select(i => i.Name).ToHashSet();

                        var missingServersInZone = cloudServersInZone.Subtract(currentServersInZone);
                        var junkServersInZone = currentServersInZone.Subtract(cloudServersInZone);

                        foreach (var server in junkServersInZone)
                        {
                            zoneGroup.Nodes.Remove(
                                zoneGroup.Nodes.Cast<Server>().First(s => s.DisplayName == server));
                        }

                        foreach (var server in missingServersInZone)
                        {
                            var instance = instances.First(i => i.Name == server);
                            Server.Create(instance.Name, instance.Name, zoneGroup);
                        }

                        zoneGroup.Expand();
                    }
                });
        }

        private void OnIapConnectClick(Server server, bool connectAs)
        {
            VmInstanceReference instance = new VmInstanceReference(
                server.FileGroup.Text,
                server.Parent.Text,
                server.DisplayName);

            WaitDialog.RunWithDialog(
               this.mainForm,
               "Opening Cloud IAP tunnel...",
               () => this.tunnelManager.Connect(new IapTunnelEndpoint(
                    instance,
                    RemoteDesktopPort),
                    DefaultTunnelConnectionTimeout),
                tunnel =>
                {
                    var originalInheritMode = server.ConnectionSettings.InheritSettingsType.Mode;
                    var originalServerName = server.Properties.ServerName.Value;
                    var originalServerPort = server.ConnectionSettings.Port.Value;

                    try
                    {
                        server.ConnectionSettings.InheritSettingsType.Mode = InheritanceMode.None;
                        server.Properties.ServerName.Value = "localhost";
                        server.ConnectionSettings.Port.Value = tunnel.LocalPort;

                        // Set focus on selected server and connect.
                        server.TreeView.SelectedNode = server;
                        if (connectAs)
                        {
                            server.DoConnectAs();
                        }
                        else
                        {
                            server.Connect();
                        }
                    }
                    finally
                    {
                        // Restore original settings in case the user later wants to
                        // connect directly again.
                        server.Properties.ServerName.Value = originalServerName;
                        server.ConnectionSettings.Port.Value = originalServerPort;
                        server.ConnectionSettings.InheritSettingsType.Mode = originalInheritMode;
                    }
                });
        }

        private void OnResetPasswordClick(Server server)
        {
            // Derive a suggested username from the Google username.
            var googleUsername = GcloudAccountConfiguration.ActiveAccount.Name;
            var suggestedUsername = googleUsername.IndexOf('@') > 0
                ? googleUsername.Substring(0, googleUsername.IndexOf('@'))
                : string.Empty;

            // Prompt for username to use.
            var username = GenerateCredentialsDialog.PromptForUsername(this.mainForm, suggestedUsername);
            if (username == null)
            {
                return;
            }

            VmInstanceReference instance = new VmInstanceReference(
                server.FileGroup.Text,
                server.Parent.Text,
                server.DisplayName);
            
            WaitDialog.RunWithDialog(
                this.mainForm,
                "Generating Windows logon credentials...",
                () => this.windowsPasswordManager.ResetPassword(instance, username),
                credentials =>
                {
                    ShowCredentialsDialog.ShowDialog(
                        this.mainForm,
                        credentials.UserName,
                        credentials.Password);

                    server.LogonCredentials.InheritSettingsType.Mode = InheritanceMode.None;
                    server.LogonCredentials.Domain.Value = "localhost";
                    server.LogonCredentials.UserName.Value = credentials.UserName;
                    server.LogonCredentials.SetPassword(credentials.Password);
                });
        }

        private void OnShowSerialPortOutputClick(Server server)
        {
            VmInstanceReference instance = new VmInstanceReference(
                server.FileGroup.Text,
                server.Parent.Text,
                server.DisplayName);

            WaitDialog.RunWithDialog(
                this.mainForm,
                "Loading project information...",
                () => this.projects.Value.GetProjectAsync(instance.ProjectId),
                project =>
                {
                    SerialPortOutputWindow.Show(this.mainForm, project.GetSerialPortOutput(instance));
                });
        }

        private void OnOpenCloudConsoleClick(Server server)
        {
            VmInstanceReference instance = new VmInstanceReference(
                server.FileGroup.Text,
                server.Parent.Text,
                server.DisplayName);

            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = "https://console.cloud.google.com/compute/instancesDetail/zones/" +
                          $"{instance.Zone}/instances/{instance.InstanceName}?project={instance.ProjectId}"
            });
        }

        private void OnOpenStackdriverLogsClick(Server server)
        {
            VmInstanceReference instance = new VmInstanceReference(
                    server.FileGroup.Text,
                    server.Parent.Text,
                    server.DisplayName);

            WaitDialog.RunWithDialog(
                this.mainForm,
                "Loading instance information...",
                () => this.projects.Value.GetProjectAsync(instance.ProjectId).Result.QueryInstanceAsync(instance),
                instanceDetails =>
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        UseShellExecute = true,
                        Verb = "open",
                        FileName = "https://console.cloud.google.com/logs/viewer?" +
                              $"resource=gce_instance%2Finstance_id%2F{instanceDetails.Id}&project={instance.ProjectId}"
                    });
                });
        }
    }
}
