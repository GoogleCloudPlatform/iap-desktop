//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#nullable disable

namespace Google.Solutions.IapDesktop.Application.Windows
{
    [ComVisible(false)]
    [SkipCodeCoverage("GUI plumbing")]
    public partial class ToolWindowViewBase : DockContent
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly DockPanel panel;

        /// <summary>
        /// State to use when opening/restoring the window next time.
        /// </summary>
        private DockState restoreState;

        private void UpdateRestoreState(DockState newState)
        {
            Debug.Assert(this.DesignMode || this.restoreState != DockState.Unknown);
            Debug.Assert(this.DesignMode || this.restoreState != DockState.Float);
            Debug.Assert(this.DesignMode || this.restoreState != DockState.Hidden);

            switch (newState)
            {
                case DockState.Unknown:
                case DockState.Float:
                    //
                    // We don't restore these states, ignore.
                    //
                    break;

                case DockState.Document:
                case DockState.DockTop:
                case DockState.DockLeft:
                case DockState.DockBottom:
                case DockState.DockRight:
                case DockState.DockTopAutoHide:
                case DockState.DockLeftAutoHide:
                case DockState.DockBottomAutoHide:
                case DockState.DockRightAutoHide:
                    //
                    // These are good states to restore to.
                    //
                    this.restoreState = newState;
                    break;

                case DockState.Hidden:
                    //
                    // Ignore and keep the last good restore state instead.
                    //
                    break;
            }
        }

        public ToolWindowViewBase()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;
        }

        public ToolWindowViewBase(
            IServiceProvider serviceProvider,
            DockState defaultDockState) : this()
        {
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.panel = serviceProvider.GetService<IMainWindow>().MainPanel;
            var stateRepository = serviceProvider.GetService<ToolWindowStateRepository>();

            // Read persisted window state.
            var state = stateRepository.GetSetting(
                GetType().Name, // Unique name of tool window
                defaultDockState);
            this.restoreState = state.DockState.Value;

            // Save persisted window state.
            this.Disposed += (sender, args) =>
            {
                try
                {
                    //
                    // Persist the restore state. This may or may not
                    // be the same we read during startup.
                    //
                    state.DockState.Value = this.restoreState;
                    stateRepository.SetSetting(state);
                }
                catch (Exception e)
                {
                    ApplicationTraceSource.Log.TraceWarning(
                        "Saving tool window state failed: {0}", e.Message);
                }
            };

            //
            // When a tool window contains an ActiveX control, then
            // it's possible that we receive mouse events while
            // the window is being disposed (reentrancy).
            //
            // If we let these mouse events touch the context menu, then
            // we're causing an ObjectDisposedException (b/237985825, 
            // b/238222518).
            //
            // To prevent this from happening, we have to detach the context
            // menu *before* the ActiveX is disposed. The Dispose event
            // is called *after* the ActiveX is disposed -- therefore,
            // register as a component.
            //
            this.components.Add(Disposable.For((Action)(() =>
            {
                this.TabPageContextMenu = null;
                this.TabPageContextMenuStrip = null;
            })));
        }

        //---------------------------------------------------------------------
        // Show/Hide.
        //---------------------------------------------------------------------

        public bool IsClosed { get; private set; } = false;

        public bool ShowCloseMenuItemInContextMenu
        {
            get => this.closeMenuItem.Visible;
            set => this.closeMenuItem.Visible = false;
        }

        public void CloseSafely()
        {
            if (this.HideOnClose)
            {
                Hide();
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Show or reactivate window.
        /// </summary>
        protected virtual void ShowWindow()
        {
            Debug.Assert(this.panel != null);
            Debug.Assert(this.boundWindow != null, "Window has been bound");

            this.TabText = this.Text;

            //
            // NB. IsHidden indicates that the window is not shown at all,
            // not even as auto-hide.
            //
            if (this.IsHidden)
            {
                // Show in default position.
                Show(this.panel, this.restoreState);
            }

            //
            // If the window is in auto-hide mode, simply activating
            // is not enough.
            //
            switch (this.VisibleState)
            {
                case DockState.DockTopAutoHide:
                case DockState.DockBottomAutoHide:
                case DockState.DockLeftAutoHide:
                case DockState.DockRightAutoHide:
                    this.panel.ActiveAutoHideContent = this;
                    break;
            }

            //
            // Move focus to window.
            //
            Activate();

            //
            // If an auto-hide window loses focus and closes, we fail to 
            // catch that event. 
            // To force an update, disregard the cached state and re-raise
            // the UserVisibilityChanged event.
            //
            OnUserVisibilityChanged(true);
            this.wasUserVisible = true;
        }

        public bool IsAutoHide
        {
            get
            {
                switch (this.VisibleState)
                {
                    case DockState.DockTopAutoHide:
                    case DockState.DockBottomAutoHide:
                    case DockState.DockLeftAutoHide:
                    case DockState.DockRightAutoHide:
                        return true;

                    default:
                        return false;
                }
            }
            set
            {
                switch (this.VisibleState)
                {
                    case DockState.DockTop:
                    case DockState.DockTopAutoHide:
                        this.DockState = DockState.DockTopAutoHide;
                        break;

                    case DockState.DockBottom:
                    case DockState.DockBottomAutoHide:
                        this.DockState = DockState.DockBottomAutoHide;
                        break;

                    case DockState.DockLeft:
                    case DockState.DockLeftAutoHide:
                        this.DockState = DockState.DockLeftAutoHide;
                        break;

                    case DockState.DockRight:
                    case DockState.DockRightAutoHide:
                        this.DockState = DockState.DockRightAutoHide;
                        break;
                }
            }
        }

        public bool IsDocked
        {
            get
            {
                switch (this.VisibleState)
                {
                    case DockState.DockTop:
                    case DockState.DockBottom:
                    case DockState.DockLeft:
                    case DockState.DockRight:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool IsDockable => this.IsDocked || this.IsAutoHide || this.IsFloat;

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void closeMenuItem_Click(object sender, System.EventArgs e)
        {
            CloseSafely();
        }

        private void ToolWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.DockState != DockState.Document && e.Shift && e.KeyCode == Keys.Escape)
            {
                CloseSafely();
            }
        }

        //---------------------------------------------------------------------
        // Track visibility.
        //
        // NB. The DockPanel library does not provide good properties or evens 
        // that would allow you to determine whether a window is effectively
        // visible to the user or not.
        //
        // This table shows the value of key properties based on the window state:
        //
        // 
        // ---------------------------------------------------------------------------------------
        //           |                     |             |         |                   | Pane.ActiveContent
        //           | State               | IsActivated | IsFloat | Visible/DockState | == this
        // ---------------------------------------------------------------------------------------
        // Float     | Single pane         | (any)       | TRUE    | Float             | TRUE    
        //           | Split pane, focus   | FALSE       | TRUE    | Float             | TRUE    
        //           | Split pane, no focus| TRUE        | TRUE    | Float             | TRUE    
        //           | Background          | FALSE       | TRUE    | Float             | FALSE
        // ---------------------------------------------------------------------------------------
        // AutoHide  | Single              | (any)       | FALSE   | DockRightAutoHide | TRUE    
        //           | Background          | (any)       | FALSE   | DockRightAutoHide | TRUE
        // ---------------------------------------------------------------------------------------
        // Dock      | Single pane         | TRUE        | FALSE   | DockRight         | TRUE    
        //           | Split pane, focus   | FALSE (!)   | FALSE   | DockRight         | TRUE    
        //           | Split pane, no focus| TRUE  (!)   | FALSE   | DockRight         | TRUE    
        //           | Background          | FALSE       | FALSE   | DockRight         | FALSE
        // -----------------------------------------------------------------------------------------
        //
        // IsHidden is TRUE during construction, and FALSE ever after.
        // When docked and hidden, the size is reset to (0, 0)
        //

        protected bool IsInBackground =>
            (this.IsFloat && this.Pane.ActiveContent != this) ||
            (this.IsAutoHide && this.Size.Height == 0 && this.Size.Width == 0) ||
            (this.IsDocked && this.Pane.ActiveContent != this);

        protected bool IsUserVisible => !this.IsHidden && !this.IsInBackground;
        private bool wasUserVisible = false;

        private void RaiseUserVisibilityChanged()
        {
            // Only call OnUserVisibilityChanged if there really was a change.
            if (this.IsUserVisible != this.wasUserVisible)
            {
                OnUserVisibilityChanged(this.IsUserVisible);
                this.wasUserVisible = this.IsUserVisible;
            }
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);

            UpdateRestoreState(this.DockState);
            RaiseUserVisibilityChanged();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);

            UpdateRestoreState(this.DockState);
            RaiseUserVisibilityChanged();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            UpdateRestoreState(this.DockState);
            RaiseUserVisibilityChanged();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.IsClosed = false;

            UpdateRestoreState(this.DockState);
            RaiseUserVisibilityChanged();
        }

        protected override void OnClosed(EventArgs e)
        {
            // NB. This method might be invoked more than once if a disconnect
            // event coincides (which is reasnably common when closing the app
            // with active sessions).

            base.OnClosed(e);

            this.IsClosed = true;
        }

        protected virtual void OnUserVisibilityChanged(bool visible)
        {
            // Can be overridden in derived class.
        }

        //---------------------------------------------------------------------
        // Factory and MVVM binding.
        //---------------------------------------------------------------------

        private object boundWindow;

        /// <summary>
        /// Gets or creates a MVVM-enabled tool window and prepares it for viewing. 
        /// Callers have the opportunity to customize the view model before calling
        /// .Show() on the returned object.
        /// </summary>
        internal static BoundToolWindow<TToolWindowView, TToolWindowViewModel> GetToolWindow<TToolWindowView, TToolWindowViewModel>(
            IServiceProvider serviceProvider)
            where TToolWindowView : ToolWindowViewBase, IView<TToolWindowViewModel>
            where TToolWindowViewModel : ViewModelBase
        {
            //
            // NB. ToolWindows can be singletons, and we must not bind them
            // multiple times.
            //
            var view = serviceProvider.GetService<TToolWindowView>();
            if (view.boundWindow != null)
            {
                //
                // This is a singleton and it has been bound before.
                //
                return (BoundToolWindow<TToolWindowView, TToolWindowViewModel>)view.boundWindow;
            }
            else
            {
                //
                // This is new object (transient or singleton), and it
                // has not been bound yet.
                //
                // Create an intermediate object that lets the caller initialize the
                // view model before calling Show().
                //
                var boundWindow = new BoundToolWindow<TToolWindowView, TToolWindowViewModel>(
                    view,
                    serviceProvider.GetService<TToolWindowViewModel>(),
                    serviceProvider.GetService<IBindingContext>(),
                    serviceProvider.GetService<IThemeService>().ToolWindowTheme);
                view.boundWindow = boundWindow;

                if (view.HideOnClose)
                {
                    Debug.Assert(
                        ((ServiceRegistry)serviceProvider).Registrations[typeof(TToolWindowView)] == ServiceLifetime.Singleton,
                        "HideOnClose windows should be singletons");
                }

                return boundWindow;
            }
        }

        internal class BoundToolWindow<TToolWindowView, TToolWindowViewModel>
            : IToolWindow<TToolWindowView, TToolWindowViewModel>
            where TToolWindowView : ToolWindowViewBase, IView<TToolWindowViewModel>
            where TToolWindowViewModel : ViewModelBase
        {
            private bool bound = false;

            private readonly IControlTheme theme;
            private readonly TToolWindowView view;
            private readonly IBindingContext bindingContext;

            public BoundToolWindow(
                TToolWindowView view,
                TToolWindowViewModel viewModel,
                IBindingContext bindingContext,
                IControlTheme theme)
            {
                this.view = view.ExpectNotNull(nameof(view));
                this.ViewModel = viewModel.ExpectNotNull(nameof(viewModel));
                this.bindingContext = bindingContext.ExpectNotNull(nameof(bindingContext));
                this.theme = theme;
            }

            public TToolWindowViewModel ViewModel { get; }

            /// <summary>
            /// Explicitly perform a bind to access the view. Prefer
            /// to call Show() instead.
            /// </summary>
            public TToolWindowView Bind()
            {
                if (!this.bound)
                {
                    //
                    // The caller had sufficient opportunity to initialize
                    // the view mode, so we can now bind it to the view.
                    //
                    Window<TToolWindowView, TToolWindowViewModel>.Bind(
                        this.view,
                        this.ViewModel,
                        this.theme,
                        this.bindingContext);

                    this.bound = true;
                }

                return this.view;
            }

            /// <summary>
            /// Bind and show the tool window.
            /// </summary>
            public void Show()
            {
                Bind();

                this.view.ShowWindow();
            }
        }

        //---------------------------------------------------------------------
        // Utility methods.
        //---------------------------------------------------------------------

        protected async Task InvokeActionAsync(
            Func<Task> action,
            string actionName)
        {
            Debug.Assert(
                actionName.Contains("ing "),
                "Action name should be formatted like 'Doing something'");

            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Ignore.
            }
            catch (Exception e)
            {
                this.exceptionDialog
                    .Show(this, $"{actionName} failed", e);
            }
        }
    }
}