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

using Google.Solutions.Common.Threading;
using NUnit.Framework;
using System.Threading;

namespace Google.Solutions.Common.Test.Threading
{
    [TestFixture]
    public class TestCancellationTokenExtensions : CommonFixtureBase
    {
        [Test]
        public void Cancel_WhenFirstTokenCancelled_CombinedTokenIsCancelled()
        {
            using (var first = new CancellationTokenSource())
            using (var second = new CancellationTokenSource())
            using (var combined = first.Token.Combine(second.Token))
            {
                Assert.That(combined.Token.IsCancellationRequested, Is.False);

                first.Cancel();

                Assert.That(combined.Token.IsCancellationRequested, Is.True);
            }
        }

        [Test]
        public void Cancel_WhenSecondTokenCancelled_CombinedTokenIsCancelled()
        {
            using (var first = new CancellationTokenSource())
            using (var second = new CancellationTokenSource())
            using (var combined = first.Token.Combine(second.Token))
            {
                Assert.That(combined.Token.IsCancellationRequested, Is.False);

                second.Cancel();

                Assert.That(combined.Token.IsCancellationRequested, Is.True);
            }
        }
    }
}
