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
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    public class TestBindingExtensions
    {
        private class ViewModelWithBareProperties : ViewModelBase
        {
            private string? one;
            private int two;

            public string? One
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

        //---------------------------------------------------------------------
        // OnPropertyChange.
        //---------------------------------------------------------------------

        private class DummyBinding : BindingExtensions.Binding
        {
            public override void Dispose()
            {
            }
        }

        [Test]
        public void OnPropertyChange_WhenObservedPropertyChanges_ThenOnPropertyChangeTriggersCallback()
        {
            var callbacks = 0;
            var observed = new ViewModelWithBareProperties();

            using (BindingExtensions.CreatePropertyChangeBinding(
                observed,
                o => o.One,
                v => { callbacks++; }))
            {
                observed.One = "observed";
                Assert.That(callbacks, Is.EqualTo(1));
            }

            observed.One = "not observed";
            Assert.That(callbacks, Is.EqualTo(1));
        }

        [Test]
        public void OnPropertyChange_WhenNonObservedPropertyChanges_ThenOnPropertyChangeIgnoresUpdate()
        {
            var callbacks = 0;
            var observed = new ViewModelWithBareProperties();

            using (BindingExtensions.CreatePropertyChangeBinding(
                observed,
                o => o.One,
                v => { callbacks++; }))
            {
                observed.Two = 2;
                Assert.That(callbacks, Is.EqualTo(0));
            }
        }

        [Test]
        public void OnPropertyChange_WhenObservedPropertyChangesButPeerIsBusy_ThenOnPropertyChangeIgnoresUpdate()
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
                Assert.That(callbacks, Is.EqualTo(0));
            }
        }

        //---------------------------------------------------------------------
        // OnControlPropertyChange.
        //---------------------------------------------------------------------

        [Test]
        public void OnControlPropertyChange_WhenObservedControlPropertyChanges_ThenOnControlPropertyChangeTriggersCallback()
        {
            var callbacks = 0;
            var observed = new TextBox();

            using (BindingExtensions.CreateControlPropertyChangeBinding(
                observed,
                o => o.Text,
                v => { callbacks++; }))
            {
                observed.Text = "observed";
                Assert.That(callbacks, Is.EqualTo(1));
            }

            observed.Text = "not observed";
            Assert.That(callbacks, Is.EqualTo(1));
        }

        [Test]
        public void OnControlPropertyChange_WhenNonObservedControlPropertyChanges_ThenOnControlPropertyChangeIgnoresThis()
        {
            var callbacks = 0;
            var observed = new TextBox();

            using (BindingExtensions.CreateControlPropertyChangeBinding(
                observed,
                o => o.Text,
                v => { callbacks++; }))
            {
                observed.TextAlign = HorizontalAlignment.Center;
                Assert.That(callbacks, Is.EqualTo(0));
            }
        }

        [Test]
        public void OnControlPropertyChange_WhenNonObservedControlPropertyChangesButPeerIsBusy_ThenOnPropertyChangeIgnoresUpdate()
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
                Assert.That(callbacks, Is.EqualTo(0));
            }
        }

        [Test]
        public void OnControlPropertyChange_WhenControlHasNoAppropriateEvent_ThenOnControlPropertyChangeThrowsArgumentException()
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
        public void BindProperty_NotifiesContext()
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
        public void BindProperty_AppliesInitialValue()
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

            Assert.That(control.Text, Is.EqualTo("text from model"));
        }

        [Test]
        public void BindProperty_PropagatesControlChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties();

            control.BindProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(model.One, Is.Null);
            control.Text = "test";
            Assert.That(model.One, Is.EqualTo("test"));
        }

        [Test]
        public void BindProperty_PropagatesModelChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties();

            control.BindProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(control.Text, Is.EqualTo(""));
            model.One = "test";
            Assert.That(control.Text, Is.EqualTo("test"));
        }


        //---------------------------------------------------------------------
        // Readonly binding for bare properties.
        //---------------------------------------------------------------------

        [Test]
        public void BindReadonlyProperty_NotifiesContext()
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
        public void BindReadonlyProperty_AppliesInitialValue()
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

            Assert.That(control.Text, Is.EqualTo("text from model"));
        }

        [Test]
        public void BindReadonlyProperty_PropagatesControlChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties();

            control.BindReadonlyProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(model.One, Is.Null);
            control.Text = "test";
            Assert.That(model.One, Is.Null);
        }

        [Test]
        public void BindReadonlyProperty_PropagatesModelChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithBareProperties();

            control.BindReadonlyProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(control.Text, Is.EqualTo(""));
            model.One = "test";
            Assert.That(control.Text, Is.EqualTo("test"));
        }

        //---------------------------------------------------------------------
        // Binding for observable properties.
        //---------------------------------------------------------------------

        [Test]
        public void BindObservableProperty_NotifiesContext()
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
        public void BindObservableProperty_AppliesInitialValue()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();
            model.One.Value = "text from model";

            control.BindObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(control.Text, Is.EqualTo("text from model"));
        }

        [Test]
        public void BindObservableProperty_PropagatesControlChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();

            control.BindObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(model.One.Value, Is.EqualTo(""));
            control.Text = "test";
            Assert.That(model.One.Value, Is.EqualTo("test"));
        }

        [Test]
        public void BindObservableProperty_PropagatesModelChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();

            control.BindObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(control.Text, Is.EqualTo(""));
            model.One.Value = "test";
            Assert.That(control.Text, Is.EqualTo("test"));
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
        public void BindReadonlyObservableProperty_AppliesInitialValue()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();
            model.One.Value = "text from model";

            control.BindReadonlyObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(control.Text, Is.EqualTo("text from model"));
        }

        [Test]
        public void BindReadonlyObservableProperty_PropagatesControlChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();

            control.BindReadonlyObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(model.One.Value, Is.EqualTo(""));
            control.Text = "test";
            Assert.That(model.One.Value, Is.EqualTo(""));
        }

        [Test]
        public void BindReadonlyObservableProperty_PropagatesModelChanges()
        {
            var control = new TextBox();
            var model = new ViewModelWithObservableProperties();

            control.BindReadonlyObservableProperty(
                t => t.Text,
                model,
                m => m.One,
                new Mock<IBindingContext>().Object);

            Assert.That(control.Text, Is.EqualTo(""));
            model.One.Value = "test";
            Assert.That(control.Text, Is.EqualTo("test"));
        }
    }
}
