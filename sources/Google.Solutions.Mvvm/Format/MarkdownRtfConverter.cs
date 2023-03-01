using System;
using System.Collections.Generic;
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
            using (var visitor = new NodeVisitor(this, writer))
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
        }

        /// <summary>
        /// Visitor class.
        /// </summary>
        private class NodeVisitor : IDisposable
        {
            private const int FirstLineIndent = -270;
            private const int BlockIndent = 360;

            private readonly MarkdownRtfConverter converter;
            protected readonly RtfWriter writer;
            private readonly uint indentationLevel;

            private bool inParagraph = false;
            private int nextListItemNumber = 1;

            public NodeVisitor(
                MarkdownRtfConverter converter,
                RtfWriter writer,
                uint indentationLevel = 0)
            {
                this.converter = converter;
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
                    this.writer.WriteParagraphStart();
                    this.writer.SetBold(true);
                    
                    // TODO: Set font size
                    this.writer.WriteText(heading.Text);
                    this.writer.SetBold(false);
                    this.writer.WriteParagraphEnd();
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
                    this.writer.SetFontColor(this.converter.Colors.LinkIndex);
                    Visit(link.Children);
                    this.writer.SetFontColor(this.converter.Colors.FontIndex);
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
                        this.converter,
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
                        this.converter,
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
                    this.writer.WriteHeader(this.converter.Fonts.Text);
                    this.writer.WriteColorTable(this.converter.Colors.GetTable());
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
