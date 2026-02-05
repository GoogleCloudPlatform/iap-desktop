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

                Assert.That(buffer.ToString(), Is.EqualTo(@"\\\{\}"));
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

                Assert.That(buffer.ToString(), Is.EqualTo(@"\line "));
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

                Assert.That(buffer.ToString(), Is.EqualTo(@"\u252?"));
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

                Assert.That(buffer.ToString(), Is.EqualTo(@"abc!"));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("{\\fonttbl}\r\n"));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("{\\fonttbl{\\f0 Microsoft Sans Serif;}{\\f1 Courier New;}}\r\n"));
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

                Assert.That(buffer.ToString(), Is.EqualTo(string.Empty));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("\r\n{\\colortbl\\red0\\green255\\blue255;\\red255\\green0\\blue255;}\r\n"));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("\r\n{\\pard\\par}\r\n\r\n"));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("\\sb100\r\n\\sa200\r\n"));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("\\cf1 \\highlight2 \\fs48 \\f3 \\cf0 \\highlight0 "));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("\\b \\i \\ul text\\b0 \\i0 \\ul0 "));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("{\\field{\\*\\fldinst{HYPERLINK \"href\"}}{\\fldrslt{text}}}\r\n"));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("{\\pntext\\1.\\tab}{\\*\\pn\\pnlvlbody\\pnf0\\pnindent0\\pnstart1\\pndec{\\pntxta.}}\\fi100\\li200"));
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

                Assert.That(
                    buffer.ToString(), Is.EqualTo("{\\pntext\\f3\\'B7\\f0\\tab}{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\bullet}}\\fi100\\li200"));
            }
        }
    }
}
