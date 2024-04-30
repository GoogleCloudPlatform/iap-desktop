//
// Copyright 2021 Google LLC
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

using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials
{
    [Service]
    public class NewCredentialsViewModel : ViewModelBase
    {
        //
        // SAM usernames do not permit these characters, see
        // https://docs.microsoft.com/en-us/windows/desktop/adschema/a-samaccountname
        //
        private const string DisallowedCharactersInUsername = "\"/\\[]:;|=,+*?<>";

        private static readonly string[] ReservedUsernames = new[]
        {
            "administrator",
            "guest",
            "defaultaccount",
            "wdagutilityaccount"
        };

        static NewCredentialsViewModel()
        {
            Debug.Assert(ReservedUsernames.All(u => u == u.ToLower()));
        }

        private string username = string.Empty;

        public bool IsAllowedCharacterForUsername(char c)
        {
            return !DisallowedCharactersInUsername.Contains(c);
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public string Username
        {
            get => this.username;
            set
            {
                this.username = value;
                RaisePropertyChange();
                RaisePropertyChange((NewCredentialsViewModel m) => m.IsUsernameReserved);
                RaisePropertyChange((NewCredentialsViewModel m) => m.IsOkButtonEnabled);
            }
        }

        public bool IsUsernameReserved
        {
            get => ReservedUsernames.Contains(this.username.ToLower());
        }

        public bool IsOkButtonEnabled
        {
            get => !string.IsNullOrWhiteSpace(this.username) &&
               !ReservedUsernames.Contains(this.username.ToLower());
        }
    }
}
