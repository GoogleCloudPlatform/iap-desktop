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
        public LayoutTable Layout { get; } = new LayoutTable();

        public void Convert(
            MarkdownDocument document,
            RtfWriter writer)
        {
            using (var visitor = new NodeVisitor(
                this.Layout,
                this.Fonts, 
                this.Colors,
                writer))
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
            public Color Text { get; set; } = Color.DarkSlateGray;
            public Color Link { get; set; } = Color.DarkBlue;
            public Color Code { get; set; } = Color.LightGray;

            public uint BackgroundIndex = 0;
            public uint TextIndex = 1;
            public uint LinkIndex = 2;
            public uint CodeIndex = 3;

            public Color[] GetTable()
            {
                return new[]
                {
                    this.Background,
                    this.Text,
                    this.Link,
                    this.Code
                };
            }
        }

        public class FontTable
        {
            public FontFamily Text { get; set; } = FontFamily.GenericSansSerif;
            public FontFamily Code { get; set; } = FontFamily.GenericMonospace;
            public FontFamily Symbols { get; set; } = new FontFamily("Symbol");

            public uint TextIndex = 0;
            public uint CodeIndex = 1;
            public uint SymbolsIndex = 2;

            public FontFamily[] GetTable()
            {
                return new[]
                {
                    this.Text,
                    this.Code,
                    this.Symbols
                };
            }

            public uint FontSizeHeading1 = 16;
            public uint FontSizeHeading2 = 14;
            public uint FontSizeHeading3 = 13;
            public uint FontSizeHeading4 = 12;
            public uint FontSizeHeading5 = 11;
            public uint FontSizeHeading6 = 10;
            public uint FontSize = 10;
        }

        public class LayoutTable
        {
            /// <summary>
            /// Space before paragraph, in twips.
            /// </summary>
            public uint SpaceBeforeParagraph { get; set; } = 100;

            /// <summary>
            /// Space after paragraph, in twips.
            /// </summary>
            public uint SpaceAfterParagraph { get; set; } = 100;

            /// <summary>
            /// Space before paragraph, in twips.
            /// </summary>
            public uint SpaceBeforeListItem { get; set; } = 50;

            /// <summary>
            /// Space after paragraph, in twips.
            /// </summary>
            public uint SpaceAfterListItem { get; set; } = 50;
        }

        /// <summary>
        /// Visitor class.
        /// </summary>
        private class NodeVisitor : IDisposable
        {
            private const int FirstLineIndent = -270;
            private const int BlockIndent = 360;

            protected readonly RtfWriter writer;

            private readonly LayoutTable layoutTable;
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
                LayoutTable layoutTable,
                FontTable fontTable,
                ColorTable colorTable,
                RtfWriter writer,
                uint indentationLevel = 0)
            {
                this.layoutTable = layoutTable;
                this.fontTable = fontTable;
                this.colorTable = colorTable;
                this.writer = writer;
                this.indentationLevel = indentationLevel;
            }

            //-----------------------------------------------------------------
            // Paragraph management.
            //-----------------------------------------------------------------

            private void EndParagraph()
            {
                if (this.inParagraph)
                {
                    this.inParagraph = false;
                    this.writer.EndParagraph();
                }
            }

            private void StartParagraph(
                uint fontSize, 
                uint fontColorIndex,
                uint spaceBefore, 
                uint spaceAfter)
            {
                EndParagraph();

                this.inParagraph = true;
                this.writer.StartParagraph();
                this.writer.SetSpaceBefore(spaceBefore);
                this.writer.SetSpaceAfter(spaceAfter);
                this.writer.SetFontSize(fontSize);
                this.writer.SetFontColor(fontColorIndex);
            }

            private void StartParagraph()
            {
                StartParagraph(
                    this.fontTable.FontSize,
                    this.colorTable.TextIndex,
                    this.layoutTable.SpaceBeforeParagraph,
                    this.layoutTable.SpaceAfterParagraph);
            }

            private void ContinueParagraph()
            {
                if (!this.inParagraph)
                {
                    StartParagraph();
                }
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
                    StartParagraph(
                        FontSizeForHeading(heading),
                        this.colorTable.TextIndex,
                        this.layoutTable.SpaceBeforeParagraph,
                        this.layoutTable.SpaceAfterParagraph);
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
                    this.writer.StartHyperlink(link.Href);
                    this.writer.SetUnderline(true); // TODO: Links aren't clickable
                    this.writer.SetFontColor(this.colorTable.LinkIndex);
                    Visit(link.Children);
                    this.writer.SetFontColor();
                    this.writer.SetUnderline(false);
                    this.writer.EndHyperlink();
                }
                else if (node is MarkdownDocument.EmphasisNode emph)
                {
                    ContinueParagraph();
                    if (emph.IsCode)
                    {
                        this.writer.SetHighlightColor(this.colorTable.CodeIndex);
                        this.writer.SetFont(this.fontTable.CodeIndex);
                        this.writer.WriteText(emph.Text);
                        this.writer.SetFont();
                        this.writer.SetHighlightColor();
                    }
                    else if (emph.IsStrong)
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
                else if (node is MarkdownDocument.UnorderedListItemNode ul) // TODO: Format in list item broken
                {
                    using (var block = new NodeVisitor(
                        this.layoutTable,
                        this.fontTable,
                        this.colorTable,
                        this.writer, 
                        this.indentationLevel + 1))
                    {
                        block.StartParagraph(
                            this.fontTable.FontSize,
                            this.colorTable.TextIndex,
                            this.layoutTable.SpaceBeforeListItem,
                            this.layoutTable.SpaceAfterListItem);
                        this.writer.UnorderedListItem(
                            FirstLineIndent,
                            (int)block.indentationLevel * BlockIndent,
                            this.fontTable.SymbolsIndex,
                            this.fontTable.TextIndex);
                        block.Visit(ul.Children);
                        block.EndParagraph();
                    }
                }
                else if (node is MarkdownDocument.OrderedListItemNode ol)
                {
                    using (var block = new NodeVisitor(
                        this.layoutTable,
                        this.fontTable,
                        this.colorTable,
                        this.writer,
                        this.indentationLevel + 1))
                    {
                        block.StartParagraph(
                            this.fontTable.FontSize,
                            this.colorTable.TextIndex,
                            this.layoutTable.SpaceBeforeListItem,
                            this.layoutTable.SpaceAfterListItem);
                        this.writer.OrderedListItem(
                            FirstLineIndent,
                            (int)block.indentationLevel * BlockIndent,
                            block.nextListItemNumber++);
                        block.Visit(ol.Children);
                        block.EndParagraph();
                    }
                }
                else if (node is MarkdownDocument.DocumentNode)
                {
                    this.writer.StartDocument();
                    this.writer.FontTable(this.fontTable.GetTable());
                    this.writer.ColorTable(this.colorTable.GetTable());
                    Visit(node.Children);

                    //
                    // Add an empty paragraph to prevent "hanging" list items.
                    //
                    StartParagraph();
                    EndParagraph();
                    this.writer.EndDocument();
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
