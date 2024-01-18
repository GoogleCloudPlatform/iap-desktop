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
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Linq;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Windows
{

    public interface IMenu
    {
    }

    public interface IMenuCommand<TMenu> : ICommandBase //TODO: Remove?
        where TMenu : IMenu
    { }

    public enum MenuCommandType
    {
        ToolbarCommand,
        MenuCommand
    }

    /// <summary>
    /// Extensible menu, such as a main menu or context menu.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public abstract class Menu<TContext> : IMenu
        where TContext : class
    {
        /// <summary>
        /// Underlying command container.
        /// </summary>
        public ICommandContainer<TContext> Commands { get; }

        /// <summary>
        /// Type of commands that this menu hosts.
        /// </summary>
        public MenuCommandType CommandType { get; }

        protected Menu(
            MenuCommandType commandType,
            ICommandContainer<TContext> commands)
        {
            this.CommandType = commandType;
            this.Commands = commands.ExpectNotNull(nameof(commands));
        }

        private void DiscoverCommands(
            IServiceCategoryProvider serviceProvider,
            Type category)
        {
            serviceProvider.ExpectNotNull(nameof(serviceProvider));

            //
            // Determine the set of command that we need to register
            // and order them by rank. Ranks might have gaps.
            //
            var registrations = serviceProvider
                .GetServicesByCategory(category)
                .OfType<MenuCommandBase<TContext>>()
                .Select(command =>
                    new {
                        Command = command,
                        Attribute = command.GetType().GetCustomAttribute<MenuCommandAttribute>()
                    })
                .Where(item => item.Attribute != null)
                .Where(item => item.Command.CommandType == this.CommandType)
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
            foreach (var registration in registrations)
            {
                if (lastRank != 0 && (registration.Attribute.Rank >> 8) != (lastRank >> 8))
                {
                    this.Commands.AddSeparator();
                }

                var container = this.Commands.AddCommand(registration.Command);

                if (registration.Command is IMenu submenu)
                {
                    //
                    // Register sub-commands.
                    //
                    var subMenu = new SubMenu(
                        registration.Command.CommandType,
                        container);
                    subMenu.DiscoverCommands(
                        serviceProvider,
                        typeof(IMenuCommand<>).MakeGenericType(registration.Command.GetType()));
                }

                lastRank = registration.Attribute.Rank;
            }
        }

        /// <summary>
        /// Discover commands that have been annotated using a 
        /// MenuCommandAttribute and add them to the menu.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public void DiscoverCommands(IServiceCategoryProvider serviceProvider)
        {
            DiscoverCommands(
                serviceProvider,
                typeof(IMenuCommand<>).MakeGenericType(GetType()));
        }

        private class SubMenu : Menu<TContext>
        {
            public SubMenu(
                MenuCommandType commandType,
                ICommandContainer<TContext> commands)
                : base(commandType, commands)
            {
            }
        }
    }
}
