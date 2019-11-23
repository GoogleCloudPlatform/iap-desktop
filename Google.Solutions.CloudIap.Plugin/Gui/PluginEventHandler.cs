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
using Google.Solutions.Compute;
using Google.Solutions.Compute.Auth;
using RdcMan;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.CloudIap.Plugin.Gui
{
    internal class PluginEventHandler
    {
        private const int RemoteDesktopPort = 3389;

        private readonly PluginConfigurationStore configurationStore;
        private readonly TunnelManagerBase tunnelManager;
        private readonly IAuthorization authorization;

        private readonly Form mainForm;
        private readonly MenuStrip mainMenu;

        public PluginEventHandler(
            PluginConfigurationStore configurationStore,
            IAuthorization authorization,
            Form mainForm,
            MenuStrip mainMenu)
        {
            this.configurationStore = configurationStore;
            this.authorization = authorization;
            this.mainForm = mainForm;
            this.mainMenu = mainMenu;

            // N.B. Do not pre-create a ComputeEngineAdapter because the 
            // underlying ComputeService caches OAuth credentials, defying
            // re-auth.

            var configuration = configurationStore.Configuration;
            Compute.Compute.Trace.Listeners.Add(new DefaultTraceListener());
            Compute.Compute.Trace.Switch.Level = configuration.TracingLevel;

            this.tunnelManager = configuration.Tunneler == Tunneler.Gcloud
                ? (TunnelManagerBase)new GcloudTunnelManager(this.configurationStore)
                : (TunnelManagerBase)new DefaultTunnelingManager(authorization.Credential);

            // Add menu items.
            var configMenuItem = new ToolStripMenuItem("&Settings...");
            configMenuItem.Click += (sender, args) =>
            {
                var currentConfiguration = this.configurationStore.Configuration;
                if (ConfigurationDialog.ShowDialog(this.mainForm, currentConfiguration)
                    == DialogResult.OK)
                {
                    // Write back updated configuration
                    configurationStore.Configuration = currentConfiguration;
                }
            };

            var tunnelsMenuItem = new ToolStripMenuItem("Active &tunnels...");
            tunnelsMenuItem.Click += (sender, args) =>
            {
                TunnelsWindow.ShowDialog(this.mainForm, this.tunnelManager);
            };

            var signOutMenuItem = new ToolStripMenuItem("Sign &out");
            signOutMenuItem.Click += async (sender, args) =>
            {
                try
                {
                    await this.authorization.RevokeAsync();
                    MessageBox.Show(
                        this.mainForm,
                        "You will be prompted to sign in again once you restart the application.",
                        "Signed out",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception e)
                {
                    ExceptionUtil.HandleException(this.mainForm, "Sign out", e);
                }
            };

            var pluginMenuItem = new ToolStripMenuItem("Cloud &IAP");
            pluginMenuItem
                .DropDownItems
                .AddRange(new ToolStripMenuItem[] { 
                    configMenuItem,
                    tunnelsMenuItem,
                    signOutMenuItem,
                });

            this.mainMenu.Items.Insert(
                this.mainMenu.Items.Count - 1,
                pluginMenuItem);
            
            // Adjust states of Session > xxx menu items depending on selected server.
            this.mainMenu.MenuActivate += (sender, args) =>
            {
                // The plugin API does not provide access to the currently selected node
                // in the server tree, so we need to access an internal member to get hold
                // of this information.
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

        //---------------------------------------------------------------------
        // Plugin event handlers.
        //---------------------------------------------------------------------

        public void OnContextMenu(System.Windows.Forms.ContextMenuStrip contextMenuStrip, RdcTreeNode node)
        {
            // If node refers to a node in a virtual group like "Connected Servers",
            // perform a dereference first.
            if (node is ServerRef serverRef)
            {
                node = serverRef.ServerNode;
            }

            if (node is FileGroup fileGroup)
            {
                ToolStripMenuItem loadServers = new ToolStripMenuItem(
                    string.Format(
                        "{0} GCE &instances from {1}",
                        fileGroup.Nodes.Count == 0 ? "Add" : "Refresh",
                        node.Text), 
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
                new TunnelDestination(
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
                () => ComputeEngineAdapter.Create(authorization.Credential).QueryInstancesAsync(projectId),
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
                },
                this.authorization.ReauthorizeAsync);
        }

        private void OnIapConnectClick(Server server, bool connectAs)
        {
            var instance = new VmInstanceReference(
                server.FileGroup.Text,
                server.Parent.Text,
                server.DisplayName);

            // Silly workaround for an RDCMan bug: When the server tree is set to
            // auto-hide and you trigger this action, the WM_KILLFOCUS message is not
            // handled properly by the respective ServerLabel. By activating the 
            // server tree, we prevent that from happening (at the expense of 
            // having the server tree briefly pop up).
            try
            {
                mainForm.GetType().GetMethod(
                    "GoToServerTree",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(
                        this.mainForm,
                        new object[0]);
            }
            catch (Exception)
            {
            }

            WaitDialog.RunWithDialog(
               this.mainForm,
               "Opening Cloud IAP tunnel...",
               () => this.tunnelManager.ConnectAsync(new TunnelDestination(
                    instance,
                    RemoteDesktopPort),
                    this.configurationStore.Configuration.IapConnectionTimeout),
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
                },
                this.authorization.ReauthorizeAsync);
        }

        private void OnResetPasswordClick(Server server)
        {
            // Derive a suggested username from the Windows login name.
            var suggestedUsername = Environment.UserName;

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
                () => ComputeEngineAdapter.Create(authorization.Credential).ResetWindowsUserAsync(instance, username),
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
                },
                this.authorization.ReauthorizeAsync);
        }

        private void OnShowSerialPortOutputClick(Server server)
        {
            var instanceRef = new VmInstanceReference(
                server.FileGroup.Text,
                server.Parent.Text,
                server.DisplayName);

            // TODO: Handle reauth.
            SerialPortOutputWindow.Show(
                this.mainForm,
                $"{instanceRef.InstanceName} ({instanceRef.Zone})",
                ComputeEngineAdapter.Create(authorization.Credential).GetSerialPortOutput(instanceRef));
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
                () => ComputeEngineAdapter.Create(authorization.Credential).QueryInstanceAsync(instance),
                instanceDetails =>
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        UseShellExecute = true,
                        Verb = "open",
                        FileName = "https://console.cloud.google.com/logs/viewer?" +
                              $"resource=gce_instance%2Finstance_id%2F{instanceDetails.Id}&project={instance.ProjectId}"
                    });
                },
                this.authorization.ReauthorizeAsync);
        }
    }
}
