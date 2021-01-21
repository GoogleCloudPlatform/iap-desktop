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

using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Ssh.Controls;
using NUnit.Framework;
using System;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Test.Controls
{
    [TestFixture]
    public class TestTerminalFont : ApplicationFixtureBase
    {

        //---------------------------------------------------------------------
        // NextSmallerFont.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFontFamilyIsOk_ThenNextSmallerFontThrowsArgumentException()
        {
            using (var font = new Font(TerminalFont.FontFamily, 10f, FontStyle.Regular))
            {
                using (var smallerFont = TerminalFont.NextSmallerFont(font))
                {
                    Assert.AreEqual(smallerFont.Size, font.Size - 1);
                }
            }
        }

        [Test]
        public void WhenFontFamilyIsWrong_ThenNextSmallerFontThrowsArgumentException()
        {
            using (var font = new Font(FontFamily.GenericSerif, 10f, FontStyle.Regular))
            {
                Assert.Throws<ArgumentException>(
                    () => TerminalFont.NextSmallerFont(font));
            }
        }

        //---------------------------------------------------------------------
        // NextLargerFont.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFontFamilyIsOk_ThenNNextLargerFontThrowsArgumentException()
        {
            using (var font = new Font(TerminalFont.FontFamily, 10f, FontStyle.Regular))
            {
                using (var smallerFont = TerminalFont.NextLargerFont(font))
                {
                    Assert.AreEqual(smallerFont.Size, font.Size + 1);
                }
            }
        }

        [Test]
        public void WhenFontFamilyIsWrong_ThenNextLargerFontThrowsArgumentException()
        {
            using (var font = new Font(FontFamily.GenericSerif, 10f, FontStyle.Regular))
            {
                Assert.Throws<ArgumentException>(
                    () => TerminalFont.NextLargerFont(font));
            }
        }
    }
}
