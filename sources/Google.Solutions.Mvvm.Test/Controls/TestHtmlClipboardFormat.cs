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

using Google.Solutions.Mvvm.Controls;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    public class TestHtmlClipboardFormat
    {
        [Test]
        public void Format_AddsHeader()
        {
            var html = "<b>some html</b>";
            var formatted = HtmlClipboardFormat.Format(html);
            Assert.AreEqual(
                "Version:0.9\r\n" +
                "StartHTML:00000095\r\n" +
                "EndHTML:00000187\r\n" +
                "StartFragment:00000138\r\n" +
                "EndFragment:00000154<!DOCTYPE><html><body><!--StartFragment -->" +
                "<b>some html</b><!--EndFragment --></body></html>",
                formatted);
        }
    }
}
