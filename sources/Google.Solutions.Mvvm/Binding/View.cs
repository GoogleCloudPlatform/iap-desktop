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

using Google.Apis.Util;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
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

        //---------------------------------------------------------------------
        // Dialogs.
        //---------------------------------------------------------------------

        /// <summary>
        /// Create an MVVM-enabled dialog and view model using the service provider.
        /// </summary>
        public static Dialog<TView, TViewModel> GetDialog<TView, TViewModel>(
            this IServiceProvider serviceProvider)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            return GetViewFactory<TView, TViewModel>(serviceProvider).CreateDialog();
        }

        /// <summary>
        /// Create an MVVM-enabled dialog and view model using the service provider.
        /// </summary>
        public static Dialog<TView, TViewModel> GetDialog<TView, TViewModel>(
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
        public static Window<TView, TViewModel> GetWindow<TView, TViewModel>(
            this IServiceProvider serviceProvider)
            where TView : Form, IView<TViewModel>
            where TViewModel : ViewModelBase
        {
            return GetViewFactory<TView, TViewModel>(serviceProvider).CreateWindow();
        }

        /// <summary>
        /// Create an MVVM-enabled window and view model using the service provider.
        /// </summary>
        public static Window<TView, TViewModel> GetWindow<TView, TViewModel>(
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

    /// <summary>
    /// Factory for creating Views of the same kind. A new instance
    /// and view model is used for each View.
    /// </summary>
    public class ViewFactory<TView, TViewModel>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        private TViewModel CreateViewModel()
        {
            return (TViewModel)this.serviceProvider.GetService(typeof(TViewModel));
        }

        public IControlTheme Theme { get; set; }

        internal ViewFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider.ThrowIfNull(nameof(serviceProvider));
        }

        public Dialog<TView, TViewModel> CreateDialog(TViewModel viewModel)
        {
            var view = new Dialog<TView, TViewModel>(serviceProvider, viewModel);

            if (this.Theme != null)
            {
                view.Theme = this.Theme;
            }

            return view;
        }

        public Dialog<TView, TViewModel> CreateDialog()
        {
            return CreateDialog(CreateViewModel());
        }

        public Window<TView, TViewModel> CreateWindow()
        {
            var view = new Window<TView, TViewModel>(
                serviceProvider,
                (TViewModel)serviceProvider.GetService(typeof(TViewModel)));

            if (this.Theme != null)
            {
                view.Theme = this.Theme;
            }

            return view;
        }
    }

    /// <summary>
    /// Hydrated view that can be shown as a dialog.
    /// </summary>
    public sealed class Dialog<TView, TViewModel> : IDisposable
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        private bool shown;

        public TViewModel ViewModel { get; }

        public IControlTheme Theme { get; set; }

        internal Dialog(IServiceProvider serviceProvider, TViewModel viewModel)
        {
            this.serviceProvider = serviceProvider;
            this.ViewModel = viewModel;
        }

        /// <summary>
        /// Show dialog. Disposes the view and view model afterwards.
        /// </summary>
        public DialogResult ShowDialog(IWin32Window parent)
        {
            if (this.shown)
            {
                throw new InvalidOperationException(
                    "ShowDialog must not be called more than once, or when the" +
                    "form has been accessed before");
            }
            else
            {
                this.shown = true;
            }

            //
            // Create view, show, and dispose it.
            //
            using (var view = (TView)this.serviceProvider.GetService(typeof(TView)))
            {
                view.SuspendLayout();

                this.Theme?.ApplyTo(view);

                //
                // Bind view <-> view model.
                //
                this.ViewModel.Bind(view);
                view.Bind(this.ViewModel);
                view.ResumeLayout();

                var result = view.ShowDialog(parent);

                //
                // Retain the view model, but prevent it from keeping the view alive.
                //
                this.ViewModel.Unbind();

                return result;
            }
        }

        public void Dispose()
        {
            this.ViewModel.Dispose();
        }
    }

    /// <summary>
    /// Hydrated view that can be used as a standalone window.
    /// </summary>
    public sealed class Window<TView, TViewModel>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        private TView form;

        public TViewModel ViewModel { get; }

        public IControlTheme Theme { get; set; }

        internal Window(IServiceProvider serviceProvider, TViewModel viewModel)
        {
            this.serviceProvider = serviceProvider;
            this.ViewModel = viewModel;
        }

        /// <summary>
        /// Helper method to bind and initialize a view.
        /// </summary>
        public static void Bind(
            TView view,
            TViewModel viewModel,
            IControlTheme theme)
        {
            view.SuspendLayout();

            theme?.ApplyTo(view);

            //
            // Bind view <-> view model.
            //
            viewModel.Bind(view);
            view.Bind(viewModel);
            view.ResumeLayout();

            //
            // Tie lifetime of the view model to that of the view.
            //
            bool disposed = false;
            view.Disposed += (_, __) =>
            {
                if (!disposed)
                {
                    viewModel.Unbind();
                    viewModel.Dispose();
                    disposed = true;
                }
            };
        }

        public TView Form
        {
            get
            {
                if (this.form == null)
                {
                    //
                    // Create view and bind it.
                    //
                    var view = (TView)this.serviceProvider.GetService(typeof(TView));

                    Bind(view, this.ViewModel, this.Theme);

                    this.form = view;
                }

                return this.form;
            }
        }
    }
}