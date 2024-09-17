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

using Google.Solutions.Common.Linq;
using System.Collections.Generic;

namespace Google.Solutions.Mvvm.Binding.Commands
{
    /// <summary>
    /// Context command that contains a group of sub-commands.
    /// </summary>
    public interface IContextCommandGroup<TContext> : IContextCommand<TContext>
    {
        /// <summary>
        /// Sub-commands.
        /// </summary>
        IReadOnlyCollection<IContextCommand<TContext>> SubCommands { get; }
    }

    public static class ContextCommandGroupExtensions
    {
        public static ICommandContainer<TContext> AddCommandGroup<TContext>(
            this ICommandContainer<TContext> container,
            IContextCommandGroup<TContext> command,
            int? index)
            where TContext : class
        {
            var subContainer = container.AddCommand(command, index);
            foreach (var subCommand in command.SubCommands.EnsureNotNull())
            {
                subContainer.AddCommand(subCommand);
            }

            return subContainer;
        }
    }
}
