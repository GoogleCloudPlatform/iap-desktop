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

using Google.Apis.Util;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Windows
{
    /// <summary>
    /// MVVM view.
    /// </summary>
    public interface IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        /// <summary>
        /// Bind view model to model. This method is only
        /// called once, briefly after construction.
        /// </summary>
        void Bind(TViewModel viewModel);

        DialogResult Show(IWin32Window parent);
    }

    /// <summary>
    /// View-related extension methods for IServiceProvider.
    /// </summary>
    public static class ViewExtensions
    {
        /// <summary>
        /// Returns a factory for MVVM-enabled Views. The View class
        /// and the view model are created using the service provider.
        /// </summary>
        public static ViewFactory<TView, TViewModel> GetViewFactory<TView, TViewModel>(
            this IServiceProvider serviceProvider)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            return new ViewFactory<TView, TViewModel>(serviceProvider);
        }

        /// <summary>
        /// Create an MVVM-enabled View and view model using the service provider.
        /// </summary>
        public static View<TView, TViewModel> GetView<TView, TViewModel>(
            this IServiceProvider serviceProvider)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            return GetViewFactory<TView, TViewModel>(serviceProvider).Create();
        }

        /// <summary>
        /// Create an MVVM-enabled View using the service provider, and bind
        /// a custom view model.
        /// </summary>
        public static View<TView, TViewModel> GetView<TView, TViewModel>(
            this IServiceProvider serviceProvider,
            TViewModel viewModel)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            serviceProvider.ThrowIfNull(nameof(serviceProvider));
            viewModel.ThrowIfNull(nameof(viewModel));

            return new View<TView, TViewModel>(serviceProvider, viewModel);
        }
    }

    /// <summary>
    /// Factory for creating Views of the same kind. A new instance
    /// and view model is used for each View.
    /// </summary>
    public class ViewFactory<TView, TViewModel>
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        internal ViewFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider.ThrowIfNull(nameof(serviceProvider));
        }

        public View<TView, TViewModel> Create()
        {
            return new View<TView, TViewModel>(
                serviceProvider,
                (TViewModel)serviceProvider.GetService(typeof(TViewModel)));
        }
    }

    /// <summary>
    /// View that has been "hydrated" with a view model and is ready to
    /// be shown.
    /// </summary>
    public class View<TView, TViewModel>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        public TViewModel ViewModel { get; }

        public IControlTheme Theme { get; set; }

        public bool Shown { get; private set; } = false;

        internal View(IServiceProvider serviceProvider, TViewModel viewModel)
        {
            this.serviceProvider = serviceProvider;
            this.ViewModel = viewModel;
        }

        private void CheckNotShownBefore()
        {
            if (this.Shown)
            {
                throw new InvalidOperationException("View can only be shown once");
            }

            this.Shown = true;
        }

        /// <summary>
        /// Show dialog. Disposes the view and view model afterwards.
        /// </summary>
        public DialogResult ShowDialog(IWin32Window parent)
        {
            CheckNotShownBefore();

            //
            // Create view.
            //
            using (this.ViewModel as IDisposable)
            using (var view = (TView)this.serviceProvider.GetService(typeof(TView)))
            {
                this.Theme?.ApplyTo(view);

                //
                // Bind view <-> view model.
                //
                this.ViewModel.View = view;
                view.Bind(this.ViewModel);

                var result = view.ShowDialog(parent);

                //
                // Retain view model so that the caller can extract
                // results from it, but avoid it from keeping the view
                // alive.
                //
                this.ViewModel.View = null;

                return result;
            }
        }

        /// <summary>
        /// Show form.
        /// </summary>
        public TView Show(IWin32Window parent)
        {
            CheckNotShownBefore();

            //
            // Create view.
            //
            var view = (TView)this.serviceProvider.GetService(typeof(TView));
            this.Theme?.ApplyTo(view);

            //
            // Bind view <-> view model.
            //
            this.ViewModel.View = view;
            view.Bind(this.ViewModel);

            //
            // Dispose view model when form is disposed.
            //
            if (this.ViewModel is IDisposable disposableViewModel)
            {
                view.Disposed += (_, __) => disposableViewModel.Dispose();
            }

            //
            // Show the form and leave it to the caller to decide when to close
            // and dispose it.
            //
            view.Show(parent);
            return view;
        }
    }
}
