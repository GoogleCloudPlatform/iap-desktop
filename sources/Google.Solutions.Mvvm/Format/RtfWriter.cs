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

        public void StartDocument(FontFamily font)
        {
            this.writer.Write(@"{\rtf1\ansi\deff0\deflang1033\widowctrl{\fonttbl{\f0 ");
            this.writer.Write(font.Name);
            this.writer.Write(";}}");
            this.writer.WriteLine();
        }

        public void EndDocument()
        { }

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
            //this.writer.Write("\\sb");
            //this.writer.Write(sb.ToString());
            //this.writer.WriteLine();
        }

        public void SetSpaceAfter(uint sa)
        {
            //this.writer.Write("\\sa");
            //this.writer.Write(sa.ToString());
            //this.writer.WriteLine();
        }

        public void SetFontColor(uint index)
        {
            this.writer.Write("\\cf");
            this.writer.Write(index.ToString());
            this.writer.Write(" ");

        }
        public void SetBackgroundColor(uint index)
        {
            this.writer.Write("\\cb");
            this.writer.Write(index.ToString());
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

        public void SetFontSize(uint size)
        {
            this.writer.Write("\\fs");
            this.writer.Write((size * 2).ToString());
            this.writer.Write(" ");
        }

        public void Hyperlink(string text, string href)
        {
            StartHyperlink(href);
            WriteText(text);
            EndHyperlink();
        }

        public void StartHyperlink(string href)
        {
            this.writer.Write("{\\field{\\*\\fldinst{HYPERLINK ");
            this.writer.Write(href);
            this.writer.Write("}}{\\fldrslt{");
        }

        public void EndHyperlink()
        { 
            this.writer.Write("}}}");
            this.writer.WriteLine();
        }

        public void UnorderedListItem(int firstLineIndent, int blockIndent)
        {
            this.writer.Write("{\\pntext\\bullet\\tab}");
            this.writer.Write("{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\bullet}}");
            this.writer.Write("\\fi");
            this.writer.Write(firstLineIndent.ToString());
            this.writer.Write("\\li");
            this.writer.Write(blockIndent.ToString());
        }

        public void OrderedListItem(int firstLineIndent, int blockIndent, int number)
        {
            this.writer.Write("{\\pntext\\" + number + "\\tab}");
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
