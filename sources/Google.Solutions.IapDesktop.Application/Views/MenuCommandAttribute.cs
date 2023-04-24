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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Linq;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Views
{
    /// <summary>
    /// Declare that a class can be surfaced as a context
    /// command in a toolbar or menu.
    /// </summary>
    public class MenuCommandAttribute : ServiceCategoryAttribute
    {
        /// <summary>
        /// Rank, used for ordering.
        /// 
        /// Whenever two consecutive ranks differ in more than the
        /// least-significant byte, a separator is injected between
        /// them.
        /// </summary>
        public ushort Rank { get; set; } = 0xFF00;

        public MenuCommandAttribute() : base(typeof(IMenuCommand))
        {
        }
    }

    public interface IMenuCommand
    { }

    public enum MenuCommandType
    {
        ToolbarCommand,
        MenuCommand
    }

    internal static class MenuCommandExtensions
    {
        /// <summary>
        /// Find commands that have been annotated with [MenuCommand] and
        /// add them to the command container.
        /// </summary>
        public static void AddRegisteredCommands<TContext>(
            this ICommandContainer<TContext> commandContainer,
            IServiceCategoryProvider serviceProvider,
            MenuCommandType commandType)
            where TContext : class
        {
            //
            // Determine the set of command that we need to register
            // and order them by rank. Ranks might have gaps.
            //
            var commands = serviceProvider
                .GetServicesByCategory<IMenuCommand>()
                .OfType<MenuCommand<TContext>>()
                .Select(command =>
                    new {
                        Command = command,
                        Attribute = command.GetType().GetCustomAttribute<MenuCommandAttribute>()
                    })
                .Where(item => item.Attribute != null)
                .Where(item => item.Command.IsToolbarCommand == (commandType == MenuCommandType.ToolbarCommand))
                .OrderBy(item => item.Attribute.Rank);

            //
            // Add commands to the container. 
            //
            // Whenever two consecutive ranks differ in their second-most
            // significant byte, we add a separator. We never add two
            // subsequent separators.
            //
            // For ex:
            // - 0x100
            // - 0x110
            // - 0x140
            //     <- add separator
            // - 0x400
            // - 0x410
            //     <- add separator
            // - 0x510
            //

            ushort lastRank = 0;
            foreach (var command in commands)
            {
                if (lastRank != 0 && (command.Attribute.Rank >> 8) != (lastRank >> 8))
                {
                    commandContainer.AddSeparator();
                }

                commandContainer.AddCommand(command.Command);
                lastRank = command.Attribute.Rank;
            }
        }
    }
}
