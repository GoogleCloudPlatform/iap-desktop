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

using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Windows
{
    public class PropertiesViewModel : ViewModelBase
    {
        private readonly List<PropertiesSheetViewModelBase> sheets;

        public PropertiesViewModel()
        {
            this.sheets = new List<PropertiesSheetViewModelBase>();

            this.IsDirty = new ObservableFunc<bool>(
                () => this.sheets.Any(s => s.IsDirty.Value));

            this.ApplyCommand = ObservableCommand.Build(
                "&Apply",
                ApplyChangesAsync,
                this.IsDirty);
            this.OkCommand = ObservableCommand.Build(
                "OK",
                ApplyChangesAsync);
            this.CancelCommand = ObservableCommand.Build(
                "Cancel",
                () => Task.CompletedTask);
        }

        private async Task ApplyChangesAsync()
        {
            foreach (var sheet in this.sheets.Where(t => t.IsDirty.Value))
            {
                await sheet
                    .ApplyChangesAsync()
                    .ConfigureAwait(true);
                
                Debug.Assert(!sheet.IsDirty.Value);
            }
        }

        internal void AddSheet(PropertiesSheetViewModelBase sheet)
        {
            this.sheets.Add(sheet);
            sheet.IsDirty.AddDependentProperty(this.IsDirty);
        }

        internal IEnumerable<PropertiesSheetViewModelBase> Sheets => this.sheets;

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public IObservableProperty<bool> IsDirty { get; }

        //---------------------------------------------------------------------
        // Commands.
        //---------------------------------------------------------------------

        public IObservableCommand ApplyCommand { get; }

        public IObservableCommand OkCommand { get; }

        public IObservableCommand CancelCommand { get; }
    }
}
