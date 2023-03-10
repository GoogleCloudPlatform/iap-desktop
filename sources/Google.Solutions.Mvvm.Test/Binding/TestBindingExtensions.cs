//
// Copyright 2020 Google LLC
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
using Google.Solutions.Mvvm.Commands;
using Google.Solutions.Testing.Common.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Solutions.Mvvm.Binding.BindingExtensions;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    public class TestBindingExtensions
    {
        private class ViewModelWithBareProperties : ViewModelBase
        {
            private string one;
            private int two;

            public string One
            {
                get => this.one;
                set
                {
                    this.one = value;
                    RaisePropertyChange();
                }
            }

            public int Two
            {
                get => this.two;
                set
                {
                    this.two = value;
                    RaisePropertyChange();
                }
            }
        }

        private class ViewModelWithObservableProperties : ViewModelBase
        {
            public ObservableProperty<string> One = ObservableProperty.Build("");
        }

        private class ViewModelWithCommand : ViewModelBase
        {
            public ObservableProperty<CommandState> CommandState { get; set; }
            public ICommand<ViewModelWithCommand> Command { get; set; }
        }

        //---------------------------------------------------------------------
        // OnPropertyChange tests.
        //---------------------------------------------------------------------

        private class DummyBinding : BindingExtensions.Binding
        {
        }

        [Test]
        public void WhenObservedPropertyChanges_ThenOnPropertyChangeTriggersCallback()
        {
            var callbacks = 0;
            var observed = new ViewModelWithBareProperties();

            using (BindingExtensions.CreatePropertyChangeBinding(
                observed,
                o => o.One,
                v => { callbacks++; }))
            {
                observed.One = "observed";
                Assert.AreEqual(1, callbacks);
            }

            observed.One = "not observed";
            Assert.AreEqual(1, callbacks);
        }

        [Test]
        public void WhenNonObservedPropertyChanges_ThenOnPropertyChangeIgnoresUpdate()
        {
            var callbacks = 0;
            var observed = new ViewModelWithBareProperties();

            using (BindingExtensions.CreatePropertyChangeBinding(
                observed,
                o => o.One,
                v => { callbacks++; }))
            {
                observed.Two = 2;
                Assert.AreEqual(0, callbacks);
            }
        }

        [Test]
        public void WhenObservedPropertyChangesButPeerIsBusy_ThenOnPropertyChangeIgnoresUpdate()
        {
            var callbacks = 0;
            var observed = new ViewModelWithBareProperties();

            using (var binding = BindingExtensions.CreatePropertyChangeBinding(
                observed,
                o => o.One,
                v => { callbacks++; }))
            {
                binding.Peer = new DummyBinding()
                {
                    IsBusy = true
                };

                observed.One = "observed";
                Assert.AreEqual(0, callbacks);
            }
        }

        //---------------------------------------------------------------------
        // OnControlPropertyChange tests.
        //---------------------------------------------------------------------

        [Test]
        public void WhenObservedControlPropertyChanges_ThenOnControlPropertyChangeTriggersCallback()
        {
            var callbacks = 0;
            var observed = new TextBox();

            using (BindingExtensions.CreateControlPropertyChangeBinding(
                observed,
                o => o.Text,
                v => { callbacks++; }))
            {
                observed.Text = "observed";
                Assert.AreEqual(1, callbacks);
            }

            observed.Text = "not observed";
            Assert.AreEqual(1, callbacks);
        }

        [Test]
        public void WhenNonObservedControlPropertyChanges_ThenOnControlPropertyChangeIgnoresThis()
        {
            var callbacks = 0;
            var observed = new TextBox();

            using (BindingExtensions.CreateControlPropertyChangeBinding(
                observed,
                o => o.Text,
                v => { callbacks++; }))
            {
                observed.TextAlign = HorizontalAlignment.Center;
                Assert.AreEqual(0, callbacks);
            }
        }

        [Test]
        public void WhenNonObservedControlPropertyChangesButPeerIsBusy_ThenOnPropertyChangeIgnoresUpdate()
        {
            var callbacks = 0;
            var observed = new TextBox();

            using (var binding = BindingExtensions.CreateControlPropertyChangeBinding(
                observed,
                o => o.Text,
                v => { callbacks++; }))
            {
                binding.Peer = new DummyBinding()
                {
                    IsBusy = true
                };

                observed.TextAlign = HorizontalAlignment.Center;
                Assert.AreEqual(0, callbacks);
            }
        }

        [Test]
        public void WhenControlHasNoAppropriateEvent_ThenOnControlPropertyChangeThrowsArgumentException()
        {
            var observed = new TextBox();

            Assert.Throws<ArgumentException>(() => BindingExtensions.CreateControlPropertyChangeBinding(
                observed,
                o => o.PasswordChar,
                _ => { }));
        }

        //---------------------------------------------------------------------
        // Binding for bare properties.
        //---------------------------------------------------------------------

        [Test]
        public void BindPropertyNotifiesContext()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties
            {
                One = "text from model"
            };

            var context = new Mock<IBindingContext>();

            control.BindProperty(
                t => t.Text,
                model,
                m => m.One,
                context.Object);

            context.Verify(c => c.OnBindingCreated(
                It.Is<IComponent>(ctl => ctl == control),
                It.IsAny<IDisposable>()),
                Times.Exactly(2));
        }

        [Test]
        public void BindPropertyAppliesInitialValue()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties
            {
                One = "text from model"
            };

            control.BindProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("text from model", control.Text);
        }

        [Test]
        public void BindPropertyPropagatesControlChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties();

            control.BindProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.IsNull(model.One);
            control.Text = "test";
            Assert.AreEqual("test", model.One);
        }

        [Test]
        public void BindPropertyPropagatesModelChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties();

            control.BindProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("", control.Text);
            model.One = "test";
            Assert.AreEqual("test", control.Text);
        }


        //---------------------------------------------------------------------
        // Readonly binding for bare properties.
        //---------------------------------------------------------------------

        [Test]
        public void BindReadonlyPropertyNotifiesContext()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties
            {
                One = "text from model"
            };

            var context = new Mock<IBindingContext>();

            control.BindReadonlyProperty(
                t => t.Text,
                model,
                m => m.One,
                context.Object);

            context.Verify(c => c.OnBindingCreated(
                It.Is<IComponent>(ctl => ctl == control),
                It.IsAny<IDisposable>()),
                Times.Once);
        }

        [Test]
        public void BindReadonlyPropertyAppliesInitialValue()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties
            {
                One = "text from model"
            };

            control.BindReadonlyProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("text from model", control.Text);
        }

        [Test]
        public void BindReadonlyPropertyPropagatesControlChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties();

            control.BindReadonlyProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.IsNull(model.One);
            control.Text = "test";
            Assert.IsNull(model.One);
        }

        [Test]
        public void BindReadonlyPropertyPropagatesModelChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties();

            control.BindReadonlyProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("", control.Text);
            model.One = "test";
            Assert.AreEqual("test", control.Text);
        }

        //---------------------------------------------------------------------
        // Binding for observable properties.
        //---------------------------------------------------------------------

        [Test]
        public void BindObservablePropertyNotifiesContext()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();
            model.One.Value = "text from model";

            var context = new Mock<IBindingContext>();

            control.BindObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                context.Object);

            context.Verify(c => c.OnBindingCreated(
                It.Is<IComponent>(ctl => ctl == control),
                It.IsAny<IDisposable>()),
                Times.Exactly(2));
        }

        [Test]
        public void BindObservablePropertyAppliesInitialValue()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();
            model.One.Value = "text from model";

            control.BindObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("text from model", control.Text);
        }

        [Test]
        public void BindObservablePropertyPropagatesControlChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();

            control.BindObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("", model.One.Value);
            control.Text = "test";
            Assert.AreEqual("test", model.One.Value);
        }

        [Test]
        public void BindObservablePropertyPropagatesModelChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();

            control.BindObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("", control.Text);
            model.One.Value = "test";
            Assert.AreEqual("test", control.Text);
        }

        //---------------------------------------------------------------------
        // Readonly binding for bare properties.
        //---------------------------------------------------------------------

        [Test]
        public void BindReadonlyObservableProperty()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();
            model.One.Value = "text from model";

            var context = new Mock<IBindingContext>();

            control.BindReadonlyObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                context.Object);

            context.Verify(c => c.OnBindingCreated(
                It.Is<IComponent>(ctl => ctl == control),
                It.IsAny<IDisposable>()),
                Times.Once);
        }

        [Test]
        public void BindReadonlyObservablePropertyAppliesInitialValue()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();
            model.One.Value = "text from model";

            control.BindReadonlyObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("text from model", control.Text);
        }

        [Test]
        public void BindReadonlyObservablePropertyPropagatesControlChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();

            control.BindReadonlyObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("", model.One.Value);
            control.Text = "test";
            Assert.AreEqual("", model.One.Value);
        }

        [Test]
        public void BindReadonlyObservablePropertyPropagatesModelChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();

            control.BindReadonlyObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.AreEqual("", control.Text);
            model.One.Value = "test";
            Assert.AreEqual("test", control.Text);
        }

        //---------------------------------------------------------------------
        // Binding for commands.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCommandDisabled_ThenButtonIsDisabled()
        {
            var commandAvailable = ObservableProperty.Build(CommandState.Disabled);
            var command = new Command<ViewModelWithCommand>(
                "Command name",
                _ => commandAvailable.Value,
                _ => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command,
                CommandState = commandAvailable
            })
            {
                var button = new Button();
                form.Controls.Add(button);

                button.BindCommand(
                    viewModel,
                    m => m.Command,
                    m => m.CommandState,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.IsFalse(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenCommandAvailable_ThenButtonIsEnabled()
        {
            var commandAvailable = ObservableProperty.Build(CommandState.Enabled);
            var command = new Command<ViewModelWithCommand>(
                "Command name",
                _ => commandAvailable.Value,
                _ => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command,
                CommandState = commandAvailable
            })
            {
                var button = new Button()
                {
                    Enabled = false
                };
                form.Controls.Add(button);

                button.BindCommand(
                    viewModel,
                    m => m.Command,
                    m => m.CommandState,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.IsTrue(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenCommandTurnsFromUnavailableToAvailable_ThenButtonIsEnabled()
        {
            var commandAvailable = ObservableProperty.Build(CommandState.Unavailable);
            var command = new Command<ViewModelWithCommand>(
                "Command name",
                _ => commandAvailable.Value,
                _ => { });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command,
                CommandState = commandAvailable
            })
            {
                var button = new Button()
                {
                    Enabled = false
                };
                form.Controls.Add(button);

                button.BindCommand(
                    viewModel,
                    m => m.Command,
                    m => m.CommandState,
                    new Mock<IBindingContext>().Object);

                form.Show();

                Assert.IsFalse(button.Enabled);
                commandAvailable.Value = CommandState.Enabled;
                Assert.IsTrue(button.Enabled);

                form.Close();
            }
        }

        [Test]
        public void WhenCommandIsExecuting_ThenButtonIsDisabled()
        {
            var button = new Button();
            var command = new Command<ViewModelWithCommand>(
                "Command name",
                _ => CommandState.Enabled,
                _ => {
                    Assert.IsFalse(button.Enabled);
                });

            using (var form = new Form())
            using (var viewModel = new ViewModelWithCommand()
            {
                Command = command
            })
            {
                form.Controls.Add(button);

                button.BindCommand(
                    viewModel,
                    m => m.Command,
                    m => m.CommandState,
                    new Mock<IBindingContext>().Object);

                form.Show();

                button.PerformClick();
                Assert.IsTrue(button.Enabled);
                
                form.Close();
            }
        }

        [Test]
        [InteractiveTest]
        [Apartment(ApartmentState.STA)]
        public void TestCommandBindingUi()
        {
            var commandAvailable = ObservableProperty.Build(CommandState.Enabled);
            var command = new Command<ViewModelWithCommand>(
                "Command name",
                vm => CommandState.Enabled,
                async vm =>
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

                button.BindCommand(
                    viewModel,
                    m => m.Command,
                    m => m.CommandState,
                    new Mock<IBindingContext>().Object);

                form.ShowDialog();
            }
        }
    }
}
