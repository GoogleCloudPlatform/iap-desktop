using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Google.Solutions.Mvvm.Format
{
    /// <summary>
    /// Converts Markdown to RTF.
    /// </summary>
    internal class MarkdownRtfConverter
    {
        public ColorTable Colors { get; } = new ColorTable();
        public FontTable Fonts { get; } = new FontTable();

        public void Convert(
            MarkdownDocument document,
            RtfWriter writer)
        {
            using (var visitor = new NodeVisitor(this.Fonts, this.Colors, writer))
            {
                visitor.Visit(document.Root);
            }
        }

        public string ConvertToString(MarkdownDocument document)
        {
            using (var buffer = new StringWriter())
            using (var writer = new RtfWriter(buffer))
            {
                Convert(document, writer);
                return buffer.ToString();
            }
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public class ColorTable
        {
            public Color Background { get; set; } = Color.White;
            public Color Font { get; set; } = Color.Black;
            public Color Link { get; set; } = Color.Blue;

            public uint BackgroundIndex = 0;
            public uint FontIndex = 1;
            public uint LinkIndex = 2;

            public Color[] GetTable()
            {
                return new[]
                {
                    this.Background,
                    this.Font,
                    this.Link
                };
            }
        }

        public class FontTable
        {
            public FontFamily Text { get; set; } = FontFamily.GenericSansSerif;
            public uint FontSizeHeading1 = 16;
            public uint FontSizeHeading2 = 14;
            public uint FontSizeHeading3 = 13;
            public uint FontSizeHeading4 = 12;
            public uint FontSizeHeading5 = 11;
            public uint FontSizeHeading6 = 10;
            public uint FontSize = 10;
        }

        /// <summary>
        /// Visitor class.
        /// </summary>
        private class NodeVisitor : IDisposable
        {
            private const int FirstLineIndent = -270;
            private const int BlockIndent = 360;

            protected readonly RtfWriter writer;
            private readonly FontTable fontTable;
            private readonly ColorTable colorTable;

            private readonly uint indentationLevel;

            private bool inParagraph = false;
            private int nextListItemNumber = 1;

            private uint FontSizeForHeading(MarkdownDocument.HeadingNode heading)
            {
                Debug.Assert(heading.Level >= 1);

                var fontSizes = new uint[]
                {
                    this.fontTable.FontSizeHeading1,
                    this.fontTable.FontSizeHeading2,
                    this.fontTable.FontSizeHeading3,
                    this.fontTable.FontSizeHeading4,
                    this.fontTable.FontSizeHeading5,
                    this.fontTable.FontSizeHeading6,
                };

                return fontSizes[Math.Min(heading.Level, fontSizes.Length) - 1];
            }

            public NodeVisitor(
                FontTable fontTable,
                ColorTable colorTable,
                RtfWriter writer,
                uint indentationLevel = 0)
            {
                this.fontTable = fontTable;
                this.colorTable = colorTable;
                this.writer = writer;
                this.indentationLevel = indentationLevel;
            }

            //-----------------------------------------------------------------
            // Paragraph management.
            //-----------------------------------------------------------------

            private void ContinueParagraph()
            {
                if (!this.inParagraph)
                {
                    this.inParagraph = true;
                    this.writer.WriteParagraphStart();
                }
            }

            private void EndParagraph()
            {
                if (this.inParagraph)
                {
                    this.inParagraph = false;
                    this.writer.WriteParagraphEnd();
                }
            }
            private void StartParagraph(uint fontSize)
            {
                EndParagraph();

                this.inParagraph = true;
                this.writer.WriteParagraphStart();
                this.writer.SetFontSize(fontSize);
            }

            public virtual void Dispose()
            {
                EndParagraph();
            }

            //-----------------------------------------------------------------
            // Node visitor.
            //-----------------------------------------------------------------

            private void Visit(IEnumerable<MarkdownDocument.Node> nodes)
            {
                foreach (var node in nodes)
                {
                    Visit(node);
                }
            }

            public void Visit(MarkdownDocument.Node node)
            {
                if (node is MarkdownDocument.HeadingNode heading)
                {
                    StartParagraph(FontSizeForHeading(heading));
                    this.writer.SetBold(true);
                    this.writer.WriteText(heading.Text);
                    this.writer.SetBold(false);
                    EndParagraph();
                }
                else if (node is MarkdownDocument.TextNode text)
                {
                    ContinueParagraph();
                    this.writer.WriteText(text.Text);
                }

                else if (node is MarkdownDocument.LinkNode link)
                {
                    ContinueParagraph();
                    this.writer.WriteHyperlinkStart(link.Href);
                    this.writer.SetUnderline(true); // TODO: Set link color
                    this.writer.SetFontColor(this.colorTable.LinkIndex);
                    Visit(link.Children);
                    this.writer.SetFontColor(this.colorTable.FontIndex);
                    this.writer.SetUnderline(false);
                    this.writer.WriteHyperlinkEnd();
                }
                else if (node is MarkdownDocument.EmphasisNode emph)
                {
                    ContinueParagraph();
                    if (emph.IsStrong)
                    {
                        this.writer.SetBold(true);
                        this.writer.WriteText(emph.Text);
                        this.writer.SetBold(false);
                    }
                    else
                    {
                        this.writer.SetItalic(true);
                        this.writer.WriteText(emph.Text);
                        this.writer.SetItalic(false);
                    }
                }
                else if (node is MarkdownDocument.ParagraphBreak)
                {
                    EndParagraph();
                }
                else if (node is MarkdownDocument.UnorderedListItemNode ul)
                {
                    using (var block = new NodeVisitor(
                        this.fontTable,
                        this.colorTable,
                        this.writer, 
                        this.indentationLevel + 1))
                    {
                        this.writer.WriteUnorderedListItem(
                            FirstLineIndent,
                            (int)block.indentationLevel * BlockIndent);
                        block.Visit(ul.Children);
                    }
                }
                else if (node is MarkdownDocument.OrderedListItemNode ol)
                {
                    using (var block = new NodeVisitor(
                        this.fontTable,
                        this.colorTable,
                        this.writer,
                        this.indentationLevel + 1))
                    {
                        this.writer.WriteOrderedListItem(
                            FirstLineIndent,
                            (int)block.indentationLevel * BlockIndent,
                            this.nextListItemNumber++);
                        block.Visit(ol.Children);
                    }
                }
                else if (node is MarkdownDocument.DocumentNode)
                {
                    this.writer.WriteHeader(this.fontTable.Text);
                    this.writer.WriteColorTable(this.colorTable.GetTable());
                    this.writer.SetBackgroundColor(this.colorTable.BackgroundIndex);
                    Visit(node.Children);
                }
                else
                {
                    if (!(node is MarkdownDocument.OrderedListItemNode))
                    {
                        this.nextListItemNumber = 1; // Reset
                    }

                    Visit(node.Children);
                }
            }
        }
    }
}
