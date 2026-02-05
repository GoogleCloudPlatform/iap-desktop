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
            public ViewModelWithCommand(ObservableCommand command)
            {
                this.Command = command;
            }

            public ObservableCommand Command { get; set; }
        }

        private class ViewModelWithCommandAndState : ViewModelWithCommand
        {
            public ViewModelWithCommandAndState(
                ObservableCommand command,
                ObservableProperty<CommandState> commandState)
                : base(command)
            {
                this.CommandState = commandState;
            }

            public ObservableProperty<CommandState> CommandState { get; set; }
        }

        //---------------------------------------------------------------------
        // Button.
        //---------------------------------------------------------------------

        [Test]
        public void Button_WhenButtonCommandDisabled_ThenButtonIsDisabled()
        {
            var commandAvailable = ObservableProperty.Build(false);
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask,
                commandAvailable);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
            {
                var button = new Button();
                form.Controls.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.That(button.Text, Is.EqualTo(command.Text));
                Assert.That(button.Enabled, Is.False);

                form.Close();
            }
        }

        [Test]
        public void Button_WhenButtonCommandAvailable_ThenButtonIsEnabled()
        {
            var commandAvailable = ObservableProperty.Build(true);
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask,
                commandAvailable);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
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

                Assert.That(button.Text, Is.EqualTo(command.Text));
                Assert.IsTrue(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void Button_WhenButtonCommandIsExecuting_ThenButtonIsDisabled()
        {
            var button = new Button();
            var command = ObservableCommand.Build(
                "Command name",
                () =>
                {
                    Assert.That(button.Enabled, Is.False);
                    return Task.CompletedTask;
                });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
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
        public void Button_WhenButtonCommandSucceeds_ThenContextIsNotified()
        {
            var button = new Button();
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
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
        public void Button_WhenButtonCommandThrowsException_ThenContextIsNotified()
        {
            var button = new Button();
            var command = ObservableCommand.Build(
                "Command name",
                () => throw new ArgumentException());

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
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
        public void Button_WhenButtonCommandThrowsTaskCancelledException_ThenContextIsNotNotified()
        {
            var button = new Button();
            var command = ObservableCommand.Build(
                "Command name",
                () => throw new TaskCanceledException());

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
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
        public void Button_WhenButtonCommandTextNotEmpty_ThenButtonTextIsUpdated()
        {
            var button = new Button()
            {
                Text = "Original text"
            };
            var command = ObservableCommand.Build(
                "Command text",
                () => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
            {
                form.Controls.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.That(button.Text, Is.EqualTo("Command text"));

                form.Close();
            }
        }

        [Test]
        public void Button_WhenButtonCommandTextIsNullOrEmpty_ThenButtonTextIsLeftAsIs()
        {
            var button = new Button()
            {
                Text = "Original text"
            };
            var command = ObservableCommand.Build(
                string.Empty,
                () => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
            {
                form.Controls.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.That(button.Text, Is.EqualTo("Original text"));

                form.Close();
            }
        }

        //---------------------------------------------------------------------
        // ToolStripButton.
        //---------------------------------------------------------------------

        [Test]
        public void ToolStripButton_WhenToolStripButtonCommandDisabled_ThenToolStripButtonIsDisabled()
        {
            var commandAvailable = ObservableProperty.Build(false);
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask,
                commandAvailable);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
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

                Assert.That(button.Text, Is.EqualTo(command.Text));
                Assert.That(button.Enabled, Is.False);

                form.Close();
            }
        }

        [Test]
        public void ToolStripButton_WhenToolStripButtonCommandAvailable_ThenToolStripButtonIsEnabled()
        {
            var commandAvailable = ObservableProperty.Build(true);
            var command = ObservableCommand.Build(
                "Command name",
                () => Task.CompletedTask,
                commandAvailable);

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
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

                Assert.That(button.Text, Is.EqualTo(command.Text));
                Assert.IsTrue(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void ToolStripButton_WhenToolStripButtonCommandIsExecuting_ThenToolStripButtonIsDisabled()
        {
            var button = new ToolStripButton();
            var command = ObservableCommand.Build(
                "Command name",
                () =>
                {
                    Assert.That(button.Enabled, Is.False);
                    return Task.CompletedTask;
                });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
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
        public void ToolStripButton_WhenToolStripButtonCommandTextNotEmpty_ThenToolStripButtonTextIsUpdated()
        {
            var button = new ToolStripButton()
            {
                Text = "Original text"
            };
            var command = ObservableCommand.Build(
                "Command text",
                () => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
            {
                var toolStrip = new ToolStrip();
                form.Controls.Add(toolStrip);
                toolStrip.Items.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.That(button.Text, Is.EqualTo("Command text"));

                form.Close();
            }
        }

        [Test]
        public void ToolStripButton_WhenToolStripButtonCommandTextIsNullOrEmpty_ThenToolStripButtonTextIsLeftAsIs()
        {
            var button = new ToolStripButton()
            {
                Text = "Original text"
            };
            var command = ObservableCommand.Build(
                string.Empty,
                () => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand(command))
            {
                var toolStrip = new ToolStrip();
                form.Controls.Add(toolStrip);
                toolStrip.Items.Add(button);

                button.BindObservableCommand(
                    viewModel,
                    m => m.Command,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.That(button.Text, Is.EqualTo("Original text"));

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
            using (var viewModel = new ViewModelWithCommandAndState(command, commandAvailable))
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
