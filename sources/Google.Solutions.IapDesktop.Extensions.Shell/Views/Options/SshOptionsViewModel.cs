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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.Ssh.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Options
{
    public interface ISshOptionsSheet : IPropertiesSheet
    { }

    public class SshOptionsViewModel : ViewModelBase, IPropertiesSheetViewModel
    {
        private static readonly SshKeyType[] publicKeyTypes =
            Enum.GetValues(typeof(SshKeyType))
                .Cast<SshKeyType>()
                .ToArray();

        private bool isPropagateLocaleEnabled;
        private int publicKeyValidityInDays;
        private SshKeyType publicKeyType;

        private readonly SshSettingsRepository settingsRepository;

        private bool isDirty;

        public SshOptionsViewModel(
            SshSettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository;

            //
            // Read current settings.
            //
            // NB. Do not hold on to the settings object because other tabs
            // might apply changes to other application settings.
            //
            var settings = this.settingsRepository.GetSettings();

            this.IsPropagateLocaleEnabled =
                settings.IsPropagateLocaleEnabled.BoolValue;

            this.PublicKeyValidityInDays =
                (int)TimeSpan.FromSeconds(settings.PublicKeyValidity.IntValue).TotalDays;
            this.IsPublicKeyValidityInDaysEditable = !settings.PublicKeyValidity.IsReadOnly;

            this.publicKeyType = settings.PublicKeyType.EnumValue;
            this.IsPublicKeyTypeEditable = !settings.PublicKeyType.IsReadOnly;

            this.isDirty = false;
        }

        //---------------------------------------------------------------------
        // IOptionsDialogPane.
        //---------------------------------------------------------------------

        public string Title => "SSH";

        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.isDirty = value;
                RaisePropertyChange();
            }
        }

        public DialogResult ApplyChanges()
        {
            Debug.Assert(this.IsDirty);

            //
            // Save settings.
            //
            var settings = this.settingsRepository.GetSettings();

            settings.IsPropagateLocaleEnabled.BoolValue =
                this.IsPropagateLocaleEnabled;
            settings.PublicKeyValidity.IntValue =
                (int)TimeSpan.FromDays((int)this.PublicKeyValidityInDays).TotalSeconds;
            settings.PublicKeyType.EnumValue = this.PublicKeyType;

            this.settingsRepository.SetSettings(settings);

            this.IsDirty = false;

            return DialogResult.OK;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsPublicKeyValidityInDaysEditable { get; }
        public bool IsPublicKeyTypeEditable { get; }

        public bool IsPropagateLocaleEnabled
        {
            get => this.isPropagateLocaleEnabled;
            set
            {
                this.IsDirty = true;
                this.isPropagateLocaleEnabled = value;
                RaisePropertyChange();
            }
        }

        public SshKeyType PublicKeyType
        {
            get => this.publicKeyType;
            set
            {
                this.IsDirty = true;
                this.publicKeyType = value;
                RaisePropertyChange();
            }
        }

        public int PublicKeyTypeIndex
        {
            get => Array.IndexOf(publicKeyTypes, this.PublicKeyType);
            set => this.PublicKeyType = publicKeyTypes[value];
        }

        public decimal PublicKeyValidityInDays
        {
            get => this.publicKeyValidityInDays;
            set
            {
                this.IsDirty = true;
                this.publicKeyValidityInDays = (int)value;
                RaisePropertyChange();
            }
        }

        public IList<SshKeyType> AllPublicKeyTypes => publicKeyTypes;
    }
}
