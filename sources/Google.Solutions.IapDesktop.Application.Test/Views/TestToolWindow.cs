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


using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Views
{
    [TestFixture]
    public class TestToolWindow : ApplicationFixtureBase
    {
        private class SampleView : ToolWindow, IView<SampleViewModel>
        {
            public int BindCalls = 0;

            public void Bind(SampleViewModel viewModel, IBindingContext bindingContext)
            {
                Assert.IsNotNull(viewModel);
                this.BindCalls++;
            }
        }

        private class SampleViewModel : ViewModelBase
        {
        }

        //---------------------------------------------------------------------
        // Factory and MVVM binding.
        //---------------------------------------------------------------------

        [Test]
        public void WhenViewIsSingleton_ThenViewIsBoundOnlyOnce()
        {
            var registry = new ServiceRegistry();
            registry.AddMock<IThemeService>();
            registry.AddMock<IBindingContext>();
            registry.AddSingleton<SampleView>();
            registry.AddTransient<SampleViewModel>();

            var window1 = ToolWindow.GetWindow<SampleView, SampleViewModel>(registry);
            var window2 = ToolWindow.GetWindow<SampleView, SampleViewModel>(registry);

            var view1 = window1.Bind();
            var view2 = window2.Bind();

            Assert.AreSame(window1, window2);
            Assert.AreEqual(1, view1.BindCalls);
            Assert.AreEqual(1, view2.BindCalls);
        }

        [Test]
        public void WhenViewIsTransient_ThenEachInstanceIsBound()
        {
            var registry = new ServiceRegistry();
            registry.AddMock<IThemeService>();
            registry.AddMock<IBindingContext>();
            registry.AddTransient<SampleView>();
            registry.AddTransient<SampleViewModel>();

            var window1 = ToolWindow.GetWindow<SampleView, SampleViewModel>(registry);
            var window2 = ToolWindow.GetWindow<SampleView, SampleViewModel>(registry);

            var view1 = window1.Bind();
            var view2 = window2.Bind();

            Assert.AreNotSame(window1, window2);
            Assert.AreEqual(1, view1.BindCalls);
            Assert.AreEqual(1, view2.BindCalls);
        }
    }
}
