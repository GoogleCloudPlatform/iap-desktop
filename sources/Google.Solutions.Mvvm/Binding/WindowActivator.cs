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
using Google.Solutions.Mvvm.Theme;
using System;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// Window that can be shown as a dialog.
    /// </summary>
    public interface IWindow<TViewModel>
        where TViewModel : ViewModelBase
    {
        /// <summary>
        /// View model used by the window.
        /// </summary>
        TViewModel ViewModel { get; }
    }

    /// <summary>
    /// Window that can be shown as a dialog.
    /// </summary>
    public interface IDialogWindow<TView, TViewModel> : IWindow<TViewModel>, IDisposable
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        /// <summary>
        /// Show the dialog.
        /// </summary>
        DialogResult ShowDialog(IWin32Window? parent);
    }

    /// <summary>
    /// Window that can be shown as a top-level window.
    /// </summary>
    public interface ITopLevelWindow<TView, TViewModel> : IWindow<TViewModel>
        where TView : class, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        /// <summary>
        /// Window form.
        /// </summary>
        TView Form { get; }
    }

    public class WindowActivator<TView, TViewModel, TTheme> : IActivator<IWindow<TViewModel>>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
        where TTheme : IControlTheme
    {
        private readonly IActivator<TView> viewActivator;
        private readonly IActivator<TViewModel> viewModelActivator;
        private readonly TTheme theme;
        private readonly IBindingContext bindingContext;

        public WindowActivator(
            IActivator<TView> viewActivator,
            IActivator<TViewModel> viewModelActivator,
            TTheme theme,
            IBindingContext bindingContext)
        {
            this.viewActivator = viewActivator;
            this.viewModelActivator = viewModelActivator;
            this.theme = theme;
            this.bindingContext = bindingContext;
        }

        public virtual IDialogWindow<TView, TViewModel> CreateDialog(TViewModel viewModel)
        {
            return new DialogWindow<TView, TViewModel, TTheme>(
                this.viewActivator,
                viewModel,
                this.theme,
                this.bindingContext);
        }

        public virtual IDialogWindow<TView, TViewModel> CreateDialog()
        {
            return new DialogWindow<TView, TViewModel, TTheme>(
                this.viewActivator,
                this.viewModelActivator.Activate(),
                this.theme,
                this.bindingContext);
        }

        public virtual ITopLevelWindow<TView, TViewModel> CreateWindow()
        {
            return new TopLevelWindow<TView, TViewModel, TTheme>(
                this.viewActivator,
                this.viewModelActivator.Activate(),
                this.theme,
                this.bindingContext);
        }

        public IWindow<TViewModel> Activate()
        {
            return CreateWindow();
        }
    }

    internal sealed class DialogWindow<TView, TViewModel, TTheme> : IDialogWindow<TView, TViewModel>
        where TView : Form, IView<TViewModel>
        where TViewModel : ViewModelBase
        where TTheme : IControlTheme
    {
        private readonly IActivator<TView> viewActivator;
        private readonly TTheme theme;
        private readonly IBindingContext bindingContext;

        private bool shown;

        public TViewModel ViewModel { get; }

        internal DialogWindow(
            IActivator<TView> viewActivator,
            TViewModel viewModel,
            TTheme theme,
            IBindingContext bindingContext)
        {
            this.viewActivator = viewActivator;
            this.ViewModel = viewModel;
            this.theme = theme;
            this.bindingContext = bindingContext;
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

            //
            // Create view, show, and dispose it.
            //
            using (var view = this.viewActivator.Activate())
            {
                view.SuspendLayout();

                this.theme.ApplyTo(view);
                if (view is IThemedView themedView && themedView != null)
                {
                    themedView.SetTheme(this.theme);
                }

                //
                // Bind view <-> view model.
                //
                this.ViewModel.Bind(view);
                view.Bind(
                    this.ViewModel,
                    this.bindingContext);
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

    public sealed class TopLevelWindow<TView, TViewModel, TTheme> : ITopLevelWindow<TView, TViewModel>
        where TView : class, IView<TViewModel>
        where TViewModel : ViewModelBase
        where TTheme : IControlTheme
    {
        private readonly IActivator<TView> viewActivator;
        private readonly TTheme theme;
        private readonly IBindingContext bindingContext;

        private TView? form;

        public TViewModel ViewModel { get; }

        internal TopLevelWindow(
            IActivator<TView> viewActivator,
            TViewModel viewModel,
            TTheme theme,
            IBindingContext bindingContext)
        {
            this.viewActivator = viewActivator;
            this.ViewModel = viewModel;
            this.theme = theme;
            this.bindingContext = bindingContext;
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

            if (theme != null)
            {
                theme.ApplyTo(viewControl);
                if (view is IThemedView themedView && themedView != null)
                {
                    themedView.SetTheme(theme);
                }
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
                    var view = this.viewActivator.Activate();
                    if (view == null)
                    {
                        throw new BindingException($"No view of type {typeof(TView)} available");
                    }

                    Bind(view, this.ViewModel, this.theme, this.bindingContext);

                    this.form = view;
                }

                return this.form;
            }
        }
    }
}
