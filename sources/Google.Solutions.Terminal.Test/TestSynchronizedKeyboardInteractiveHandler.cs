//
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

using Google.Solutions.Ssh;
using Moq;
using NUnit.Framework;
using System.Threading;

namespace Google.Solutions.Terminal.Test
{
    [TestFixture]
    public class TestSynchronizedKeyboardInteractiveHandler
    {
        private class Context : SynchronizationContext
        {
            public int SendCalls = 0;
            public override void Send(SendOrPostCallback d, object state)
            {
                base.Send(d, state);
                this.SendCalls++;
            }
        }

        //--------------------------------------------------------------------
        // Prompt.
        //--------------------------------------------------------------------

        [Test]
        public void Prompt_SendsToContext()
        {
            var context = new Context();
            var handler = new Mock<IKeyboardInteractiveHandler>();
            handler
                .Setup(h => h.Prompt("name", "instruction", "prompt", false))
                .Returns("result");

            var synchronizedHandler = new SynchronizedKeyboardInteractiveHandler(
                handler.Object,
                context);

            Assert.That(
                synchronizedHandler.Prompt("name", "instruction", "prompt", false), Is.EqualTo("result"));
            Assert.That(context.SendCalls, Is.EqualTo(1));
        }


        //--------------------------------------------------------------------
        // PromptForCredentials.
        //--------------------------------------------------------------------

        [Test]
        public void PromptForCredentials_SendsToContext()
        {
            var context = new Context();
            var handler = new Mock<IKeyboardInteractiveHandler>();
            handler
                .Setup(h => h.PromptForCredentials("username"))
                .Returns(new Mock<IPasswordCredential>().Object);

            var synchronizedHandler = new SynchronizedKeyboardInteractiveHandler(
                handler.Object,
                context);

            Assert.That(synchronizedHandler.PromptForCredentials("username"), Is.Not.Null);
            Assert.That(context.SendCalls, Is.EqualTo(1));
        }
    }
}
