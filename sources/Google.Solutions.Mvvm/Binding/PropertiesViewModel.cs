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

using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Binding
{
    public class PropertiesViewModel : ViewModelBase
    {
        internal IList<Sheet> Sheets { get; }

        public PropertiesViewModel()
        {
            this.Sheets = new List<Sheet>();

            this.IsDirty = new ObservableFunc<bool>(
                () => this.Sheets.Any(s => s.ViewModel.IsDirty.Value));
            this.WindowTitle = ObservableProperty.Build("Properties");

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

            this.OkCommand.ActivityText = "Applying changes";
            this.ApplyCommand.ActivityText = "Applying changes";
        }

        private async Task ApplyChangesAsync()
        {
            foreach (var sheet in this.Sheets.Where(t => t.ViewModel.IsDirty.Value))
            {
                await sheet
                    .ViewModel
                    .ApplyChangesAsync()
                    .ConfigureAwait(true);

                Debug.Assert(!sheet.ViewModel.IsDirty.Value);
            }
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        internal IObservableProperty<bool> IsDirty { get; }

        public ObservableProperty<string> WindowTitle { get; }

        //---------------------------------------------------------------------
        // Commands.
        //---------------------------------------------------------------------

        internal IObservableCommand ApplyCommand { get; }

        internal IObservableCommand OkCommand { get; }

        internal IObservableCommand CancelCommand { get; }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Add a property sheet. This needs to be done before binding.
        /// </summary>
        public void AddSheet(
            IPropertiesSheetView view,
            PropertiesSheetViewModelBase viewModel)
        {
            Precondition.Expect(
                view.ViewModel == viewModel.GetType(),
                "The view model must match the view");
            Precondition.Expect(
                viewModel.View == null,
                "The view must not have been bound yet");

            this.Sheets.Add(new Sheet(view, viewModel));

            viewModel.IsDirty.AddDependentProperty(this.IsDirty);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        internal readonly struct Sheet
        {
            public readonly IPropertiesSheetView View;
            public readonly PropertiesSheetViewModelBase ViewModel;

            public Sheet(IPropertiesSheetView view, PropertiesSheetViewModelBase viewModel)
            {
                this.View = view;
                this.ViewModel = viewModel;
            }
        }
    }
}
