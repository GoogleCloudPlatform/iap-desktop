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
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding.Commands
{
    [TestFixture]
    public class TestCommandBindingExtensions
    {
        private class ViewModelWithCommand : ViewModelBase
        {
            public ObservableProperty<CommandState> CommandState { get; set; }
            public ObservableCommand Command { get; set; }
        }

        //---------------------------------------------------------------------
        // Button.
        //---------------------------------------------------------------------

        [Test]
        public void WhenButtonCommandDisabled_ThenButtonIsDisabled()
        {
            var commandAvailable = ObservableProperty.Build(false);
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask,
                commandAvailable);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command,
            })
            {
                var button = new Button();
                form.Controls.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.AreEqual(command.Text, button.Text);
                Assert.IsFalse(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenButtonCommandAvailable_ThenButtonIsEnabled()
        {
            var commandAvailable = ObservableProperty.Build(true);
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask,
                commandAvailable);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command,
            })
            {
                var button = new Button()
                {
                    Enabled = false
                };
                form.Controls.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.AreEqual(command.Text, button.Text);
                Assert.IsTrue(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenButtonCommandIsExecuting_ThenButtonIsDisabled()
        {
            var button = new Button();
            var command = ObservableCommand.Build(
                "Command name",
                () =>
                {
                    Assert.IsFalse(button.Enabled);
                    return Task.CompletedTask;
                });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                form.Controls.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                button.PerformClick();
                Assert.IsTrue(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenButtonCommandSucceeds_ThenContextIsNotified()
        {
            var button = new Button();
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                form.Controls.Add(button);

                var bindingContext = new Mock<IBindingContext>();
                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    bindingContext.Object);

                form.Show();

                button.PerformClick();

                bindingContext.Verify(
                    ctx => ctx.OnCommandExecuted(
                        It.Is<ICommandBase>(c => c == command)),
                    Times.Once);

                form.Close();
            }
        }

        [Test]
        public void WhenButtonCommandThrowsException_ThenContextIsNotified()
        {
            var button = new Button();
            var command = ObservableCommand.Build(
                "Command name",
                () => throw new ArgumentException());

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                form.Controls.Add(button);

                var bindingContext = new Mock<IBindingContext>();
                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    bindingContext.Object);

                form.Show();

                button.PerformClick();

                bindingContext.Verify(
                    ctx => ctx.OnCommandFailed(
                        form,
                        It.Is<ICommandBase>(c => c == command),
                        It.IsAny<ArgumentException>()),
                    Times.Once);

                form.Close();
            }
        }

        [Test]
        public void WhenButtonCommandThrowsTaskCancelledException_ThenContextIsNotNotified()
        {
            var button = new Button();
            var command = ObservableCommand.Build(
                "Command name",
                () => throw new TaskCanceledException());

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                form.Controls.Add(button);

                var bindingContext = new Mock<IBindingContext>();
                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    bindingContext.Object);

                form.Show();

                button.PerformClick();

                bindingContext.Verify(
                    ctx => ctx.OnCommandFailed(
                        null,
                        It.Is<ICommandBase>(c => c == command),
                        It.IsAny<TaskCanceledException>()),
                    Times.Never);

                form.Close();
            }
        }

        [Test]
        public void WhenButtonCommandTextNotEmpty_ThenButtonTextIsUpdated()
        {
            var button = new Button()
            {
                Text = "Original text"
            };
            var command = ObservableCommand.Build(
                "Command text",
                () => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                form.Controls.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.AreEqual("Command text", button.Text);

                form.Close();
            }
        }

        [Test]
        public void WhenButtonCommandTextIsNullOrEmpty_ThenButtonTextIsLeftAsIs()
        {
            var button = new Button()
            {
                Text = "Original text"
            };
            var command = ObservableCommand.Build(
                string.Empty,
                () => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                form.Controls.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.AreEqual("Original text", button.Text);

                form.Close();
            }
        }

        //---------------------------------------------------------------------
        // ToolStripButton.
        //---------------------------------------------------------------------

        [Test]
        public void WhenToolStripButtonCommandDisabled_ThenToolStripButtonIsDisabled()
        {
            var commandAvailable = ObservableProperty.Build(false);
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask,
                commandAvailable);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command,
            })
            {
                var toolStrip = new ToolStrip();
                form.Controls.Add(toolStrip);
                var button = new ToolStripButton();
                toolStrip.Items.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.AreEqual(command.Text, button.Text);
                Assert.IsFalse(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenToolStripButtonCommandAvailable_ThenToolStripButtonIsEnabled()
        {
            var commandAvailable = ObservableProperty.Build(true);
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask,
                commandAvailable);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command,
            })
            {
                var toolStrip = new ToolStrip();
                form.Controls.Add(toolStrip);
                var button = new ToolStripButton()
                {
                    Enabled = false
                };
                toolStrip.Items.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.AreEqual(command.Text, button.Text);
                Assert.IsTrue(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenToolStripButtonCommandIsExecuting_ThenToolStripButtonIsDisabled()
        {
            var button = new ToolStripButton();
            var command = ObservableCommand.Build(
                "Command name",
                () =>
                {
                    Assert.IsFalse(button.Enabled);
                    return Task.CompletedTask;
                });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                var toolStrip = new ToolStrip();
                form.Controls.Add(toolStrip);
                toolStrip.Items.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                button.PerformClick();
                Assert.IsTrue(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenToolStripButtonCommandTextNotEmpty_ThenToolStripButtonTextIsUpdated()
        {
            var button = new ToolStripButton()
            {
                Text = "Original text"
            };
            var command = ObservableCommand.Build(
                "Command text",
                () => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                var toolStrip = new ToolStrip();
                form.Controls.Add(toolStrip);
                toolStrip.Items.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.AreEqual("Command text", button.Text);

                form.Close();
            }
        }

        [Test]
        public void WhenToolStripButtonCommandTextIsNullOrEmpty_ThenToolStripButtonTextIsLeftAsIs()
        {
            var button = new ToolStripButton()
            {
                Text = "Original text"
            };
            var command = ObservableCommand.Build(
                string.Empty,
                () => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                var toolStrip = new ToolStrip();
                form.Controls.Add(toolStrip);
                toolStrip.Items.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.AreEqual("Original text", button.Text);

                form.Close();
            }
        }

        //---------------------------------------------------------------------
        // UI tests.
        //---------------------------------------------------------------------

        [Test]
        [RequiresInteraction]
        [Apartment(ApartmentState.STA)]
        public void TestCommandBindingUi()
        {
            var commandAvailable = ObservableProperty.Build(CommandState.Enabled);
            var command = ObservableCommand.Build(
                "Command name",
                async () =>
                {
                    await Task.Delay(500);
                    MessageBox.Show("Execute");
                });

            using (var form = new Form()
            {
                Width = 400,
                Height = 400
            })
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command,
                CommandState = commandAvailable
            })
            {
                var button = new Button()
                {
                    Text = "Bound button"
                };
                form.Controls.Add(button);
                form.AcceptButton = button;

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.ShowDialog();
            }
        }
    }
}
