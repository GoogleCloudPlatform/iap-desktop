//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Settings.Collection;

namespace Google.Solutions.IapDesktop.Application.Windows.Auth
{
    public class AuthorizeOptionsViewModel : ViewModelBase
    {
        public AuthorizeOptionsViewModel(IRepository<IAccessSettings> repository)
        {
            //
            // Load current settings from repository.
            //
            _ = WorkforcePoolProviderLocator.TryParse(
                repository.GetSettings().WorkforcePoolProvider.Value,
                out var workforcePoolProvider);

            this.IsGaiaOptionChecked = ObservableProperty.Build(workforcePoolProvider == null);
            this.IsWorkforcePoolOptionChecked = ObservableProperty.Build(workforcePoolProvider != null);
            this.WorkforcePoolLocationId = ObservableProperty.Build(workforcePoolProvider?.Location);
            this.WorkforcePoolId = ObservableProperty.Build(workforcePoolProvider?.Pool);
            this.WorkforcePoolProviderId = ObservableProperty.Build(workforcePoolProvider?.Provider);
            this.IsOkButtonEnabled = ObservableProperty.Build(
                this.IsWorkforcePoolOptionChecked,
                this.WorkforcePoolLocationId,
                this.WorkforcePoolId,
                this.WorkforcePoolProviderId,
                (workforcePoolEnabled, locationId, poolId, providerId) =>
                {
                    if (!workforcePoolEnabled)
                    {
                        return true;
                    }
                    else
                    {
                        return
                            !string.IsNullOrEmpty(locationId) &&
                            !string.IsNullOrEmpty(poolId) &&
                            !string.IsNullOrEmpty(providerId);
                    }
                });
            this.ApplyChanges = ObservableCommand.Build(
                string.Empty,
                () =>
                {
                    //
                    // Write back changes to repository.
                    //
                    var settings = repository.GetSettings();
                    settings.WorkforcePoolProvider.Value = this.WorkforcePoolProvider?.ToString();
                    repository.SetSettings(settings);
                });
        }

        //---------------------------------------------------------------------
        // Input properties.
        //---------------------------------------------------------------------

        public WorkforcePoolProviderLocator WorkforcePoolProvider
        {
            get
            {
                if (this.IsWorkforcePoolOptionChecked.Value &&
                    !string.IsNullOrEmpty(this.WorkforcePoolLocationId.Value) &&
                    !string.IsNullOrEmpty(this.WorkforcePoolId.Value) &&
                    !string.IsNullOrEmpty(this.WorkforcePoolProviderId.Value))
                {
                    return new WorkforcePoolProviderLocator(
                        this.WorkforcePoolLocationId.Value,
                        this.WorkforcePoolId.Value,
                        this.WorkforcePoolProviderId.Value);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    this.IsGaiaOptionChecked.Value = true;
                    this.IsWorkforcePoolOptionChecked.Value = false;

                    this.WorkforcePoolId.Value = null;
                    this.WorkforcePoolProviderId.Value = null;
                }
                else
                {
                    this.IsGaiaOptionChecked.Value = false;
                    this.IsWorkforcePoolOptionChecked.Value = true;

                    this.WorkforcePoolLocationId.Value = value.Location;
                    this.WorkforcePoolId.Value = value.Pool;
                    this.WorkforcePoolProviderId.Value = value.Provider;
                }
            }
        }

        //---------------------------------------------------------------------
        // Observable commands.
        //---------------------------------------------------------------------

        public ObservableCommand ApplyChanges { get; }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public ObservableFunc<bool> IsOkButtonEnabled { get; }
        public ObservableProperty<bool> IsGaiaOptionChecked { get; }
        public ObservableProperty<bool> IsWorkforcePoolOptionChecked { get; }
        public ObservableProperty<string> WorkforcePoolLocationId { get; }
        public ObservableProperty<string> WorkforcePoolId { get; }
        public ObservableProperty<string> WorkforcePoolProviderId { get; }
    }
}
