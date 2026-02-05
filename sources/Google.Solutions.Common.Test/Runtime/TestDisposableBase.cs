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

using Google.Solutions.Common.Runtime;
using NUnit.Framework;

namespace Google.Solutions.Common.Test.Runtime
{
    [TestFixture]
    public class TestDisposableBase
    {
        private class SampleDisposable : DisposableBase
        {
            public int DisposeCalls = 0;

            protected override void Dispose(bool disposing)
            {
                this.DisposeCalls++;
                base.Dispose(disposing);
            }
        }

        //---------------------------------------------------------------------
        // IsDisposed.
        //---------------------------------------------------------------------

        [Test]
        public void IsDisposed_WhenNotDisposed_ThenIsDisposedIsFalse()
        {
            using (var d = new SampleDisposable())
            {
                Assert.That(d.IsDisposed, Is.False);
            }
        }

        [Test]
        public void IsDisposed_WhenDisposedMoreThanOnce_ThenIsDisposedIsTrue()
        {
            var d = new SampleDisposable();
            d.Dispose();
            d.Dispose(); // Again.
            Assert.That(d.IsDisposed, Is.True);
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_WhenDisposedTwice_ThenDisposeIsOnlyCalledOnce()
        {
            var d = new SampleDisposable();
            d.Dispose();
            d.Dispose(); // Again.
            Assert.That(d.DisposeCalls, Is.EqualTo(1));
        }
    }
}
