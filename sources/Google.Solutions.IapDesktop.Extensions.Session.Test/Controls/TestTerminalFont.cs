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

using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using NUnit.Framework;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Controls
{
    [TestFixture]
    public class TestTerminalFont
    {
        //---------------------------------------------------------------------
        // NextLargerFont.
        //---------------------------------------------------------------------

        [Test]
        public void NextSmallerFontReturnsSmallerFont()
        {
            using (var font = new TerminalFont(TerminalFont.DefaultFontFamily, 10f))
            {
                using (var smallerFont = font.NextSmallerFont())
                {
                    Assert.AreEqual(font.Font.Size - 1, smallerFont.Font.Size);
                }
            }
        }

        [Test]
        public void WhenMinSizeReached_NextSmallerFontKeepsSize()
        {
            var font = new TerminalFont(TerminalFont.DefaultFontFamily, 10f);
            for (var i = 0; i < 10; i++)
            {
                font = font.NextSmallerFont();
            }

            Assert.AreEqual(TerminalFont.MinimumSize, font.Font.Size);
        }

        [Test]
        public void WhenMaxSizeReached_NextLargerFontKeepsSize()
        {
            var font = new TerminalFont(TerminalFont.DefaultFontFamily, 40f);
            for (var i = 0; i < 10; i++)
            {
                font = font.NextLargerFont();
            }

            Assert.AreEqual(TerminalFont.MaximumSize, font.Font.Size);
        }

        //---------------------------------------------------------------------
        // IsValidFont.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFontNotFound_ThenIsValidFontReturnsFalse()
        {
            Assert.IsFalse(TerminalFont.IsValidFont(string.Empty));
            Assert.IsFalse(TerminalFont.IsValidFont("doesnotexist"));
        }

        [Test]
        public void WhenFontNotMonospaced_ThenIsValidFontReturnsFalse()
        {
            using (var font = new Font(FontFamily.GenericSansSerif, 10))
            {
                Assert.IsFalse(TerminalFont.IsValidFont(font));
            }
        }
    }
}
