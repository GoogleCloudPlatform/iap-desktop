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

using Google.Solutions.Mvvm.Binding;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.ActiveDirectory
{
    public class JoinViewModel : ViewModelBase
    {
        public JoinViewModel()
        {
            this.DomainName = ObservableProperty.Build(string.Empty);
            this.IsDomainNameInvalid = ObservableProperty.Build(
                this.DomainName,
                name => !string.IsNullOrWhiteSpace(name) && !name.Contains('.'));

            this.ComputerName = ObservableProperty.Build(string.Empty);
            this.IsComputerNameInvalid = ObservableProperty.Build(
                this.ComputerName,
                name => !string.IsNullOrWhiteSpace(name) &&
                        !IsValidNetbiosComputerName(name));

            this.IsOkButtonEnabled = ObservableProperty.Build(
                this.DomainName,
                this.ComputerName,
                (string domain, string computer) =>
                    !string.IsNullOrEmpty(domain) && domain.Contains('.') &&
                    !string.IsNullOrEmpty(computer) && computer.Trim().Length <= 15);
        }

        internal static bool IsValidNetbiosComputerName(string name)
        {
            name = name.Trim();

            //
            // Perform a basic check. The actual rules are more complex,
            // see <https://docs.microsoft.com/en-us/troubleshoot/windows-server/identity/naming-conventions-for-computer-domain-site-ou>
            //
            return name.Length > 0 &&
                name.Length <= 15 &&
                name.All(c => char.IsLetterOrDigit(c) || c == '-');
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> DomainName { get; }

        public ObservableFunc<bool> IsDomainNameInvalid { get; }

        public ObservableProperty<string> ComputerName { get; }

        public ObservableFunc<bool> IsComputerNameInvalid { get; }

        public ObservableFunc<bool> IsOkButtonEnabled { get; }
    }
}
