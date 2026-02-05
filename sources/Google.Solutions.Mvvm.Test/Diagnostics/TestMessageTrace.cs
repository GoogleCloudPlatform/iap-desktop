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

using Google.Solutions.Mvvm.Diagnostics;
using NUnit.Framework;
using System;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Diagnostics
{
    [TestFixture]
    public class TestMessageTrace
    {

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_FormatsTrace()
        {
            var trace = new MessageTrace(new[]
            {
                new Message()
                {
                    Msg = 0x1,
                    LParam = new IntPtr(0x01234567),
                    WParam = new IntPtr(0x09ABCDEF)
                },
                new Message()
                {
                    Msg = 0x2,
                    LParam = new IntPtr(0x01234567),
                    WParam = new IntPtr(0x09ABCDEF)
                },
                new Message()
                {
                    Msg = 0x7FFFFFFF,
                    LParam = new IntPtr(0x01234567),
                    WParam = new IntPtr(0x09ABCDEF)
                }
            });

            Assert.That(
                trace.ToString(), Is.EqualTo("0x00000001 (LParam: 0x0000000001234567, WParam: 0x0000000009ABCDEF, WM_CREATE)\n" +
                "0x00000002 (LParam: 0x0000000001234567, WParam: 0x0000000009ABCDEF, WM_DESTROY)\n" +
                "0x7FFFFFFF (LParam: 0x0000000001234567, WParam: 0x0000000009ABCDEF, 2147483647)\n"));
        }
    }
}
