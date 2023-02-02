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
using Google.Solutions.Mvvm.Theme;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestView
    {
        private class SampleViewModel : ViewModelBase
        {
            public bool IsDisposed => this.Disposed;
        }

        private class SampleForm : Form, IView<SampleViewModel>
        {
            public SampleViewModel ViewModel { get; private set; }

            public void Bind(SampleViewModel viewModel)
            {
                this.ViewModel = viewModel;
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);

                this.DialogResult = DialogResult.OK;
                Close();
            }
        }

        private IServiceProvider CreateServiceProvider(
            SampleForm view,
            SampleViewModel viewModel)
        {
            var serviceProvider = new Mock<IServiceProvider>();

            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(SampleForm))))
                .Returns(view);

            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(SampleViewModel))))
                .Returns(viewModel);

            return serviceProvider.Object;
        }

        //---------------------------------------------------------------------
        // ViewExtensions.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFactoryThemeSet_ThenCreateAppliesTheme()
        {
            var serviceProvider = CreateServiceProvider(new SampleForm(), new SampleViewModel());
            var theme = new Mock<IControlTheme>().Object;

            var factory = serviceProvider.GetViewFactory<SampleForm, SampleViewModel>();
            factory.Theme = theme;

            var view = factory.Create();

            Assert.AreSame(theme, view.Theme);
        }

        //---------------------------------------------------------------------
        // ShowDialog.
        //---------------------------------------------------------------------

        [Test]
        public void ShowDialogAppliesTheme()
        {
            var serviceProvider = CreateServiceProvider(new SampleForm(), new SampleViewModel());
            var theme = new Mock<IControlTheme>();

            var view = serviceProvider.GetView<SampleForm, SampleViewModel>();
            view.Theme = theme.Object;

            view.ShowDialog(null);

            theme.Verify(t => t.ApplyTo(It.IsAny<Control>()), Times.Once);
        }

        [Test]
        public void ShowDialogBindsViewModel()
        {
            var form = new SampleForm();
            var serviceProvider = CreateServiceProvider(form, new SampleViewModel());
            var view = serviceProvider.GetView<SampleForm, SampleViewModel>();

            Assert.IsNull(form.ViewModel);

            view.ShowDialog(null);

            Assert.IsNotNull(form.ViewModel);
        }

        [Test]
        public void ShowDialogDisposesResources()
        {
            var form = new SampleForm();
            var viewModel = new SampleViewModel();

            var serviceProvider = CreateServiceProvider(form, viewModel);
            var view = serviceProvider.GetView<SampleForm, SampleViewModel>();

            Assert.IsFalse(form.IsDisposed);
            Assert.IsFalse(viewModel.IsDisposed);

            view.ShowDialog(null);

            Assert.IsTrue(form.IsDisposed);
            Assert.IsTrue(viewModel.IsDisposed);
        }

        [Test]
        public void WhenCalledBefore_ThenShowDialogThrowsException()
        {
            var serviceProvider = CreateServiceProvider(new SampleForm(), new SampleViewModel());
            var theme = new Mock<IControlTheme>();

            var view = serviceProvider.GetView<SampleForm, SampleViewModel>();
            view.Theme = theme.Object;

            view.ShowDialog(null);

            Assert.Throws<InvalidOperationException>(() => view.ShowDialog(null));
        }

        //---------------------------------------------------------------------
        // Show.
        //---------------------------------------------------------------------

        [Test]
        public void ShowBindsViewModel()
        {
            var form = new SampleForm();
            var serviceProvider = CreateServiceProvider(form, new SampleViewModel());
            var view = serviceProvider.GetView<SampleForm, SampleViewModel>();

            Assert.IsNull(form.ViewModel);

            view.Show(null);

            Assert.IsNotNull(form.ViewModel);
        }

        [Test]
        public void ShowDisposesResources()
        {
            var form = new SampleForm();
            var viewModel = new SampleViewModel();

            var serviceProvider = CreateServiceProvider(form, viewModel);
            var view = serviceProvider.GetView<SampleForm, SampleViewModel>();

            Assert.IsFalse(form.IsDisposed);
            Assert.IsFalse(viewModel.IsDisposed);

            using (view.Show(null))
            { }

            Assert.IsTrue(form.IsDisposed);
            Assert.IsTrue(viewModel.IsDisposed);
        }

        [Test]
        public void WhenCalledBefore_ThenShowThrowsException()
        {
            var serviceProvider = CreateServiceProvider(new SampleForm(), new SampleViewModel());
            var theme = new Mock<IControlTheme>();

            var view = serviceProvider.GetView<SampleForm, SampleViewModel>();
            view.Theme = theme.Object;

            view.Show(null);

            Assert.Throws<InvalidOperationException>(() => view.Show(null));
        }
    }
}
