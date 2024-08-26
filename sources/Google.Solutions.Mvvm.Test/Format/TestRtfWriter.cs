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

using Google.Solutions.Mvvm.Format;
using NUnit.Framework;
using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace Google.Solutions.Mvvm.Test.Format
{
    [TestFixture]
    public class TestRtfWriter
    {
        //---------------------------------------------------------------------
        // Text.
        //---------------------------------------------------------------------

        [Test]
        public void Text_EscapesSpecialCharacters()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.Text(@"\{}");

                Assert.AreEqual(@"\\\{\}", buffer.ToString());
            }
        }

        [Test]
        public void Text_EscapesNewlines()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.Text("\r\n");

                Assert.AreEqual(@"\line ", buffer.ToString());
            }
        }

        [Test]
        public void Text_EscapesNonAsciiChars()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.Text("\u00fc");

                Assert.AreEqual(@"\u252?", buffer.ToString());
            }
        }

        [Test]
        public void Text_DoesNotEscapeAsciiChars()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.Text("abc!");

                Assert.AreEqual(@"abc!", buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // FontTable.
        //---------------------------------------------------------------------

        [Test]
        public void FontTable_WhenEmpty()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.FontTable(Array.Empty<FontFamily>());

                Assert.AreEqual(
                    "{\\fonttbl}\r\n",
                    buffer.ToString());
            }
        }

        [Test]
        public void FontTable()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.FontTable(new[]
                {
                    FontFamily.GenericSansSerif,
                    FontFamily.GenericMonospace
                });

                Assert.AreEqual(
                    "{\\fonttbl{\\f0 Microsoft Sans Serif;}{\\f1 Courier New;}}\r\n",
                    buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // ColorTable.
        //---------------------------------------------------------------------

        [Test]
        public void ColorTable_WhenEmpty()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.ColorTable(Array.Empty<Color>());

                Assert.AreEqual(string.Empty, buffer.ToString());
            }
        }

        [Test]
        public void ColorTable()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.ColorTable(new[]
                {
                    Color.Cyan,
                    Color.Magenta
                });

                Assert.AreEqual(
                    "\r\n{\\colortbl\\red0\\green255\\blue255;\\red255\\green0\\blue255;}\r\n",
                    buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Paragraph.
        //---------------------------------------------------------------------

        [Test]
        public void Paragraph()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.StartParagraph();
                writer.EndParagraph();

                Assert.AreEqual(
                    "\r\n{\\pard\\par}\r\n\r\n",
                    buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Spacing.
        //---------------------------------------------------------------------

        [Test]
        public void Spacing()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.SetSpaceBefore(100);
                writer.SetSpaceAfter(200);

                Assert.AreEqual(
                    "\\sb100\r\n\\sa200\r\n",
                    buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Font.
        //---------------------------------------------------------------------

        [Test]
        public void Font()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.SetFontColor(1);
                writer.SetHighlightColor(2);
                writer.SetFontSize(24);
                writer.SetFont(3);
                writer.SetFontColor();
                writer.SetHighlightColor();

                Assert.AreEqual(
                    "\\cf1 \\highlight2 \\fs48 \\f3 \\cf0 \\highlight0 ",
                    buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Formatting.
        //---------------------------------------------------------------------

        [Test]
        public void Formatting()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.SetBold(true);
                writer.SetItalic(true);
                writer.SetUnderline(true);
                writer.Text("text");
                writer.SetBold(false);
                writer.SetItalic(false);
                writer.SetUnderline(false);

                Assert.AreEqual(
                    "\\b \\i \\ul text\\b0 \\i0 \\ul0 ",
                    buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Hyperlink.
        //---------------------------------------------------------------------

        [Test]
        public void Hyperlink()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.Hyperlink("text", "href");

                Assert.AreEqual(
                    "{\\field{\\*\\fldinst{HYPERLINK \"href\"}}{\\fldrslt{text}}}\r\n",
                    buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Lists.
        //---------------------------------------------------------------------

        [Test]
        public void OrderedListItem()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.OrderedListItem(
                    100,
                    200,
                    1);

                Assert.AreEqual(
                    "{\\pntext\\1.\\tab}{\\*\\pn\\pnlvlbody\\pnf0\\pnindent0\\pnstart1\\pndec{\\pntxta.}}\\fi100\\li200",
                    buffer.ToString());
            }
        }

        [Test]
        public void UnorderedListItem()
        {
            var buffer = new StringBuilder();
            using (var writer = new RtfWriter(new StringWriter(buffer)))
            {
                writer.StartDocument();

                buffer.Clear();
                writer.UnorderedListItem(
                    100,
                    200,
                    3);

                Assert.AreEqual(
                    "{\\pntext\\f3\\'B7\\f0\\tab}{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\bullet}}\\fi100\\li200",
                    buffer.ToString());
            }
        }
    }
}
