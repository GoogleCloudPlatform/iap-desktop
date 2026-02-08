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

using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding.Commands
{
    [TestFixture]
    public class TestContextCommandGroup
    {
        //---------------------------------------------------------------------
        // AddCommandGroup.
        //---------------------------------------------------------------------

        [Test]
        public void AddCommandGroup_WhenSubCommandsIsNull_ThenAddCommandGroupAddsSingleCommand()
        {
            var emptyGroup = new Mock<IContextCommandGroup<string>>();

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.None,
                new ContextSource<string>(),
                new Mock<IBindingContext>().Object))
            {
                var item = container.AddCommandGroup(emptyGroup.Object, 0);

                Assert.That(item, Is.Not.Null);
                Assert.That(container.MenuItems.Count, Is.EqualTo(1));
                Assert.That(((CommandContainer<string>)item).MenuItems.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void AddCommandGroup_WhenSubCommandsIsEmpty_ThenAddCommandGroupAddsSingleCommand()
        {
            var emptyGroup = new Mock<IContextCommandGroup<string>>();
            emptyGroup
                .SetupGet(g => g.SubCommands)
                .Returns(new List<IContextCommand<string>>());

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.None,
                new ContextSource<string>(),
                new Mock<IBindingContext>().Object))
            {
                var item = container.AddCommandGroup(emptyGroup.Object, 0);

                Assert.That(item, Is.Not.Null);
                Assert.That(container.MenuItems.Count, Is.EqualTo(1));
                Assert.That(((CommandContainer<string>)item).MenuItems.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void AddCommandGroup_WhenSubCommandsNotEmpty_ThenAddCommandGroupAddsSingleCommand()
        {
            var subCommand1 = new Mock<IContextCommand<string>>();
            var subCommand2 = new Mock<IContextCommand<string>>();

            var emptyGroup = new Mock<IContextCommandGroup<string>>();
            emptyGroup
                .SetupGet(g => g.SubCommands)
                .Returns(new[] { subCommand1.Object, subCommand2.Object });

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.None,
                new ContextSource<string>(),
                new Mock<IBindingContext>().Object))
            {
                var item = container.AddCommandGroup(emptyGroup.Object, 0);

                Assert.That(item, Is.Not.Null);
                Assert.That(container.MenuItems.Count, Is.EqualTo(1));
                Assert.That(((CommandContainer<string>)item).MenuItems.Count, Is.EqualTo(2));
            }
        }
    }
}
