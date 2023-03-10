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

using Google.Solutions.Common.Util;
using System;
using System.Drawing;
using System.IO;

namespace Google.Solutions.Mvvm.Format
{
    /// <summary>
    /// Simple RTF writer.
    /// </summary>
    internal class RtfWriter : IDisposable
    {
        //
        // NB. For a concise summary of RTF syntax, see
        // https://metacpan.org/dist/RTF-Writer/view/lib/RTF/Cookbook.pod.
        //
        private readonly TextWriter writer;

        public RtfWriter(TextWriter writer)
        {
            this.writer = writer.ExpectNotNull(nameof(writer));
        }

        public void Text(string s)
        {
            foreach (var c in s)
            {
                if (c == '\\' || c == '{' || c == '}')
                {
                    //
                    // These characters need to be escaped.
                    //
                    this.writer.Write('\\');
                    this.writer.Write(c);
                }
                else if (c == '\r')
                {
                    //
                    // Ignore.
                    //
                }
                else if (c == '\n')
                {
                    this.writer.Write("\\line ");
                }
                else if (c >= 0x80)
                {
                    this.writer.Write("\\u");
                    this.writer.Write(Convert.ToUInt32(c));
                    this.writer.Write("?");
                }
                else
                {
                    this.writer.Write(c);
                }
            }
        }

        public void StartDocument()
        {
            this.writer.Write(@"{\rtf1\ansi\deff0\deflang1033\widowctrl");
        }

        public void EndDocument()
        { }


        public void FontTable(FontFamily[] fonts)
        {
            this.writer.Write("{\\fonttbl");
            
            for (int i = 0; i < fonts.Length; i++)
            {
                this.writer.Write("{\\f");
                this.writer.Write(i.ToString());
                this.writer.Write(" ");
                this.writer.Write(fonts[i].Name);
                this.writer.Write(";}");
            }
            this.writer.Write("}");
            this.writer.WriteLine();
        }

        public void ColorTable(Color[] colors)
        {
            if (colors == null || colors.Length == 0)
            {
                return;
            }

            this.writer.WriteLine();
            this.writer.Write("{\\colortbl");

            foreach (var c in colors)
            {
                this.writer.Write("\\red" + c.R);
                this.writer.Write("\\green" + c.G);
                this.writer.Write("\\blue" + c.B);
                this.writer.Write(";");
            }

            this.writer.Write("}");
            this.writer.WriteLine();
        }

        public void StartParagraph()
        {
            this.writer.WriteLine();
            this.writer.Write("{\\pard");
        }

        public void EndParagraph()
        {
            this.writer.WriteLine("\\par}");
            this.writer.WriteLine();
        }

        public void SetSpaceBefore(uint sb)
        {
            this.writer.Write("\\sb");
            this.writer.Write(sb.ToString());
            this.writer.WriteLine();
        }

        public void SetSpaceAfter(uint sa)
        {
            this.writer.Write("\\sa");
            this.writer.Write(sa.ToString());
            this.writer.WriteLine();
        }

        public void SetFontColor(uint index = 0)
        {
            this.writer.Write("\\cf");
            this.writer.Write(index.ToString());
            this.writer.Write(" ");

        }

        public void SetHighlightColor(uint index = 0)
        {
            this.writer.Write("\\highlight");
            this.writer.Write(index.ToString());
            this.writer.Write(" ");
        }

        public void SetFont(uint index = 0)
        {
            this.writer.Write("\\f");
            this.writer.Write(index.ToString());
            this.writer.Write(" ");
        }

        public void SetFontSize(uint size)
        {
            this.writer.Write("\\fs");
            this.writer.Write((size * 2).ToString());
            this.writer.Write(" ");
        }

        public void SetBold(bool bold)
        {
            this.writer.Write(bold ? "\\b " : "\\b0 ");
        }

        public void SetUnderline(bool bold)
        {
            this.writer.Write(bold ? "\\ul " : "\\ul0 ");
        }

        public void SetItalic(bool bold)
        {
            this.writer.Write(bold ? "\\i " : "\\i0 ");
        }

        public void Hyperlink(string text, string href)
        {
            StartHyperlink(href);
            Text(text);
            EndHyperlink();
        }

        public void StartHyperlink(string href)
        {
            this.writer.Write("{\\field{\\*\\fldinst{HYPERLINK \"");
            this.writer.Write(href);
            this.writer.Write("\"}}{\\fldrslt{");
        }

        public void EndHyperlink()
        { 
            this.writer.Write("}}}");
            this.writer.WriteLine();
        }

        public void UnorderedListItem(
            int firstLineIndent, 
            int blockIndent,
            uint symbolFont)
        {
            this.writer.Write("{\\pntext\\f"+ symbolFont + "\\'B7\\f0\\tab}");
            this.writer.Write("{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\bullet}}");
            this.writer.Write("\\fi");
            this.writer.Write(firstLineIndent.ToString());
            this.writer.Write("\\li");
            this.writer.Write(blockIndent.ToString());
        }

        public void OrderedListItem(int firstLineIndent, int blockIndent, int number)
        {
            this.writer.Write("{\\pntext\\" + number + ".\\tab}");
            this.writer.Write("{\\*\\pn\\pnlvlbody\\pnf0\\pnindent0\\pnstart1\\pndec{\\pntxta.}}");
            this.writer.Write("\\fi");
            this.writer.Write(firstLineIndent.ToString());
            this.writer.Write("\\li");
            this.writer.Write(blockIndent.ToString());
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.writer.Dispose();
        }
    }
}
