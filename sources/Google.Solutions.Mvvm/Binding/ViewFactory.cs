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
using Google.Solutions.Mvvm.Theme;
using System;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// Factory for creating dialogs.
    /// </summary>
    /// <typeparam name="TView"></typeparam>
    /// <typeparam name="TViewModel"></typeparam>
    public interface IDialogFactory<TView, TViewModel>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        /// <summary>
        /// Get or set the theme to apply, optional.
        /// </summary>
        IControlTheme? Theme { get; set; }

        /// <summary>
        /// Create dialog using an existing view model.
        /// </summary>
        IDialog<TView, TViewModel> CreateDialog(TViewModel viewModel);

        /// <summary>
        /// Create dialog.
        /// </summary>
        IDialog<TView, TViewModel> CreateDialog();
    }

    /// <summary>
    /// Factory for creating Views of the same kind. A new instance
    /// and view model is used for each View.
    /// </summary>
    public class ViewFactory<TView, TViewModel> : IDialogFactory<TView, TViewModel>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        private TViewModel CreateViewModel()
        {
            return (TViewModel)this.serviceProvider.GetService(typeof(TViewModel));
        }

        /// <summary>
        /// Get or set the theme to apply, optional.
        /// </summary>
        public IControlTheme? Theme { get; set; }

        internal ViewFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider.ExpectNotNull(nameof(serviceProvider));
        }

        public IDialog<TView, TViewModel> CreateDialog(TViewModel viewModel)
        {
            var view = new Dialog<TView, TViewModel>(this.serviceProvider, viewModel);

            if (this.Theme != null)
            {
                view.Theme = this.Theme;
            }

            return view;
        }

        public IDialog<TView, TViewModel> CreateDialog()
        {
            return CreateDialog(CreateViewModel());
        }

        public Window<TView, TViewModel> CreateWindow()
        {
            var view = new Window<TView, TViewModel>(
                this.serviceProvider,
                (TViewModel)this.serviceProvider.GetService(typeof(TViewModel)));

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
    public interface IDialog<TView, TViewModel> : IDisposable
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        /// <summary>
        /// Get or set the theme to apply.
        /// </summary>
        IControlTheme? Theme { get; set; }

        /// <summary>
        /// View model used by the dialog.
        /// </summary>
        TViewModel ViewModel { get; }

        /// <summary>
        /// Show the dialog.
        /// </summary>
        DialogResult ShowDialog(IWin32Window? parent);
    }

    public sealed class Dialog<TView, TViewModel> : IDialog<TView, TViewModel>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        private bool shown;

        public TViewModel ViewModel { get; }

        public IControlTheme? Theme { get; set; }

        internal Dialog(IServiceProvider serviceProvider, TViewModel viewModel)
        {
            this.serviceProvider = serviceProvider;
            this.ViewModel = viewModel;
        }

        /// <summary>
        /// Show dialog. Disposes the view and view model afterwards.
        /// </summary>
        public DialogResult ShowDialog(IWin32Window? parent)
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

            var bindingContext = (IBindingContext)
                this.serviceProvider.GetService(typeof(IBindingContext));
            if (bindingContext == null)
            {
                throw new BindingException("Binding context not available");
            }

            //
            // Create view, show, and dispose it.
            //
            using (var view = (TView)this.serviceProvider.GetService(typeof(TView)))
            {
                view.SuspendLayout();

                this.Theme?.ApplyTo(view);
                if (this.Theme != null &&
                    view is IThemedView<TViewModel> themedView && themedView != null)
                {
                    themedView.SetTheme(this.Theme);
                }

                //
                // Bind view <-> view model.
                //
                this.ViewModel.Bind(view);
                view.Bind(
                    this.ViewModel,
                    bindingContext);
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

    public sealed class Window<TView, TViewModel>
        where TView : class, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        private TView? form;

        public TViewModel ViewModel { get; }

        /// <summary>
        /// Get or set the theme to apply, optional.
        /// </summary>
        public IControlTheme? Theme { get; set; }

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
            IControlTheme? theme,
            IBindingContext bindingContext)
        {
            var viewControl = (ContainerControl)(object)view;

            viewControl.SuspendLayout();

            theme?.ApplyTo(viewControl);
            if (theme != null &&
                view is IThemedView<TViewModel> themedView && themedView != null)
            {
                themedView.SetTheme(theme);
            }

            //
            // Bind view <-> view model.
            //
            viewModel.Bind(viewControl);
            view.Bind(viewModel, bindingContext);
            viewControl.ResumeLayout();

            //
            // Tie lifetime of the view model to that of the view.
            //
            var disposed = false;
            viewControl.Disposed += (_, __) =>
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
                    if (view == null)
                    {
                        throw new BindingException($"No view of type {typeof(TView)} available");
                    }

                    var bindingContext = (IBindingContext)
                        this.serviceProvider.GetService(typeof(IBindingContext));
                    if (bindingContext == null)
                    {
                        throw new BindingException("Binding context not available");
                    }

                    Bind(view, this.ViewModel, this.Theme, bindingContext);

                    this.form = view;
                }

                return this.form;
            }
        }
    }
}
