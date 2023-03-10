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
using Google.Solutions.Mvvm.Windows;
using Google.Solutions.Testing.Common;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Windows
{
    [TestFixture]
    public class TestPropertiesViewModel
    {
        private class SampleSheet : PropertiesSheetViewModelBase
        {
            public uint ApplyCalls = 0;

            public SampleSheet() : base("Sample")
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
        public void WhenSheetChangesDirtyState_ThenIsDirtyIsUpdated()
        {
            using (var viewModel = new PropertiesViewModel())
            {
                var sheet = new SampleSheet();
                viewModel.AddSheet(sheet);
                viewModel.AddSheet(new SampleSheet());

                Assert.IsFalse(viewModel.IsDirty.Value);

                PropertyAssert.RaisesPropertyChangedNotification(
                    sheet.IsDirty,
                    () => sheet.IsDirty.Value = true,
                    "Value");

                Assert.IsTrue(viewModel.IsDirty.Value);

                PropertyAssert.RaisesPropertyChangedNotification(
                    sheet.IsDirty,
                    () => sheet.IsDirty.Value = false,
                    "Value");

                Assert.IsFalse(viewModel.IsDirty.Value);
            }
        }

        //---------------------------------------------------------------------
        // ApplyCommand.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSheetDirty_ThenApplyCommandIsEnabled()
        {
            using (var viewModel = new PropertiesViewModel())
            {
                var sheet = new SampleSheet();
                sheet.IsDirty.Value = true;
                viewModel.AddSheet(sheet);

                Assert.AreEqual(
                    CommandState.Enabled,
                    viewModel.ApplyCommand.QueryState(viewModel));
            }
        }

        [Test]
        public void WhenNoSheetDirty_ThenApplyCommandIsDisabled()
        {
            using (var viewModel = new PropertiesViewModel())
            {
                viewModel.AddSheet(new SampleSheet());

                Assert.AreEqual(
                    CommandState.Disabled,
                    viewModel.ApplyCommand.QueryState(viewModel));
            }
        }


        [Test]
        public void WhenSomeSheetsDirty_ThenApplyCommandOnlyAppliesDirtySheets()
        {
            using (var viewModel = new PropertiesViewModel())
            {
                var sheet1 = new SampleSheet();
                sheet1.IsDirty.Value = true;

                var sheet2 = new SampleSheet();
                sheet2.IsDirty.Value = false;

                viewModel.AddSheet(sheet1);
                viewModel.AddSheet(sheet2);

                viewModel.ApplyCommand.ExecuteAsync(viewModel);

                Assert.AreEqual(1, sheet1.ApplyCalls);
                Assert.AreEqual(0, sheet2.ApplyCalls);
            }
        }
    }
}
