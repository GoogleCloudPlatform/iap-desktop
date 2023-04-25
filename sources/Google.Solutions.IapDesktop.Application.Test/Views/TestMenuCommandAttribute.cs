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
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Views
{
    [TestFixture]
    public class TestMenuCommandAttribute : ApplicationFixtureBase
    {
        public interface IRightContext { }
        public interface IWrongContext { }

        //---------------------------------------------------------------------
        // AddRegisteredCommands - filtering commands.
        //---------------------------------------------------------------------

        [MenuCommand]
        internal class NotContextCommand : IMenuCommand
        {
        }

        internal abstract class SampleCommandBase<TContext> : MenuCommand<TContext>
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

        [MenuCommand(Rank = 1)]
        internal class ContextCommandForWrongContext : SampleCommandBase<IWrongContext>
        {
        }

        [MenuCommand(Rank = 1)]
        internal class ContextCommandForToolbarsOnly : SampleCommandBase<IRightContext>
        {
            public ContextCommandForToolbarsOnly()
            {
                this.CommandType = MenuCommandType.ToolbarCommand;
            }
        }

        [Test]
        public void WhenCommandIsForDifferentContext_ThenAddRegisteredCommandsIgnoresIt()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<NotContextCommand>();
            serviceRegistry.AddTransient<ContextCommandForWrongContext>();
            serviceRegistry.AddServiceToCategory<IMenuCommand, NotContextCommand>();
            serviceRegistry.AddServiceToCategory<IMenuCommand, ContextCommandForWrongContext>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();

            commandContainer.Object.AddRegisteredCommands(serviceRegistry, MenuCommandType.MenuCommand);
            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Never);
        }

        [Test]
        public void WhenCommandIsForDifferentCommandType_ThenAddRegisteredCommandsIgnoresIt()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<ContextCommandForToolbarsOnly>();
            serviceRegistry.AddServiceToCategory<IMenuCommand, ContextCommandForToolbarsOnly>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();

            commandContainer.Object.AddRegisteredCommands(serviceRegistry, MenuCommandType.MenuCommand);
            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Never);
        }

        [Test]
        public void WhenCommandMatches_ThenAddRegisteredCommandsAddsCommand()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<ContextCommandForToolbarsOnly>();
            serviceRegistry.AddServiceToCategory<IMenuCommand, ContextCommandForToolbarsOnly>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();

            commandContainer.Object.AddRegisteredCommands(serviceRegistry, MenuCommandType.ToolbarCommand);
            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // AddRegisteredCommands - ranking.
        //---------------------------------------------------------------------

        [MenuCommand(Rank = 0x100)]
        internal class ContextCommandWithRank100 : SampleCommandBase<IRightContext>
        {
        }

        [MenuCommand(Rank = 0x110)]
        internal class ContextCommandWithRank110 : SampleCommandBase<IRightContext>
        {
        }

        [MenuCommand(Rank = 0x300)]
        internal class ContextCommandWithRank300 : SampleCommandBase<IRightContext>
        {
        }

        [Test]
        public void WhenRanksFarApart_ThenAddRegisteredCommandsInjectsSeparator()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddTransient<ContextCommandWithRank100>();
            serviceRegistry.AddTransient<ContextCommandWithRank110>();
            serviceRegistry.AddTransient<ContextCommandWithRank300>();
            serviceRegistry.AddServiceToCategory<IMenuCommand, ContextCommandWithRank100>();
            serviceRegistry.AddServiceToCategory<IMenuCommand, ContextCommandWithRank110>();
            serviceRegistry.AddServiceToCategory<IMenuCommand, ContextCommandWithRank300>();

            var commandContainer = new Mock<ICommandContainer<IRightContext>>();

            commandContainer.Object.AddRegisteredCommands(serviceRegistry, MenuCommandType.MenuCommand);
            commandContainer.Verify(c => c.AddCommand(It.IsAny<IContextCommand<IRightContext>>()), Times.Exactly(3));
            commandContainer.Verify(c => c.AddSeparator(It.IsAny<int?>()), Times.Once);
        }
    }
}
