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

using NUnit.Framework;

namespace Google.Solutions.Platform.Test
{
    [TestFixture]
    public class TestProcessEnvironment
    {
        //---------------------------------------------------------------------
        // NativeArchitecture.
        //---------------------------------------------------------------------

        [Test]
        public void NativeArchitecture()
        {
            Assert.That(
                ProcessEnvironment.NativeArchitecture, Is.Not.EqualTo(Architecture.Unknown));
        }

        //---------------------------------------------------------------------
        // ProcessArchitecture.
        //---------------------------------------------------------------------

        [Test]
        public void ProcessArchitecture()
        {
#if X86
            var expected = Architecture.X86;
#elif X64
            var expected = Architecture.X64;
#elif ARM64
            var expected = Architecture.Arm64;
#else
#error Unknown architecture
#endif

            Assert.That(
                ProcessEnvironment.ProcessArchitecture, Is.EqualTo(expected));
        }
    }
}
