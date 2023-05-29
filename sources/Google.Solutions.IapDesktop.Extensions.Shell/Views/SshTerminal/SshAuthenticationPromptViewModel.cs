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

namespace Google.Solutions.IapDesktop.Extensions.Session.Views.SshTerminal
{
    [Service]
    public class SshAuthenticationPromptViewModel : ViewModelBase
    {
        private string title;
        private string description;
        private string input;
        private bool isPasswordMasked;

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public string Title
        {
            get => this.title;
            set
            {
                this.title = value;
                RaisePropertyChange();
            }
        }

        public string Description
        {
            // Insert line breaks so that the lines don't get overly long.
            get => this.description.Replace(". ", ".\n");
            set
            {
                this.description = value;
                RaisePropertyChange();
            }
        }

        public string Input
        {
            get => this.input;
            set
            {
                this.input = value;
                RaisePropertyChange();
                RaisePropertyChange((SshAuthenticationPromptViewModel m) => m.IsOkButtonEnabled);
            }
        }

        public bool IsPasswordMasked
        {
            get => this.isPasswordMasked;
            set
            {
                this.isPasswordMasked = value;
                RaisePropertyChange();
            }
        }

        public bool IsOkButtonEnabled
        {
            get => !string.IsNullOrWhiteSpace(this.input);
        }
    }
}
