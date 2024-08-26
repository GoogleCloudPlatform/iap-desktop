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
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    public class TestPropertiesViewModel
    {
        private class SampleSheetView : UserControl, IPropertiesSheetView
        {
            public Type ViewModel => typeof(SampleSheetViewModel);

            public void Bind(PropertiesSheetViewModelBase viewModel, IBindingContext context)
            {
            }
        }

        private class SampleSheetViewModel : PropertiesSheetViewModelBase
        {
            public uint ApplyCalls = 0;

            public SampleSheetViewModel() : base("Sample")
            {
            }

            public override ObservableProperty<bool> IsDirty { get; }
                = ObservableProperty.Build<bool>(false);

            protected override void ApplyChanges()
            {
                this.ApplyCalls++;
                this.IsDirty.Value = false;
            }
        }

        //---------------------------------------------------------------------
        // IsDirty.
        //---------------------------------------------------------------------

        [Test]
        public void IsDirty_WhenSheetChangesDirtyState_ThenIsDirtyIsUpdated()
        {
            using (var viewModel = new PropertiesViewModel())
            {
                var sheetViewModel = new SampleSheetViewModel();
                var sheetView = new SampleSheetView();
                viewModel.AddSheet(sheetView, sheetViewModel);
                viewModel.AddSheet(new SampleSheetView(), new SampleSheetViewModel());

                Assert.IsFalse(viewModel.IsDirty.Value);

                PropertyAssert.RaisesPropertyChangedNotification(
                    sheetViewModel.IsDirty,
                    () => sheetViewModel.IsDirty.Value = true,
                    "Value");

                Assert.IsTrue(viewModel.IsDirty.Value);

                PropertyAssert.RaisesPropertyChangedNotification(
                    sheetViewModel.IsDirty,
                    () => sheetViewModel.IsDirty.Value = false,
                    "Value");

                Assert.IsFalse(viewModel.IsDirty.Value);
            }
        }

        //---------------------------------------------------------------------
        // ApplyCommand.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyCommand_WhenSheetDirty_ThenApplyCommandCanBeExecuted()
        {
            using (var viewModel = new PropertiesViewModel())
            {
                var sheetViewModel = new SampleSheetViewModel();
                viewModel.AddSheet(new SampleSheetView(), sheetViewModel);

                sheetViewModel.IsDirty.Value = true;

                Assert.IsTrue(viewModel.ApplyCommand.CanExecute.Value);
            }
        }

        [Test]
        public void ApplyCommand_WhenNoSheetDirty_ThenApplyCommandCannotBeExecuted()
        {
            using (var viewModel = new PropertiesViewModel())
            {
                viewModel.AddSheet(new SampleSheetView(), new SampleSheetViewModel());

                Assert.IsFalse(viewModel.ApplyCommand.CanExecute.Value);
            }
        }


        [Test]
        public void ApplyCommand_WhenSomeSheetsDirty_ThenApplyCommandOnlyAppliesDirtySheets()
        {
            using (var viewModel = new PropertiesViewModel())
            {
                var sheetViewModel1 = new SampleSheetViewModel();
                sheetViewModel1.IsDirty.Value = true;

                var sheetViewModel2 = new SampleSheetViewModel();
                sheetViewModel2.IsDirty.Value = false;

                viewModel.AddSheet(new SampleSheetView(), sheetViewModel1);
                viewModel.AddSheet(new SampleSheetView(), sheetViewModel2);

                viewModel.ApplyCommand.ExecuteAsync(CancellationToken.None);

                Assert.AreEqual(1, sheetViewModel1.ApplyCalls);
                Assert.AreEqual(0, sheetViewModel2.ApplyCalls);
            }
        }
    }
}
