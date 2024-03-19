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
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    /// <summary>
    /// Base class for menu commands.
    /// </summary>
    public abstract class MenuCommandBase<TContext> : CommandBase, IContextCommand<TContext>
    {
        /// <summary>
        /// Type of command. When set to ToolbarCommand, the command is
        /// never reported as Unavailable, but only as Disabled.
        /// </summary>
        public MenuCommandType CommandType { get; set; } = MenuCommandType.MenuCommand;

        /// <summary>
        /// Check if command should be made available for this context.
        /// </summary>
        /// <param name="context"></param>
        protected abstract bool IsAvailable(TContext context);

        /// <summary>
        /// Check if command a command that's available should be enabled.
        /// </summary>
        /// <param name="context"></param>
        protected abstract bool IsEnabled(TContext context);

        public MenuCommandBase(string text)
            : base(text)
        {
            Debug.Assert(text.Contains("&"), "Command text should have a mnemonic");
        }

        //---------------------------------------------------------------------
        // IContextCommand.
        //---------------------------------------------------------------------

        public virtual Image? Image { get; set; } = null;

        public virtual Keys ShortcutKeys { get; set; } = Keys.None;

        public virtual bool IsDefault { get; set; } = false;

        public CommandState QueryState(TContext context)
        {
            if (!IsAvailable(context))
            {
                if (this.CommandType == MenuCommandType.ToolbarCommand)
                {
                    //
                    // Report as Disabled even if it's Unavailable.
                    // This is needed for toolbar commands to prevent
                    // icons from being rearranged when the context
                    // changes.
                    //
                    return CommandState.Disabled;
                }
                else
                {
                    return CommandState.Unavailable;
                }
            }
            else if (IsEnabled(context))
            {
                return CommandState.Enabled;
            }
            else
            {
                return CommandState.Disabled;
            }
        }

        public virtual Task ExecuteAsync(TContext context)
        {
            Execute(context);
            return Task.CompletedTask;
        }

        public virtual void Execute(TContext context)
        {
        }
    }

    public class OpenToolWindowCommand<TContext, TView, TViewModel> : MenuCommandBase<TContext>
        where TView : ToolWindowViewBase, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        private readonly Func<TContext, bool> isAvailableFunc;
        private readonly Func<TContext, bool> isEnabledFunc;
        private readonly IToolWindowHost toolWindowHost;

        public OpenToolWindowCommand(
            IToolWindowHost toolWindowHost,
            string text,
            Func<TContext, bool> isAvailableFunc,
            Func<TContext, bool> isEnabledFunc)
            : base(text)
        {
            this.toolWindowHost = toolWindowHost.ExpectNotNull(nameof(toolWindowHost));
            this.isAvailableFunc = isAvailableFunc.ExpectNotNull(nameof(isAvailableFunc));
            this.isEnabledFunc = isEnabledFunc.ExpectNotNull(nameof(isEnabledFunc));
        }

        public override string Id
        {
            get => $"OpenToolWindow.{typeof(TView).Name}";
        }

        public override Task ExecuteAsync(TContext context)
        {
            Debug.Assert(IsAvailable(context) && IsEnabled(context));

            this.toolWindowHost
                .GetToolWindow<TView, TViewModel>()
                .Show();
            return Task.CompletedTask;
        }

        protected override bool IsAvailable(TContext context)
        {
            return this.isAvailableFunc(context);
        }

        protected override bool IsEnabled(TContext context)
        {
            Debug.Assert(IsAvailable(context));
            return this.isEnabledFunc(context);
        }
    }
}
