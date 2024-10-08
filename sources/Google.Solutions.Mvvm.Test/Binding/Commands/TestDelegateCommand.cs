﻿//
// Copyright 2024 Google LLC
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
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding.Commands
{
    [TestFixture]
    public class TestDelegateCommand
    {
        //---------------------------------------------------------------------
        // Execute - Synchronous.
        //---------------------------------------------------------------------

        [Test]
        public void Execute_WhenSynchronousExecutionSucceeds_ThenContextIsNotified()
        {
            void handler(EventArgs args) { }

            using (var form = new Form())
            {
                var bindingContext = new Mock<IBindingContext>();
                var command = new DelegateCommand<EventHandler<EventArgs>, EventArgs>(
                    "Test",
                    handler,
                    bindingContext.Object);

                command.Execute(form, EventArgs.Empty);

                bindingContext.Verify(
                    ctx => ctx.OnCommandExecuted(command),
                    Times.Once);
            }
        }

        [Test]
        public void Execute_WhenSynchronousExecutionThrowsException_ThenContextIsNotified()
        {
            void handler(EventArgs args)
            {
                throw new ArgumentException();
            }

            using (var form = new Form())
            {
                var bindingContext = new Mock<IBindingContext>();
                var command = new DelegateCommand<EventHandler<EventArgs>, EventArgs>(
                    "Test",
                    handler,
                    bindingContext.Object);

                command.Execute(form, EventArgs.Empty);

                bindingContext.Verify(
                    ctx => ctx.OnCommandFailed(
                        form,
                        command,
                        It.IsAny<ArgumentException>()),
                    Times.Once);
                bindingContext.Verify(
                    ctx => ctx.OnCommandExecuted(command),
                    Times.Never);
            }
        }

        [Test]
        public void Execute_WhenSynchronousExecutionThrowsTaskCancelledException_ThenContextIsNotNotified()
        {
            void handler(EventArgs args)
            {
                throw new TaskCanceledException();
            }

            using (var form = new Form())
            {
                var bindingContext = new Mock<IBindingContext>();
                var command = new DelegateCommand<EventHandler<EventArgs>, EventArgs>(
                    "Test",
                    handler,
                    bindingContext.Object);

                command.Execute(form, EventArgs.Empty);

                bindingContext.Verify(
                    ctx => ctx.OnCommandFailed(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<ICommandBase>(),
                        It.IsAny<ArgumentException>()),
                    Times.Never);
                bindingContext.Verify(
                    ctx => ctx.OnCommandExecuted(command),
                    Times.Never);
            }
        }

        //---------------------------------------------------------------------
        // Execute - Asynchronous.
        //---------------------------------------------------------------------

        [Test]
        public void Execute_WhenAsynchronousExecutionSucceeds_ThenContextIsNotified()
        {
            Task handler(EventArgs args)
            {
                return Task.CompletedTask;
            }

            using (var form = new Form())
            {
                var bindingContext = new Mock<IBindingContext>();
                var command = new DelegateCommand<EventHandler<EventArgs>, EventArgs>(
                    "Test",
                    handler,
                    bindingContext.Object);

                command.Execute(form, EventArgs.Empty);

                bindingContext.Verify(
                    ctx => ctx.OnCommandExecuted(command),
                    Times.Once);
            }
        }
    }
}
