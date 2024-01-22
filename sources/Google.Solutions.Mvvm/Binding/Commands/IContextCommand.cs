//
// Copyright 2022 Google LLC
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

using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding.Commands
{
    /// <summary>
    /// Command that can requires a context to be executed.
    /// Typically sufaced as a context menu item.
    /// </summary>
    public interface IContextCommand<TContext> : ICommandBase
    {
        /// <summary>
        /// Queries if command should be enabled or not.
        /// </summary>
        CommandState QueryState(TContext context);

        /// <summary>
        /// Executes the command.
        /// </summary>
        Task ExecuteAsync(TContext context);

        /// <summary>
        /// Optional icon.
        /// </summary>
        Image? Image { get; }

        /// <summary>
        /// Accelerator for command.
        /// </summary>
        Keys ShortcutKeys { get; }

        /// <summary>
        /// Check if command should be executed by default.
        /// </summary>
        bool IsDefault { get; }
    }

    public enum CommandState
    {
        Enabled,
        Disabled,
        Unavailable
    }
}
