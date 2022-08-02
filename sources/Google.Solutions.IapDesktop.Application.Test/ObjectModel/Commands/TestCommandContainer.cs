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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.ObjectModel.Commands;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.ObjectModel.Commands
{
    [TestFixture]
    public class TestCommandContainer
    {
        //---------------------------------------------------------------------
        // Context.
        //---------------------------------------------------------------------

        [Test]
        public void WhenContextChanged_ThenQueryStateIsCalledOnTopLevelCommands()
        {
            var source = new CommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source))
            {
                var observedContexts = new List<string>();
                container.AddCommand(
                    "toplevel",
                    ctx =>
                    {
                        observedContexts.Add(ctx);
                        return CommandState.Enabled;
                    },
                    ctx => Assert.Fail());

                source.Context = "ctx-2";

                CollectionAssert.AreEquivalent(
                    new[] { "ctx-1", "ctx-2" },
                    observedContexts);
            }
        }

        [Test]
        public void WhenContextChanged_ThenQueryStateIsCalledOnChildCommands()
        {
            var source = new CommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source))
            {
                var subContainer = container.AddCommand(
                    "parent",
                    ctx => CommandState.Enabled,
                    ctx => { });

                var observedContexts = new List<string>();

                subContainer.AddCommand(
                    "child",
                    ctx =>
                    {
                        observedContexts.Add(ctx);
                        return CommandState.Enabled;
                    },
                    ctx => Assert.Fail());

                source.Context = "ctx-2";

                CollectionAssert.AreEquivalent(
                    new[] { "ctx-1", "ctx-2" },
                    observedContexts);
            }
        }

        [Test]
        public void WhenContextChanged_ThenExecuteUsesLatestContext()
        {
            var source = new CommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source))
            {
                container.AddCommand(
                    "toplevel",
                    ctx => CommandState.Enabled,
                    ctx => Assert.AreEqual("ctx-2", ctx));

                source.Context = "ctx-2";
            }
        }

        //---------------------------------------------------------------------
        // AddCommand.
        //---------------------------------------------------------------------

        [Test]
        public void WhenAddingCommand_ThenCollectionChangedEventIsRaised()
        {
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new CommandContextSource<string>()))
            {
                PropertyAssert.RaisesCollectionChangedNotification(
                    container.MenuItems,
                    () => container.AddCommand(
                        "toplevel",
                        ctx => CommandState.Enabled,
                        ctx => Assert.Fail()),
                    NotifyCollectionChangedAction.Add);
            }
        }

        [Test]
        public void WhenAddingSeparator_ThenCollectionChangedEventIsRaised()
        {
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new CommandContextSource<string>()))
            {
                PropertyAssert.RaisesCollectionChangedNotification(
                    container.MenuItems,
                    () => container.AddSeparator(0),
                    NotifyCollectionChangedAction.Add);
            }
        }

        //---------------------------------------------------------------------
        // ExecuteCommandByKey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyIsUnknown_ThenExecuteCommandByKeyDoesNothing()
        {
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new CommandContextSource<string>()))
            {
                container.ExecuteCommandByKey(Keys.A);
            }
        }

        [Test]
        public void WhenKeyIsMappedAndCommandIsEnabled_ThenExecuteCommandInvokesHandler()
        {
            var source = new CommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source))
            {
                string contextOfCallback = null;
                container.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx =>
                        {
                            contextOfCallback = ctx;
                        })
                    {
                        ShortcutKeys = Keys.F4
                    });

                container.ExecuteCommandByKey(Keys.F4);

                Assert.AreEqual("ctx-1", contextOfCallback);
            }
        }

        [Test]
        public void WhenKeyIsMappedAndCommandIsDisabled_ThenExecuteCommandByKeyDoesNothing()
        {
            var source = new CommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source))
            {
                container.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Disabled,
                        ctx =>
                        {
                            Assert.Fail();
                        })
                    {
                        ShortcutKeys = Keys.F4
                    });

                container.ExecuteCommandByKey(Keys.F4);
            }
        }

        //---------------------------------------------------------------------
        // ExecuteDefaultCommand.
        //---------------------------------------------------------------------

        [Test]
        public void WhenContainerDoesNotHaveDefaultCommand_ThenExecuteDefaultCommandDoesNothing()
        {
            var source = new CommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source))
            {
                container.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx => Assert.Fail("Unexpected callback"))
                    {
                    });

                container.ExecuteDefaultCommand();
            }
        }

        [Test]
        public void WhenDefaultCommandIsDisabled_ThenExecuteDefaultCommandDoesNothing()
        {
            var source = new CommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source))
            {
                container.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Disabled,
                        ctx => Assert.Fail("Unexpected callback"))
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();
            }
        }

        [Test]
        public void WhenDefaultCommandIsEnabled_ThenExecuteDefaultExecutesCommand()
        {
            var source = new CommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source))
            {
                bool commandExecuted = false;
                container.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx =>
                        {
                            commandExecuted = true;
                        })
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();
                Assert.IsTrue(commandExecuted);
            }
        }

        //---------------------------------------------------------------------
        // Invoke.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInvokeThrowsException_ThenEventIsFired()
        {
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new CommandContextSource<string>()))
            {
                Exception exception = null;
                container.CommandFailed += (s, a) =>
                {
                    exception = a.Exception;
                };

                container.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx => throw new ArgumentException())
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();

                Assert.IsNotNull(exception);
                Assert.IsInstanceOf<ArgumentException>(exception);
            }
        }

        [Test]
        public void WhenInvokeThrowsCancellationException_ThenExceptionIsSwallowed()
        {
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new CommandContextSource<string>()))
            {
                container.CommandFailed += (s, a) => Assert.Fail();

                container.AddCommand(
                    new Command<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx => throw new TaskCanceledException())
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();
            }
        }
    }
}
