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

using Google.Solutions.Common.Util;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding.Commands
{
    /// <summary>
    /// Context command that invokes callbacks.
    /// </summary>
    public class ContextCommand<TContext> : CommandBase, IContextCommand<TContext>
    {
        private string? id;
        private readonly Func<TContext, Task> executeFunc;
        private readonly Func<TContext, CommandState> queryStateFunc;

        public ContextCommand(
            string text,
            Func<TContext, CommandState> queryStateFunc,
            Func<TContext, Task> executeFunc)
            : base(text)
        {
            this.queryStateFunc = queryStateFunc.ExpectNotNull(nameof(queryStateFunc));
            this.executeFunc = executeFunc.ExpectNotNull(nameof(executeFunc));
        }

        public ContextCommand(
            string text,
            Func<TContext, CommandState> queryStateFunc,
            Action<TContext> executeAction)
            : this(
                  text,
                  queryStateFunc,
                  ctx =>
                  {
                      executeAction(ctx);
                      return Task.CompletedTask;
                  })
        {
        }

        public new string Id
        {
            get => this.id ?? base.Id;
            set => this.id = value;
        }

        public Image? Image { get; set; }

        public Keys ShortcutKeys { get; set; }

        public bool IsDefault { get; set; }

        public Task ExecuteAsync(TContext context)
        {
            if (QueryState(context) != CommandState.Enabled)
            {
                throw new InvalidOperationException(
                    "The command is unavailable or not enabled");
            }

            return this.executeFunc(context);
        }

        public CommandState QueryState(TContext context)
        {
            return this.queryStateFunc(context);
        }
    }

    public static class CommandExtensions // TODO: Move to command container
    {
        public static ICommandContainer<TContext> AddCommand<TContext>(
            this ICommandContainer<TContext> container,
            string text,
            Func<TContext, CommandState> queryStateFunc,
            Action<TContext> executeAction)
            where TContext : class
        {
            return container.AddCommand(new ContextCommand<TContext>(
                text,
                queryStateFunc,
                executeAction));
        }
    }
}
