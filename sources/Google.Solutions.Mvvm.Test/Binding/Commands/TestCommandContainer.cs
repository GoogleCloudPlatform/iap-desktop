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
            public TContext? Context { get; set; }
        }

        //---------------------------------------------------------------------
        // Context.
        //---------------------------------------------------------------------

        [Test]
        public void Context_WhenObservableContextChanged_ThenQueryStateIsCalledOnTopLevelCommands()
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
                    new ContextCommand<string>(
                        "toplevel",
                        ctx =>
                        {
                            observedContexts.Add(ctx);
                            return CommandState.Enabled;
                        },
                        ctx => Assert.Fail()));

                source.Context = "ctx-2";

                Assert.That(
                    observedContexts, Is.EquivalentTo(new[] { "ctx-1", "ctx-2" }));
            }
        }

        [Test]
        public void Context_WhenObservableContextChanged_ThenQueryStateIsCalledOnChildCommands()
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
                    new ContextCommand<string>(
                        "parent",
                        ctx => CommandState.Enabled,
                        ctx => { }));

                var observedContexts = new List<string>();

                subContainer.AddCommand(
                    new ContextCommand<string>(
                        "child",
                        ctx =>
                        {
                            observedContexts.Add(ctx);
                            return CommandState.Enabled;
                        },
                        ctx => Assert.Fail()));

                source.Context = "ctx-2";

                Assert.That(
                    observedContexts, Is.EquivalentTo(new[] { "ctx-1", "ctx-2" }));
            }
        }

        [Test]
        public void Context_WhenObservableContextChanged_ThenExecuteUsesLatestContext()
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
                        "toplevel",
                        ctx => CommandState.Enabled,
                        ctx => Assert.That(ctx, Is.EqualTo("ctx-2"))));

                source.Context = "ctx-2";
            }
        }

        [Test]
        public void Context_WhenNonObservableContextChanged_ThenExecuteUsesOldContext()
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
                    new ContextCommand<string>(
                        "toplevel",
                        ctx => CommandState.Enabled,
                        ctx => Assert.That(ctx, Is.EqualTo("ctx-1"))));

                source.Context = "ctx-2";
            }
        }

        //---------------------------------------------------------------------
        // AddCommand.
        //---------------------------------------------------------------------

        [Test]
        public void AddCommand_WhenAddingCommand_ThenCollectionChangedEventIsRaised()
        {
            using (var container = new CommandContainer<string>(
                ToolStripItemDisplayStyle.Text,
                new ContextSource<string>(),
                new Mock<IBindingContext>().Object))
            {
                PropertyAssert.RaisesCollectionChangedNotification(
                    container.MenuItems,
                    () => container.AddCommand(
                        new ContextCommand<string>(
                            "toplevel",
                            ctx => CommandState.Enabled,
                            ctx => Assert.Fail())),
                    NotifyCollectionChangedAction.Add);
            }
        }

        [Test]
        public void AddCommand_WhenAddingSeparator_ThenCollectionChangedEventIsRaised()
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
        public void ExecuteCommandByKey_WhenKeyIsUnknown_ThenExecuteCommandByKeyDoesNothing()
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
        public void ExecuteCommandByKey_WhenKeyIsMappedAndCommandIsEnabled_ThenExecuteCommandInvokesHandler()
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
                string? contextOfCallback = null;
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

                Assert.That(contextOfCallback, Is.EqualTo("ctx-1"));
            }
        }

        [Test]
        public void ExecuteCommandByKey_WhenKeyIsMappedAndCommandIsDisabled_ThenExecuteCommandByKeyDoesNothing()
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
        public void ExecuteDefaultCommand_WhenContainerDoesNotHaveDefaultCommand_ThenExecuteDefaultCommandDoesNothing()
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
        public void ExecuteDefaultCommand_WhenDefaultCommandIsDisabled_ThenExecuteDefaultCommandDoesNothing()
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
        public void ExecuteDefaultCommand_WhenDefaultCommandIsEnabled_ThenExecuteDefaultExecutesCommand()
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
                Assert.That(commandExecuted, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // Invoke.
        //---------------------------------------------------------------------

        [Test]
        public void Invoke_WhenContextIsNull_ThenCommandIsNotExecuted()
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
                    ctx => ctx.OnCommandExecuted(It.IsAny<ICommandBase>()),
                    Times.Never);
            }
        }

        [Test]
        public void Invoke_WhenInvokeSucceeds_ThenContextIsNotified()
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
                    ctx => ctx.OnCommandExecuted(It.IsAny<ICommandBase>()),
                    Times.Once);
            }
        }

        [Test]
        public void Invoke_WhenInvokeSynchronouslyThrowsException_ThenContextIsNotified()
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
                        It.IsAny<ICommandBase>(),
                        It.IsAny<ArgumentException>()),
                    Times.Once);
            }
        }

        [Test]
        public void Invoke_WhenInvokeSynchronouslyThrowsCancellationException_ThenExceptionIsSwallowed()
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
                        It.IsAny<ICommandBase>(),
                        It.IsAny<Exception>()),
                    Times.Never);
            }
        }

        [Test]
        public async Task Invoke_WhenInvokeAsynchronouslyThrowsException_ThenEventIsFired()
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
                Exception? exception = null;
                bindingContext.Setup(
                    ctx => ctx.OnCommandFailed(
                        null,
                        It.IsAny<ICommandBase>(),
                        It.IsAny<Exception>()))
                    .Callback<IWin32Window, ICommandBase, Exception>((w, c, e) => exception = e);

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

                Assert.That(exception, Is.Not.Null);
                Assert.That(exception, Is.InstanceOf<ArgumentException>());
            }
        }

        [Test]
        public void Invoke_WhenInvokeAsynchronouslyThrowsCancellationException_ThenExceptionIsSwallowed()
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
                        It.IsAny<ICommandBase>(),
                        It.IsAny<Exception>()),
                    Times.Never);
            }
        }
    }
}
