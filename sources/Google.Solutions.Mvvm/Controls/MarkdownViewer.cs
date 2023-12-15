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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Control that can render a limited subset of Markdown.
    /// </summary>
    public partial class MarkdownViewer : UserControl
    {
        private string markdown = string.Empty;
        private uint textPadding = 0;

        public MarkdownViewer()
        {
            InitializeComponent();

            //
            // The RTF box always tries to show a caret. Try to suppress
            // this by catching focus events and explicitly hiding the caret.
            //
            this.richTextBox.HideCaret();
            this.richTextBox.LinkClicked += (_, args) => OnLinkClicked(args);
            this.richTextBox.GotFocus += (_, __) => this.richTextBox.HideCaret();
            this.richTextBox.Enter += (_, __) => this.richTextBox.HideCaret();
            this.richTextBox.MouseDown += (_, __) => this.richTextBox.HideCaret();

            //
            // When the RTF box is resized or moved, it tends to loose its padding.
            //
            this.richTextBox.Layout += (_, __) => this.richTextBox.SetPadding((int)this.textPadding);
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        public LinkClickedEventHandler? LinkClicked;

        protected void OnLinkClicked(LinkClickedEventArgs args)
        {
            this.LinkClicked?.Invoke(this, args);
        }

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        public ColorStyles Colors { get; } = new ColorStyles();
        public FontStyles Fonts { get; } = new FontStyles();
        public ParagraphStyles Paragraphs { get; } = new ParagraphStyles();

        /// <summary>
        /// The intermediate RTF.
        /// </summary>
        internal string? Rtf { get; private set; }

        /// <summary>
        /// Gets or sets the Markdown text to bew rendered.
        /// </summary>
        public string Markdown
        {
            get => this.markdown;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(this.Markdown));
                }

                var document = MarkdownDocument.Parse(value);

                using (var buffer = new StringWriter())
                using (var writer = new RtfWriter(buffer))
                using (var visitor = new NodeVisitor(
                    this.Paragraphs,
                    this.Fonts,
                    this.Colors,
                    writer))
                {
                    visitor.Visit(document.Root);
                    this.Rtf = buffer.ToString();
                    this.richTextBox.Rtf = this.Rtf;
                    this.markdown = value;
                }
            }
        }

        [Category("Appearance")]
        public uint TextPadding
        {
            get => this.textPadding;
            set
            {
                this.richTextBox.SetPadding((int)value);
                this.textPadding = value;
            }
        }

        //---------------------------------------------------------------------
        // Markdown to RTF conversion.
        //---------------------------------------------------------------------

        public class ColorStyles
        {
            public Color BackColor { get; set; } = Color.White;
            public Color TextForeColor { get; set; } = Color.DarkSlateGray;
            public Color LinkForeColor { get; set; } = Color.DarkBlue;
            public Color CodeBackColor { get; set; } = Color.LightGray;

            internal uint BackgroundIndex = 0;
            internal uint TextIndex = 1;
            internal uint LinkIndex = 2;
            internal uint CodeIndex = 3;

            internal Color[] GetTable()
            {
                return new[]
                {
                    this.BackColor,
                    this.TextForeColor,
                    this.LinkForeColor,
                    this.CodeBackColor
                };
            }
        }

        public class FontStyles
        {
            public uint FontSizeHeading1 { get; set; } = 16;
            public uint FontSizeHeading2 { get; set; } = 14;
            public uint FontSizeHeading3 { get; set; } = 13;
            public uint FontSizeHeading4 { get; set; } = 12;
            public uint FontSizeHeading5 { get; set; } = 11;
            public uint FontSizeHeading6 { get; set; } = 10;
            public uint FontSize { get; set; } = 10;
            public FontFamily Text { get; set; } = FontFamily.GenericSansSerif;
            public FontFamily Code { get; set; } = FontFamily.GenericMonospace;
            public FontFamily Symbols { get; set; } = new FontFamily("Symbol");

            internal uint TextIndex = 0;
            internal uint CodeIndex = 1;
            internal uint SymbolsIndex = 2;

            internal FontFamily[] GetTable()
            {
                return new[]
                {
                    this.Text,
                    this.Code,
                    this.Symbols
                };
            }
        }

        public class ParagraphStyles
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

            private readonly ParagraphStyles layoutTable;
            private readonly FontStyles fontTable;
            private readonly ColorStyles colorTable;

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
                ParagraphStyles layoutTable,
                FontStyles fontTable,
                ColorStyles colorTable,
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
                    this.writer.Text(heading.Text);
                    this.writer.SetBold(false);
                    EndParagraph();
                }
                else if (node is MarkdownDocument.TextNode text)
                {
                    ContinueParagraph();
                    this.writer.Text(text.Text);
                }

                else if (node is MarkdownDocument.LinkNode link)
                {
                    ContinueParagraph();
                    this.writer.StartHyperlink(link.Href);
                    this.writer.SetUnderline(true);
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
                        this.writer.Text(emph.Text);
                        this.writer.SetFont();
                        this.writer.SetHighlightColor();
                    }
                    else if (emph.IsStrong)
                    {
                        this.writer.SetBold(true);
                        this.writer.Text(emph.Text);
                        this.writer.SetBold(false);
                    }
                    else
                    {
                        this.writer.SetItalic(true);
                        this.writer.Text(emph.Text);
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
                            this.fontTable.SymbolsIndex);
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
