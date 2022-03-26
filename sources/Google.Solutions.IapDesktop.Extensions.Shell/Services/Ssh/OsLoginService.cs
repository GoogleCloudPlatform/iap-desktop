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

using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh
{
    public interface IOsLoginService
    {
        Task<AuthorizedKeyPair> AuthorizeKeyPairAsync(
            ProjectLocator project,
            OsLoginSystemType os,
            ISshKeyPair key,
            TimeSpan validity,
            CancellationToken token);
    }

    public enum OsLoginSystemType
    {
        Linux
    }

    [Service(typeof(IOsLoginService))]
    public class OsLoginService : IOsLoginService
    {
        private readonly IOsLoginAdapter adapter;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal OsLoginService(IOsLoginAdapter adapter)
        {
            this.adapter = adapter;
        }

        public OsLoginService(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IOsLoginAdapter>())
        {
        }

        //---------------------------------------------------------------------
        // IOsLoginService.
        //---------------------------------------------------------------------

        public async Task<AuthorizedKeyPair> AuthorizeKeyPairAsync(
            ProjectLocator project,
            OsLoginSystemType os,
            ISshKeyPair key,
            TimeSpan validity,
            CancellationToken token)
        {
            Utilities.ThrowIfNull(project, nameof(project));
            Utilities.ThrowIfNull(key, nameof(key));

            if (os != OsLoginSystemType.Linux)
            {
                throw new ArgumentException(nameof(os));
            }

            if (validity.TotalSeconds <= 0)
            {
                throw new ArgumentException(nameof(validity));
            }

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(project))
            {
                //
                // If OS Login is enabled for a project, we have to use
                // the Posix username from the OS Login login profile.
                //
                // Note that the Posix account managed by OS login can 
                // differ based on the project that we're trying to access.
                // Therefore, make sure to specify the project when
                // importing the key.
                //
                // OS Login auto-generates a username for us. Again, this
                // username might differ based on project/organization.
                //

                //
                // Import the key for the given project.
                //

                var loginProfile = await this.adapter.ImportSshPublicKeyAsync(
                        project,
                        key,
                        validity,
                        token)
                    .ConfigureAwait(false);

                //
                // Although rare, there could be multiple POSIX accounts.
                //
                var account = loginProfile.PosixAccounts
                    .EnsureNotNull()
                    .FirstOrDefault(a => a.Primary == true &&
                                            a.OperatingSystemType == "LINUX");

                if (account == null)
                {
                    // 
                    // This is strange, the account should have been created.
                    //
                    throw new OsLoginSshKeyImportFailedException(
                        "Imported SSH key to OSLogin, but no POSIX account was created",
                        HelpTopics.TroubleshootingOsLogin);
                }

                return AuthorizedKeyPair.ForOsLoginAccount(key, account);
            }
        }
    }

    public class OsLoginSshKeyImportFailedException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public OsLoginSshKeyImportFailedException(
            string message,
            IHelpTopic helpTopic)
            : base(message)
        {
            this.Help = helpTopic;
        }
    }
}
