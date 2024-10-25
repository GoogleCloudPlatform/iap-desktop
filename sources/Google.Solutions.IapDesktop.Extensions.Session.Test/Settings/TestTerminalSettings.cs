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

using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using NUnit.Framework;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]
    public class TestTerminalSettings
    {
        //---------------------------------------------------------------------
        // IsValidFont.
        //---------------------------------------------------------------------

        [Test]
        public void IsValidFont_WhenFontNotFound()
        {
            Assert.IsFalse(TerminalSettings.IsValidFont(string.Empty));
            Assert.IsFalse(TerminalSettings.IsValidFont("doesnotexist"));
        }

        [Test]
        public void IsValidFont_WhenFontMonospaced()
        {
            Assert.IsTrue(TerminalSettings.IsValidFont(FontFamily.GenericMonospace.Name));
        }

        [Test]
        public void IsValidFont_WhenFontNotMonospaced()
        {
            Assert.IsFalse(TerminalSettings.IsValidFont(FontFamily.GenericSansSerif.Name));
        }
    }
}
