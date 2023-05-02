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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Linq;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Views
{
    public interface IMenu
    {
    }

    public interface IMenuCommand<TMenu> : ICommand
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
        private readonly ICommandContainer<TContext> commands;

        /// <summary>
        /// Type of commands that this menu hosts.
        /// </summary>
        public MenuCommandType CommandType { get; }

        protected Menu(
            MenuCommandType commandType,
            ICommandContainer<TContext> commands)
        {
            this.CommandType = commandType;
            this.commands = commands.ExpectNotNull(nameof(commands));
        }

        private void AddCommands(
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
                .OfType<MenuCommandBase<TContext>>() // TODO: use IMenuCommand<>
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
                    this.commands.AddSeparator();
                }

                var container = this.commands.AddCommand(registration.Command);

                if (registration.Command is IMenu submenu)
                {
                    //
                    // Register sub-commands.
                    //
                    var subMenu = new SubMenu(
                        registration.Command.CommandType,
                        container);
                    subMenu.AddCommands(
                        serviceProvider,
                        typeof(IMenuCommand<>).MakeGenericType(registration.Command.GetType()));
                }

                lastRank = registration.Attribute.Rank;
            }
        }

        public void AddCommands(IServiceCategoryProvider serviceProvider)
        {
            AddCommands(
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

    /// <summary>
    /// Declare that a class can be surfaced as a context
    /// command in a toolbar or menu.
    /// 
    /// Classes that use this attribute must implement IMenuCommand<TMenu>
    /// where TMenu matches the value of the Menu attribute.
    /// 
    /// Example:
    /// 
    ///   [MenuCommand(typeof(SampleMenu), Rank = 0x100)]
    ///   internal class MyCommand : IMenuCommand<SampleMenu>
    ///   {
    ///      ...
    ///   }
    ///   
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

        /// <summary>
        /// Menu that this command is extending.
        /// </summary>
        public Type Menu { get; }

        /// <summary>
        /// Declare class as a command that extends a menu.
        /// </summary>
        /// <param name="menu">Marker type for the menu to extend</param>
        public MenuCommandAttribute(Type menu)
            : base(typeof(IMenuCommand<>).MakeGenericType(menu))
        {
            this.Menu = menu.ExpectNotNull(nameof(menu));
        }
    }
}
