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

using Google.Solutions.Mvvm.Theme;
using System;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// View-related extension methods for IServiceProvider.
    /// </summary>
    public static class ViewServiceProviderExtensions
    {
        /// <summary>
        /// Returns a factory for MVVM-enabled Views. The View class
        /// and the view model are created using the service provider.
        /// </summary>
        public static WindowFactory<TView, TViewModel> GetViewFactory<TView, TViewModel>(
            this IServiceProvider serviceProvider)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            return new WindowFactory<TView, TViewModel>(serviceProvider);
        }

        //---------------------------------------------------------------------
        // Dialogs.
        //---------------------------------------------------------------------

        /// <summary>
        /// Create an MVVM-enabled dialog and view model using the service provider.
        /// </summary>
        public static IDialogWindow<TView, TViewModel> GetDialog<TView, TViewModel>(
            this IServiceProvider serviceProvider)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            return GetViewFactory<TView, TViewModel>(serviceProvider).CreateDialog();
        }

        /// <summary>
        /// Create an MVVM-enabled dialog and view model using the service provider.
        /// </summary>
        public static IDialogWindow<TView, TViewModel> GetDialog<TView, TViewModel>(
            this IServiceProvider serviceProvider,
            IControlTheme theme)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            var view = GetDialog<TView, TViewModel>(serviceProvider);
            view.Theme = theme;
            return view;
        }

        //---------------------------------------------------------------------
        // Windows.
        //---------------------------------------------------------------------

        /// <summary>
        /// Create an MVVM-enabled window and view model using the service provider.
        /// </summary>
        public static ITopLevelWindow<TView, TViewModel> GetWindow<TView, TViewModel>(
            this IServiceProvider serviceProvider)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            return GetViewFactory<TView, TViewModel>(serviceProvider).CreateWindow();
        }

        /// <summary>
        /// Create an MVVM-enabled window and view model using the service provider.
        /// </summary>
        public static ITopLevelWindow<TView, TViewModel> GetWindow<TView, TViewModel>(
            this IServiceProvider serviceProvider,
            IControlTheme theme)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            var view = GetWindow<TView, TViewModel>(serviceProvider);
            view.Theme = theme;
            return view;
        }
    }
}
