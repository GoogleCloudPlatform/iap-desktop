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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using NUnit.Framework;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.ObjectModel
{
    [TestFixture]
    public class TestBindingExtensions
    {
        //---------------------------------------------------------------------
        // OnPropertyChange tests.
        //---------------------------------------------------------------------

        private class Observable : ViewModelBase
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

        private class DummyBinding : BindingExtensions.Binding
        {
        }

        [Test]
        public void WhenObservedPropertyChanges_ThenOnPropertyChangeTriggersCallback()
        {
            var callbacks = 0;
            var observed = new Observable();

            using (observed.OnPropertyChange(
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
            var observed = new Observable();

            using (observed.OnPropertyChange(
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
            var observed = new Observable();

            using (var binding = observed.OnPropertyChange(
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

            using (observed.OnControlPropertyChange(
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

            using (observed.OnControlPropertyChange(
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

            using (var binding = observed.OnControlPropertyChange(
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

            Assert.Throws<ArgumentException>(() => observed.OnControlPropertyChange(
                o => o.PasswordChar,
                _ => { }));
        }

        //---------------------------------------------------------------------
        // Bind tests.
        //---------------------------------------------------------------------

        [Test]
        public void WhenControlChanges_ThenModelIsUpdated()
        {
            var control = new TextBox();
            var model = new Observable();

            control.BindProperty(
                t => t.Text,
                model,
                m => m.One);
            
            Assert.IsNull(model.One);
            control.Text = "test";
            Assert.AreEqual("test", model.One);
        }

        [Test]
        public void WhenModelChanges_ThenControlIsUpdated()
        {
            var control = new TextBox();
            var model = new Observable();

            control.BindProperty(
                t => t.Text,
                model,
                m => m.One);

            Assert.AreEqual("", control.Text);
            model.One = "test";
            Assert.AreEqual("test", control.Text);
        }
    }
}
