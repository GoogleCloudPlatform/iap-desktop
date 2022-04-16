//
// Copyright 2022 Google LLC
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
using Google.Solutions.Common.Threading;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    public static class SshAuthenticatorExtensions
    {
        private class SynchronizationContextBoundAuthenticator : ISshAuthenticator
        {
            private readonly ISshAuthenticator authenticator;
            private readonly SynchronizationContext context;

            public SynchronizationContextBoundAuthenticator(
                ISshAuthenticator authenticator,
                SynchronizationContext context)
            {
                this.authenticator = authenticator.ThrowIfNull(nameof(authenticator));
                this.context = context.ThrowIfNull(nameof(context));
            }

            public string Username
                => this.context.Send(() => this.authenticator.Username);

            public ISshKeyPair KeyPair
                => this.context.Send(() => this.authenticator.KeyPair);

            public string Prompt(string name, string instruction, string prompt, bool echo)
                => this.context.Send(() => this.authenticator.Prompt(name, instruction, prompt, echo));
        }

        //---------------------------------------------------------------------
        // Extension methods.
        //---------------------------------------------------------------------

        /// <summary>
        /// Create an authenticator that runs callbacks on a specific
        /// synchronization context.
        /// </summary>
        public static ISshAuthenticator BindToSynchronizationContext(
            this ISshAuthenticator authenticator,
            SynchronizationContext targetContext)
        {
            return targetContext == null
                ? authenticator
                : new SynchronizationContextBoundAuthenticator(
                    authenticator,
                    targetContext);
        }
    }
}
