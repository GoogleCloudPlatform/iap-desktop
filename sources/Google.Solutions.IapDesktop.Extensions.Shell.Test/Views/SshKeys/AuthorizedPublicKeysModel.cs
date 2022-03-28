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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshKeys
{
    public class AuthorizedPublicKeysModel
    {
        public IEnumerable<Item> Items { get; }

        public string DisplayName { get; }

        public IEnumerable<string> Warnings { get; }

        public AuthorizedPublicKeysModel(
            string displayName,
            IEnumerable<Item> items,
            IEnumerable<string> warnings)
        {
            this.DisplayName = displayName.ThrowIfNull(nameof(displayName));
            this.Items = items.EnsureNotNull();
            this.Warnings = warnings.EnsureNotNull();
        }

        public class Item
        {
            public IAuthorizedPublicKey Key { get; }

            public KeyAuthorizationMethods AuthorizationMethod { get; }

            internal Item(
                IAuthorizedPublicKey key,
                KeyAuthorizationMethods method)
            {
                Debug.Assert(method.IsSingleFlag());
                this.Key = key.ThrowIfNull(nameof(key));
                this.AuthorizationMethod = method;
            }
        }
    }
}
