using System;
using System.Collections.Generic;
using System.IO;

namespace Google.Solutions.Mvvm.Format
{
    internal static class MarkdownToRtf
    {
        private class DocumentVisitor : IDisposable
        {
            private const int FirstLineIndent = -270;
            private const int BlockIndent = 360;

            protected readonly RtfWriter writer;
            private bool inParagraph = false;
            private readonly uint indentationLevel;
            private int nextListItemNumber = 1;

            public DocumentVisitor(
                RtfWriter writer,
                uint indentationLevel = 0)
            {
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
                    this.writer.SetUnderline(true);
                    Visit(link.Children);
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
                    using (var block = new DocumentVisitor(
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
                    using (var block = new DocumentVisitor(
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

        //---------------------------------------------------------------------
        // Extension methods.
        //---------------------------------------------------------------------

        public static void WriteTo(
            this MarkdownDocument document,
            RtfWriter writer)
        {
            using (var visitor = new DocumentVisitor(writer))
            {
                visitor.Visit(document.Root);
            }
        }

        public static string ToRtf(this MarkdownDocument document)
        {
            using (var buffer = new StringWriter())
            using (var writer = new RtfWriter(buffer))
            {
                writer.WriteHeader(System.Drawing.FontFamily.GenericSansSerif);
                WriteTo(document, writer);
                return buffer.ToString();
            }
        }
    }
}
