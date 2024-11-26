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

using Google.Solutions.Mvvm.Interop;
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Interop
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestSubclassCallback
    {
        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        [Test]
        public void Ctor_WhenArgumentInvalid_ThenConstructorThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SubclassCallback(new Control(), null!));
        }

        [Test]
        public void Ctor_WhenHandleCreated_ThenHandleIsSet()
        {
            using (var form = new Form())
            {
                var callback = new SubclassCallback(form, SubclassCallback.DefaultWndProc);

                //
                // Show and close form.
                //
                form.Shown += (_, __) => form.Close();
                form.Show();

                Assert.AreEqual(form.Handle, callback.WindowHandle);
            }
        }

        //---------------------------------------------------------------------
        // Callback.
        //---------------------------------------------------------------------

        [Test]
        public void Callback_WhenSubclassInstalled_ThenCallbackReceivesMessages()
        {
            using (var form = new Form())
            {
                var messagesReceived = 0;
                var callback = new SubclassCallback(form, (ref Message m) =>
                {
                    messagesReceived++;
                    SubclassCallback.DefaultWndProc(ref m);
                });

                //
                // Show and close form.
                //
                form.Shown += (_, __) => form.Close();
                form.Show();

                Assert.AreNotEqual(0, messagesReceived);
            }
        }

        [Test]
        public void Callback_WhenCallbackFails_ThenUnhandledExceptionEventIsRaised()
        {
            using (var form = new Form())
            {
                var callback = new SubclassCallback(form, (ref Message m) =>
                {
                    throw new ArgumentException("test");
                });

                Exception? exception = null;
                callback.UnhandledException += (_, e) => exception = e;

                //
                // Show and close form.
                //
                form.Shown += (_, __) => form.Close();
                form.Show();

                Assert.IsNotNull(exception);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_WhenControlIsDisposed_ThenSublassIsDisposed()
        {
            var form = new Form();

            var callback = new SubclassCallback(form, (ref Message m) =>
            {
                SubclassCallback.DefaultWndProc(ref m);
            });

            //
            // Show and close form.
            //
            form.Shown += (_, __) => form.Close();
            form.Show();
            form.Dispose();

            Assert.IsTrue(callback.IsDisposed);
        }
    }
}
