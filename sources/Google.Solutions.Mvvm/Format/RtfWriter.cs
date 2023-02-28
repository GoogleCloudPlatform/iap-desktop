using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly Encoding encoding;

        public RtfWriter(TextWriter writer)
        {
            this.writer = writer.ThrowIfNot(writer != null, nameof(writer));
        }

        public void WriteText(string s)
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
                else if (c >= 0x80 && c <= 0xFF)
                {
                    var b = this.encoding.GetBytes(new[] { c });
                    this.writer.Write("\'");
                    this.writer.Write(b[0].ToString("X2"));
                }
                else if (c > 0xFF)
                {
                    // TODO: Unicode?
                    throw new FormatException("The text contains a non-ASCII character");
                }
                else
                {
                    this.writer.Write(c);
                }
            }
        }

        public void WriteHeader(FontFamily font)
        {
            this.writer.Write(@"{\rtf1\ansi\deff0\deflang1033\widowctrl{\fonttbl{\f0 ");
            this.writer.Write(font.Name);
            this.writer.WriteLine(";}}");
        }

        public void WriteColorTable(Color[] colors)
        {
            if (colors == null || colors.Length == 0)
            {
                return;
            }

            this.writer.WriteLine("\n{\\colortbl");

            foreach (var c in colors)
            {
                this.writer.WriteLine("\\red" + c.R);
                this.writer.WriteLine("\\green" + c.G);
                this.writer.WriteLine("\\blue" + c.B);
            }

            this.writer.WriteLine("}\n");
        }

        public void WriteParagraphStart()
        {
            this.writer.WriteLine("\n{\\pard");
        }

        public void WriteParagraphEnd()
        {
            this.writer.WriteLine("\\par}\n");
        }

        public void SetBold(bool bold)
        {
            this.writer.WriteLine(bold ? "\\b " : "\\b0 ");
        }

        public void SetUnderline(bool bold)
        {
            this.writer.WriteLine(bold ? "\\ul " : "\\ul0 ");
        }

        public void SetItalic(bool bold)
        {
            this.writer.WriteLine(bold ? "\\i " : "\\i0 ");
        }

        public void SetFontSize(int size)
        {
            this.writer.Write("\\fs");
            this.writer.Write((size * 2).ToString());
            this.writer.Write(" ");
        }

        public void WriteHyperlink(string text, string href)
        {
            this.writer.Write("{\\field{\\*\\fldinst{HYPERLINK \"");
            this.writer.Write(href);
            this.writer.Write("\"}}{\\fldrslt{\\ul");
            this.writer.Write(text);
            this.writer.Write("}}}\n");
        }

        public void WriteListItem(int firstLineIndent, int blockIndent)
        {
            this.writer.Write("{\\pntext\\bullet\\tab}");
            this.writer.Write("{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\bullet}}");
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
