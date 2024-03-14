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

using Google.Solutions.IapDesktop.Application.Host;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestSingletonApplicationBase
    {
        private class Singleton : SingletonApplicationBase
        {
            private readonly AutoResetEvent runningEvent = new AutoResetEvent(false);
            private readonly AutoResetEvent quitEvent = new AutoResetEvent(false);

            public string[] FirstInvocationArgs = null;
            public string[] SubsequentInvocationArgs = null;
            public Exception SubsequentInvocationException = null;

            public Singleton(string name) : base(name)
            {
            }

            protected override int HandleFirstInvocation(string[] args)
            {
                this.FirstInvocationArgs = args;

                this.runningEvent.Set();
                this.quitEvent.WaitOne();

                return 1;
            }

            protected override int HandleSubsequentInvocation(string[] args)
            {
                this.SubsequentInvocationArgs = args;

                this.runningEvent.Set();

                return 2;
            }

            protected override void HandleSubsequentInvocationException(Exception e)
            {
                this.SubsequentInvocationException = e;
            }

            public void WaitTillRunning()
            {
                this.runningEvent.WaitOne();
            }

            public void Quit()
            {
                this.quitEvent.Set();
            }
        }

        private class HungSingleton : Singleton
        {
            public HungSingleton(string name) : base(name)
            {
            }

            protected override int HandleSubsequentInvocation(string[] args)
            {
                throw new TimeoutException("unresponsive");
            }
        }

        //---------------------------------------------------------------------
        // Run.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoInstanceRunning_ThenRunStartsNewInstance(
            [Values(
                new string[0],
                new [] {"one", "two"},
                new [] {"one two", "three"}
            )] string[] args)
        {
            var app = new Singleton(Guid.NewGuid().ToString());
            Task.Factory.StartNew(() =>
            {
                app.Run(args);
            },
            TaskCreationOptions.LongRunning);

            // Wait for first app to start.
            app.WaitTillRunning();

            app.Quit();
            Assert.AreEqual(args, app.FirstInvocationArgs);
        }

        [Test]
        public void WhenInstanceRunning_ThenRunPassesArgumentsToFirstInstance(
            [Values(
                new string[0],
                new [] {"one", "two"},
                new [] {"one two", "three"}
            )] string[] args)
        {
            var app = new Singleton(Guid.NewGuid().ToString());
            Task.Factory.StartNew(() =>
            {
                app.Run(new[] { "first", "app" });
            },
            TaskCreationOptions.LongRunning);

            // Wait for first app to start.
            app.WaitTillRunning();

            app.Run(args);

            // Wait till first app received data.
            app.WaitTillRunning();
            Assert.AreEqual(args, app.SubsequentInvocationArgs);

            app.Run(new[] { "third" });
            Assert.AreEqual(new[] { "third" }, app.SubsequentInvocationArgs);

            app.Quit();
        }

        [Test]
        public void WhenFirstInstanceHung_ThenRunStartsNewInstance()
        {
            var appName = Guid.NewGuid().ToString();

            var first = new HungSingleton(appName);
            Task.Factory.StartNew(() =>
            {
                first.Run(new[] { "first" });
            },
            TaskCreationOptions.LongRunning);

            // Wait for first app to start.
            first.WaitTillRunning();

            var second = new Singleton(appName);

            Task.Factory.StartNew(() =>
            {
                second.Run(new[] { "second" });
            },
            TaskCreationOptions.LongRunning);

            second.WaitTillRunning();

            first.Quit();
            second.Quit();

            Assert.AreEqual(new[] { "second" }, second.FirstInvocationArgs);
        }

        [Test]
        public void WhenInstanceNamesOnlyDifferInCasing_ThenRunPassesArgumentsToFirstInstance()
        {
            var guid = Guid.NewGuid().ToString();
            var first = new Singleton("TEST_" + guid);
            Task.Factory.StartNew(() => first.Run(new[] { "first" }));
            first.WaitTillRunning();

            var second = new Singleton("test_" + guid);
            Task.Factory.StartNew(() => second.Run(new[] { "second" }));

            first.WaitTillRunning();
            Assert.AreEqual(
                new[] { "second" },
                first.SubsequentInvocationArgs);

            first.Quit();
            second.Quit();
        }

        //---------------------------------------------------------------------
        // Object names.
        //---------------------------------------------------------------------

        [Test]
        public void MutexNameIsLocal()
        {
            var app = new Singleton("test");
            StringAssert.StartsWith("Local\\test_", app.MutexName);
            StringAssert.StartsWith("test_", app.PipeName);
        }

        [Test]
        public void ObjectNamesIncludeSessionId()
        {
            var app = new Singleton("test");
            StringAssert.Contains($"_{app.SessionId:X}_", app.MutexName);
            StringAssert.Contains($"_{app.SessionId:X}_", app.PipeName);
        }

        [Test]
        public void ObjectNamesIncludeUsername()
        {
            var app = new Singleton("test");
            StringAssert.Contains(Environment.UserName.ToLower(), app.MutexName);
            StringAssert.Contains(Environment.UserName.ToLower(), app.PipeName);
        }
    }
}