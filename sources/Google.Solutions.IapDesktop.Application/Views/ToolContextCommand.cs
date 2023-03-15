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

using Google.Solutions.Mvvm.Binding.Commands;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views
{
    /// <summary>
    /// Base class for context command related to tools.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public abstract class ToolContextCommand<TContext> : CommandBase, IContextCommand<TContext>
    {
        /// <summary>
        /// State to apply when command is available.
        /// </summary>
        public CommandState AvailableState { get; set; } = CommandState.Enabled;

        /// <summary>
        /// State to apply when command is unavailable.
        /// </summary>
        public CommandState UnavailableState { get; set; } = CommandState.Unavailable;

        /// <summary>
        /// Check if command should be made available for this context.
        /// </summary>
        /// <param name="context"></param>
        protected abstract bool IsAvailable(TContext context);

        public ToolContextCommand(string text)
        {
            this.Text = text;
        }

        //---------------------------------------------------------------------
        // IContextCommand.
        //---------------------------------------------------------------------

        public virtual Image Image { get; set; } = null;

        public virtual Keys ShortcutKeys { get; set; } = Keys.None;

        public virtual bool IsDefault { get; set; } = false;

        public CommandState QueryState(TContext context)
        {
            return this.IsAvailable(context)
                ? this.AvailableState
                : this.UnavailableState;
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
}