﻿//
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

using Moq;
using NUnit.Framework;
using System;
using Google.Solutions.Mvvm.Binding;
using System.Windows.Forms;
using Google.Solutions.Testing.Common.Integration;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [Apartment(System.Threading.ApartmentState.STA)]
    [TestFixture]
    public class TestPropertiesView
    {
        private class SampleSheetView : UserControl, IPropertiesSheetView
        {
            public bool Bound = false;
            public Type ViewModel => typeof(SampleSheetViewModel);

            public void Bind(PropertiesSheetViewModelBase viewModel, IBindingContext context)
            {
                this.Bound = true;
            }
        }

        private class SampleSheetViewModel : PropertiesSheetViewModelBase
        {
            public SampleSheetViewModel() : base("Sample")
            {
            }

            public override ObservableProperty<bool> IsDirty { get; }
                = ObservableProperty.Build(false);
        }

        private IServiceProvider CreateServiceProvider()
        {
            var serviceProvider = new Mock<IServiceProvider>();

            serviceProvider.Add(new PropertiesView());
            serviceProvider.Add(new PropertiesViewModel());
            serviceProvider.AddMock<IBindingContext>();

            return serviceProvider.Object;
        }

        //---------------------------------------------------------------------
        // Binding.
        //---------------------------------------------------------------------

        [Test]
        public void SheetsAreBound()
        {
            var serviceProvider = CreateServiceProvider();

            var sheetView = new SampleSheetView();

            var window = serviceProvider.GetWindow<PropertiesView, PropertiesViewModel>();
            window.ViewModel.AddSheet(sheetView, new SampleSheetViewModel());
            window.Form.Show();

            Assert.IsTrue(sheetView.Bound);

            window.Form.Close();
        }

        [InteractiveTest]
        [Test]
        public void TestUi()
        {
            var serviceProvider = CreateServiceProvider();

            var viewModel = new SampleSheetViewModel();
            var view = new SampleSheetView();
            var button = new Button()
            {
                Text = "Mark dirty"
            };
            button.Click += (_, __) => viewModel.IsDirty.Value = true;
            view.Controls.Add(button);

            var window = serviceProvider.GetDialog<PropertiesView, PropertiesViewModel>();
            window.ViewModel.AddSheet(view, viewModel);
            window.ShowDialog(null);
        }
    }
}
