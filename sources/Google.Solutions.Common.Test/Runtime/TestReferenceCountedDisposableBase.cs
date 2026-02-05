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
using System;

namespace Google.Solutions.Common.Test.Runtime
{
    [TestFixture]
    public class TestReferenceCountedDisposableBase
    {
        private class SampleDisposable : ReferenceCountedDisposableBase
        {
            protected override void Dispose(bool disposing)
            {
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
                Assert.IsFalse(d.IsDisposed);
            }
        }

        [Test]
        public void IsDisposed_WhenDisposedOnce_ThenIsDisposedIsTrue()
        {
            var d = new SampleDisposable();
            d.Dispose();
            Assert.IsTrue(d.IsDisposed);
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_WhenDisposedTwice_ThenDisposeThrowsException()
        {
            var d = new SampleDisposable();
            d.Dispose();
            Assert.Throws<ObjectDisposedException>(() => d.Dispose());
        }

        [Test]
        public void Dispose_WhenRefCountAboveOne_ThenDisposeDoesNothingUntilRefCountDropsToZero()
        {
            var d = new SampleDisposable();
            for (var i = 0; i < 100; i++)
            {
                var refCount = d.AddReference();
                Assert.That(refCount, Is.EqualTo(2));

                d.Dispose();
                Assert.IsFalse(d.IsDisposed);
            }

            d.Dispose();
            Assert.IsTrue(d.IsDisposed);
        }
    }
}
