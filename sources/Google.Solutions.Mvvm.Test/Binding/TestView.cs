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

using Google.Solutions.Common.Runtime;
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
            public uint DisposeCount = 0;

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                this.DisposeCount++;
            }
        }

        private class SampleView : Form, IView<SampleViewModel>
        {
            public SampleViewModel? ViewModel { get; private set; }

            public void Bind(SampleViewModel viewModel, IBindingContext bindingContext)
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

        private static WindowActivator<SampleView, SampleViewModel, IControlTheme> CreateActivator(
            SampleView view,
            SampleViewModel viewModel,
            IControlTheme theme)
        {
            return new WindowActivator<SampleView, SampleViewModel, IControlTheme>(
                InstanceActivator.Create(view),
                InstanceActivator.Create(viewModel),
                theme,
                new Mock<IBindingContext>().Object);
        }

        //---------------------------------------------------------------------
        // Dialog.
        //---------------------------------------------------------------------

        [Test]
        public void ShowDialog_WhenFormUsedAsDialog_ThenShowDialogAppliesTheme()
        {
            if (!Environment.UserInteractive)
            {
                Assert.Inconclusive("Test can only be run in an interactive session");
            }

            var theme = new Mock<IControlTheme>();
            var activator = CreateActivator(
                new SampleView(),
                new SampleViewModel(),
                theme.Object);

            using (var dialog = activator.CreateDialog())
            {
                dialog.ShowDialog(null);
            }

            theme.Verify(t => t.ApplyTo(It.IsAny<Control>()), Times.Once);
        }

        [Test]
        public void ShowDialog_WhenFormUsedAsDialog_ThenShowDialogBindsViewModel()
        {
            if (!Environment.UserInteractive)
            {
                Assert.Inconclusive("Test can only be run in an interactive session");
            }

            var form = new SampleView();
            var activator = CreateActivator(
                form,
                new SampleViewModel(),
                new Mock<IControlTheme>().Object);

            using (var dialog = activator.CreateDialog())
            {
                Assert.IsNull(form.ViewModel);

                dialog.ShowDialog(null);
            }

            Assert.IsNotNull(form.ViewModel);
        }

        [Test]
        public void ShowDialog_WhenFormUsedAsDialog_ThenShowDialogCanOnlyBeCalledOnce()
        {
            if (!Environment.UserInteractive)
            {
                Assert.Inconclusive("Test can only be run in an interactive session");
            }

            var activator = CreateActivator(
                new SampleView(),
                new SampleViewModel(),
                new Mock<IControlTheme>().Object);

            using (var dialog = activator.CreateDialog())
            {
                dialog.ShowDialog(null);

                Assert.Throws<InvalidOperationException>(() => dialog.ShowDialog(null));
            }
        }

        [Test]
        public void ShowDialog_WhenFormUsedAsDialog_ThenDialogDisposesViewModel()
        {
            if (!Environment.UserInteractive)
            {
                Assert.Inconclusive("Test can only be run in an interactive session");
            }

            var form = new SampleView();
            var viewModel = new SampleViewModel();
            var activator = CreateActivator(
                form,
                viewModel,
                new Mock<IControlTheme>().Object);

            using (var dialog = activator.CreateDialog())
            {
                Assert.IsFalse(form.IsDisposed);
                Assert.IsFalse(viewModel.IsDisposed);

                dialog.ShowDialog(null);
            }

            Assert.IsTrue(form.IsDisposed);
            Assert.IsTrue(viewModel.IsDisposed);
        }

        //---------------------------------------------------------------------
        // Windows.
        //---------------------------------------------------------------------

        [Test]
        public void CreateWindow_WhenFormUsedAsWindow_ThenFormAppliesTheme()
        {
            var theme = new Mock<IControlTheme>();
            var activator = CreateActivator(
                new SampleView(),
                new SampleViewModel(),
                theme.Object);

            var f = activator.CreateWindow().Form;

            theme.Verify(t => t.ApplyTo(It.IsAny<Control>()), Times.Once);
        }

        [Test]
        public void CreateWindow_WhenFormUsedAsWindow_ThenFormBindsViewModel()
        {
            var form = new SampleView();
            var viewModel = new SampleViewModel();
            var activator = CreateActivator(
                form,
                viewModel,
                new Mock<IControlTheme>().Object);

            var window = activator.CreateWindow();

            Assert.AreSame(form, window.Form);
            Assert.AreSame(window.Form, viewModel.View);
            Assert.AreSame(viewModel, window.Form.ViewModel);
        }

        [Test]
        public void CreateWindow_WhenFormUsedAsDialog_ThenCloseFormDisposesViewModel()
        {
            var form = new SampleView();
            var viewModel = new SampleViewModel();
            var activator = CreateActivator(
                form,
                viewModel,
                new Mock<IControlTheme>().Object);

            var window = activator.CreateWindow();
            Assert.IsFalse(form.IsDisposed);
            Assert.IsFalse(viewModel.IsDisposed);

            window.Form.Show();
            window.Form.Close();

            Assert.IsTrue(form.IsDisposed);
            Assert.IsTrue(viewModel.IsDisposed);
        }

        [Test]
        public void CreateWindow_WhenFormDisposedTwice_ThenViewModelIsUnboundOnce()
        {
            var form = new SampleView();
            var viewModel = new SampleViewModel();
            var activator = CreateActivator(
                form,
                viewModel,
                new Mock<IControlTheme>().Object);

            var window = activator.CreateWindow();
            window.Form.Dispose();
            window.Form.Dispose();

            Assert.AreEqual(1, viewModel.DisposeCount);
            Assert.IsNull(viewModel.View);
        }
    }
}
