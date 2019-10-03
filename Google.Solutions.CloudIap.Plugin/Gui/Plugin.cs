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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Solutions.CloudIap.Plugin.Configuration;
using Google.Solutions.Compute.Auth;
using Google.Solutions.Compute.Iap;
using Microsoft.Win32;
using RdcMan;
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Xml;

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
        private const string ConfigurationRegistryPath = "Software\\Google\\RdcMan.Plugin\\1.0";
        private const string CredentialsRegistryPath = ConfigurationRegistryPath + "\\Credentials";

        private PluginEventHandler eventHandler;
        private PluginConfigurationStore configurationStore;
        private RegistryStore credentialStore;

        //---------------------------------------------------------------------
        // IPlugin implementation
        //---------------------------------------------------------------------

        public void PostLoad(IPluginContext context)
        {
            var mainForm = context.MainForm.MainMenuStrip.FindForm();

#if OAUTH
            // Load credential store. Keep the reference around because
            // AuthorizeDialog.Authorize will use it for asynchronous
            // operations.
            this.credentialStore = new RegistryStore(
                RegistryHive.CurrentUser,
                CredentialsRegistryPath);

            var authorization = AuthorizeDialog.Authorize(
                mainForm,
                new ClientSecrets()
                {
                    ClientId = "78381520511-4fu6ve6b49kknk3dkdnpudoi0tivq6jn.apps.googleusercontent.com",
                    ClientSecret = "dRgZl1efp_JKcUqQusuaVIrS"
                },
                new[] { IapTunnelingEndpoint.RequiredScope },
                this.credentialStore);

            if (authorization == null)
            {
                // Not authorized -> disable plugin.
                return;
            }
#else
            var gcloudAccount = GcloudAccount.ActiveAccount;
            if (!GcloudAuthorization.CanAuthorize(gcloudAccount))
            {
                MessageBox.Show(
                    "No active gcloud account found.\n\n" +
                    "Run 'gcloud auth login' to sign on and create a gcloud account.\n\n",
                    "Cloud IAP plugin deactivated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var authorization = GcloudAuthorization.CreateAuthorizationAsync(
                gcloudAccount, null).Result;
#endif

            this.configurationStore = new PluginConfigurationStore(
                RegistryHive.CurrentUser,
                ConfigurationRegistryPath);

            this.eventHandler = new PluginEventHandler(
                configurationStore,
                authorization,
                mainForm,
                context.MainForm.MainMenuStrip);
        }

        public void OnContextMenu(ContextMenuStrip contextMenuStrip, RdcTreeNode node)
        {
            this.eventHandler?.OnContextMenu(contextMenuStrip, node);
        }

        public void Shutdown()
        {
            this.eventHandler?.Shutdown();

            this.configurationStore?.Dispose();
            this.credentialStore?.Dispose();
        }

        public void OnDockServer(ServerBase server)
        {
        }

        public void OnUndockServer(IUndockedServerForm form)
        {
        }

        public void PreLoad(IPluginContext context, XmlNode xmlNode)
        {
        }

        public XmlNode SaveSettings()
        {
            return null;
        }
    }
}
