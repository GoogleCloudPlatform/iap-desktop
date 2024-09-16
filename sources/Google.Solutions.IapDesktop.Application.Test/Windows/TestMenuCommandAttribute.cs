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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestMenuCommandAttribute : ApplicationFixtureBase
    {
        public interface IRightContext { }
        public interface IWrongContext { }

        public sealed class SampleMenu : Menu<IRightContext>
        {
            public SampleMenu(
                MenuCommandType commandType,
                ICommandContainer<IRightContext> commandContainer)
                : base(commandType, commandContainer)
            {
            }
        }

        //---------------------------------------------------------------------
        // AddCommands - filtering.
        //---------------------------------------------------------------------

        [MenuCommand(typeof(SampleMenu))]
        internal class NotContextCommand : IMenuCommand<SampleMenu>
        {
            public string Id => throw new NotImplementedException();

            public string Text => throw new NotImplementedException();

            public string ActivityText => throw new NotImplementedException();
        }

        internal abstract class SampleCommandBase<TContext> : MenuCommandBase<TContext>
        {
            public SampleCommandBase() : base("&test")
            {
            }

            protected override bool IsAvailable(TContext context)
            {
                throw new NotImplementedException();
            }

            protected override bool IsEnabled(TContext context)
            {
                throw new NotImplementedException();
            }
        }

        [MenuCommand(typeof(SampleMenu), Rank = 1)]
        internal class ContextCommandForWrongContext : SampleCommandBase<IWrongContext>, IMenuCommand<SampleMenu>
        {
        }

        [MenuCommand(typeof(SampleMenu), Rank = 1)]
        internal class ContextCommandForToolbarsOnly : SampleCommandBase<IRightContext>, IMenuCommand<SampleMenu>
        {
            public ContextCommandForToolbarsOnly()
            {
                this.CommandType = MenuCommandType.ToolbarCommand;
            }
        }

        [Test]
        public void DiscoverCommands_WhenCommandIsForDifferentContext()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<NotContextCommand>();
            serviceRegistry.AddTransient<ContextCommandForWrongContext>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<SampleMenu>, NotContextCommand>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<SampleMenu>, ContextCommandForWrongContext>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();
            var menu = new SampleMenu(MenuCommandType.MenuCommand, commandContainer.Object);
            menu.DiscoverCommands(serviceRegistry);

            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Never);
        }

        [Test]
        public void DiscoverCommands_WhenCommandIsForDifferentMenu()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<ContextCommandForToolbarsOnly>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<IMenu>, ContextCommandForToolbarsOnly>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();
            var menu = new SampleMenu(MenuCommandType.ToolbarCommand, commandContainer.Object);
            menu.DiscoverCommands(serviceRegistry);

            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Never);
        }

        [Test]
        public void DiscoverCommands_WhenCommandIsForDifferentCommandType()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<ContextCommandForToolbarsOnly>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<SampleMenu>, ContextCommandForToolbarsOnly>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();
            var menu = new SampleMenu(MenuCommandType.MenuCommand, commandContainer.Object);
            menu.DiscoverCommands(serviceRegistry);

            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Never);
        }

        [Test]
        public void DiscoverCommands_WhenCommandMatches()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<ContextCommandForToolbarsOnly>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<SampleMenu>, ContextCommandForToolbarsOnly>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();
            var menu = new SampleMenu(MenuCommandType.ToolbarCommand, commandContainer.Object);
            menu.DiscoverCommands(serviceRegistry);

            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // AddCommands - ranking.
        //---------------------------------------------------------------------

        [MenuCommand(typeof(SampleMenu), Rank = 0x100)]
        internal class ContextCommandWithRank100 : SampleCommandBase<IRightContext>, IMenuCommand<SampleMenu>
        {
        }

        [MenuCommand(typeof(SampleMenu), Rank = 0x110)]
        internal class ContextCommandWithRank110 : SampleCommandBase<IRightContext>, IMenuCommand<SampleMenu>
        {
        }

        [MenuCommand(typeof(SampleMenu), Rank = 0x300)]
        internal class ContextCommandWithRank300 : SampleCommandBase<IRightContext>, IMenuCommand<SampleMenu>
        {
        }

        [Test]
        public void DiscoverCommands_WhenRanksFarApart()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<ContextCommandWithRank100>();
            serviceRegistry.AddTransient<ContextCommandWithRank110>();
            serviceRegistry.AddTransient<ContextCommandWithRank300>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<SampleMenu>, ContextCommandWithRank100>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<SampleMenu>, ContextCommandWithRank110>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<SampleMenu>, ContextCommandWithRank300>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();
            var menu = new SampleMenu(MenuCommandType.MenuCommand, commandContainer.Object);
            menu.DiscoverCommands(serviceRegistry);

            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Exactly(3));
            commandContainer.Verify(c => c.AddSeparator(It.IsAny<int?>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // AddCommands - submenus.
        //---------------------------------------------------------------------

        [MenuCommand(typeof(SampleMenu))]
        internal class ContextCommandWithSubMenu : SampleCommandBase<IRightContext>, IMenuCommand<SampleMenu>, IMenu
        {
        }

        [MenuCommand(typeof(ContextCommandWithSubMenu))]
        internal class NestedCommand : SampleCommandBase<IRightContext>, IMenuCommand<ContextCommandWithSubMenu>
        {
        }

        [Test]
        public void DiscoverCommands_WhenCommandIsMenu_ThenAddCommandsAddsNestedCommands()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<ContextCommandWithSubMenu>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<SampleMenu>, ContextCommandWithSubMenu>();

            serviceRegistry.AddTransient<NestedCommand>();
            serviceRegistry.AddServiceToCategory<IMenuCommand<ContextCommandWithSubMenu>, NestedCommand>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();
            var nestedCommandContainer = new Mock<ICommandContainer<IRightContext>>();
            commandContainer
                .Setup(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()))
                .Returns(nestedCommandContainer.Object);

            var menu = new SampleMenu(MenuCommandType.MenuCommand, commandContainer.Object);
            menu.DiscoverCommands(serviceRegistry);

            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Once);
            nestedCommandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Once);
        }
    }
}
