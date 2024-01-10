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

using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding.Commands
{
    [TestFixture]
    public class TestCommandContainer
    {
        private class NonObservableCommandContextSource<TContext> : IContextSource<TContext>
            where TContext : class
        {
            public TContext Context { get; set; }
        }

        //---------------------------------------------------------------------
        // Context.
        //---------------------------------------------------------------------

        [Test]
        public void WhenObservableContextChanged_ThenQueryStateIsCalledOnTopLevelCommands()
        {
            var source = new ContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
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
        public void WhenObservableContextChanged_ThenQueryStateIsCalledOnChildCommands()
        {
            var source = new ContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
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
        public void WhenObservableContextChanged_ThenExecuteUsesLatestContext()
        {
            var source = new ContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
            {
                container.AddCommand(
                    "toplevel",
                    ctx => CommandState.Enabled,
                    ctx => Assert.AreEqual("ctx-2", ctx));

                source.Context = "ctx-2";
            }
        }

        [Test]
        public void WhenNonObservableContextChanged_ThenExecuteUsesOldContext()
        {
            var source = new NonObservableCommandContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
            {
                container.AddCommand(
                    "toplevel",
                    ctx => CommandState.Enabled,
                    ctx => Assert.AreEqual("ctx-1", ctx));

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
                new ContextSource<string>(),
                new Mock<IBindingContext>().Object))
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
                new ContextSource<string>(),
                new Mock<IBindingContext>().Object))
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
                new ContextSource<string>(),
                new Mock<IBindingContext>().Object))
            {
                container.ExecuteCommandByKey(Keys.A);
            }
        }

        [Test]
        public void WhenKeyIsMappedAndCommandIsEnabled_ThenExecuteCommandInvokesHandler()
        {
            var source = new ContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
            {
                string contextOfCallback = null;
                container.AddCommand(
                    new ContextCommand<string>(
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
            var source = new ContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
            {
                container.AddCommand(
                    new ContextCommand<string>(
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
            var source = new ContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
            {
                container.AddCommand(
                    new ContextCommand<string>(
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
            var source = new ContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
            {
                container.AddCommand(
                    new ContextCommand<string>(
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
            var source = new ContextSource<string>()
            {
                Context = "ctx-1"
            };

            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                source,
                new Mock<IBindingContext>().Object))
            {
                var commandExecuted = false;
                container.AddCommand(
                    new ContextCommand<string>(
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
        public void WhenContextIsNull_ThenCommandIsNotExecuted()
        {
            var bindingContext = new Mock<IBindingContext>();
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new ContextSource<string>(),
                bindingContext.Object))
            {
                container.AddCommand(
                    new ContextCommand<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx => Task.CompletedTask)
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();

                bindingContext.Verify(
                    ctx => ctx.OnCommandExecuted(It.IsAny<ICommand>()),
                    Times.Never);
            }
        }

        [Test]
        public void WhenInvokeSucceeds_ThenContextIsNotified()
        {
            var bindingContext = new Mock<IBindingContext>();
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new ContextSource<string>()
                {
                    Context = "test"
                },
                bindingContext.Object))
            {
                container.AddCommand(
                    new ContextCommand<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx => Task.CompletedTask)
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();

                bindingContext.Verify(
                    ctx => ctx.OnCommandExecuted(It.IsAny<ICommand>()),
                    Times.Once);
            }
        }

        [Test]
        public void WhenInvokeSynchronouslyThrowsException_ThenContextIsNotified()
        {
            var bindingContext = new Mock<IBindingContext>();
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new ContextSource<string>()
                {
                    Context = "test"
                },
                bindingContext.Object))
            {
                container.AddCommand(
                    new ContextCommand<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx => throw new ArgumentException())
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();

                bindingContext.Verify(
                    ctx => ctx.OnCommandFailed(
                        null,
                        It.IsAny<ICommand>(),
                        It.IsAny<ArgumentException>()),
                    Times.Once);
            }
        }

        [Test]
        public void WhenInvokeSynchronouslyThrowsCancellationException_ThenExceptionIsSwallowed()
        {
            var bindingContext = new Mock<IBindingContext>();
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new ContextSource<string>()
                {
                    Context = "test"
                },
                bindingContext.Object))
            {
                container.AddCommand(
                    new ContextCommand<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        ctx => throw new TaskCanceledException())
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();

                bindingContext.Verify(
                    ctx => ctx.OnCommandFailed(
                        null,
                        It.IsAny<ICommand>(),
                        It.IsAny<Exception>()),
                    Times.Never);
            }
        }

        [Test]
        public async Task WhenInvokeAsynchronouslyThrowsException_ThenEventIsFired()
        {
            var bindingContext = new Mock<IBindingContext>();
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new ContextSource<string>()
                {
                    Context = "test"
                },
                bindingContext.Object))
            {
                Exception exception = null;
                bindingContext.Setup(
                    ctx => ctx.OnCommandFailed(
                        null,
                        It.IsAny<ICommand>(),
                        It.IsAny<Exception>()))
                    .Callback<IWin32Window, ICommand, Exception>((w, c, e) => exception = e);

                container.AddCommand(
                    new ContextCommand<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        async ctx =>
                        {
                            await Task.Yield();
                            throw new ArgumentException();
                        })
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();

                for (var i = 0; i < 10 && exception == null; i++)
                {
                    await Task.Delay(5);
                }

                Assert.IsNotNull(exception);
                Assert.IsInstanceOf<ArgumentException>(exception);
            }
        }

        [Test]
        public void WhenInvokeAsynchronouslyThrowsCancellationException_ThenExceptionIsSwallowed()
        {
            var bindingContext = new Mock<IBindingContext>();
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new ContextSource<string>(),
                bindingContext.Object))
            {
                container.AddCommand(
                    new ContextCommand<string>(
                        "test",
                        ctx => CommandState.Enabled,
                        async ctx =>
                        {
                            await Task.Yield();
                            throw new TaskCanceledException();
                        })
                    {
                        IsDefault = true
                    });

                container.ExecuteDefaultCommand();

                bindingContext.Verify(
                    ctx => ctx.OnCommandFailed(
                        null,
                        It.IsAny<ICommand>(),
                        It.IsAny<Exception>()),
                    Times.Never);
            }
        }
    }
}
