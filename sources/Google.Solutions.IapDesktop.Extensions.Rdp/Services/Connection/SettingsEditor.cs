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

using Google.Solutions.IapDesktop.Application.Settings;
using System;
using System.ComponentModel;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection
{
    public interface ISettingsEditor : INotifyPropertyChanged
    {
        ISettingsCollection Settings { get; }
        void SaveChanges();
    }

    public class SettingsEditor : ISettingsEditor
    {
        private readonly Action<ISettingsCollection> saveSettings;

        public event PropertyChangedEventHandler PropertyChanged;

        public ISettingsCollection Settings { get; }

        public SettingsEditor(
            ISettingsCollection settings,
            Action<ISettingsCollection> saveSettings)
        {
            this.Settings = settings;
            this.saveSettings = saveSettings;
        }

        //---------------------------------------------------------------------
        // ISettingsObject.
        //---------------------------------------------------------------------

        public void SaveChanges()
        {
            this.saveSettings(this.Settings);

            // Notify that all properties have changed.
            // NB. null/empty is a valid value.
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(null));
        }
    }
}
