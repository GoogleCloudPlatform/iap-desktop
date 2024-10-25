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

using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.SshKeys
{
    internal class AuthorizedPublicKeysList : SearchableList<AuthorizedPublicKeysModel.Item>
    {
        public AuthorizedPublicKeysList()
        {
            this.List.SmallImageList = new ImageList();
            this.List.SmallImageList.Images.Add(Resources.AuthorizedKey_16);

            AddColumn("User", 170);
            AddColumn("Key type", 120);
            AddColumn("Source", 120);
            AddColumn("Expiry", 120);
            AddColumn("Public key", 200);

            this.List.GridLines = true;

            this.List.BindImageIndex(m => 0);
            this.List.BindColumn(0, m => m.Key.Email ?? string.Empty);
            this.List.BindColumn(1, m => m.Key.KeyType);
            this.List.BindColumn(2, m => m.AuthorizationMethod.ToString());
            this.List.BindColumn(3, m =>
            {
                if (m.Key.ExpireOn == null)
                {
                    return string.Empty;
                }
                else if (m.Key.ExpireOn.Value < DateTime.UtcNow)
                {
                    return $"{m.Key.ExpireOn?.ToShortDateString()} (expired)";
                }
                else
                {
                    return $"{m.Key.ExpireOn?.ToShortDateString()}";
                }
            });
            this.List.BindColumn(4, m => m.Key.PublicKey);
        }
    }
}
