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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Google.Solutions.Mvvm.Format
{
    internal class MarkdownDocument
    {
        public DocumentBlock Root { get; }

        private static readonly char[] NonLineBreakingWhitespace = new char[] { ' ', '\t' };
        private static readonly char[] UnorderedListBullets = new char[] { '*', '-', '+' };

        public MarkdownDocument(string markdown)
        {
            this.Root = DocumentBlock.Parse(markdown);
        }

        public MarkdownDocument(TextReader reader)
        {
            this.Root = DocumentBlock.Parse(reader);
        }

        public override string ToString()
        {
            return this.Root.ToString();
        }

        //---------------------------------------------------------------------
        // Inner classes for blocks.
        //---------------------------------------------------------------------

        /// <summary>
        /// A block in a Markdown document.
        /// </summary>
        public abstract class Block
        {
            private Block next;
            private Block firstChild;
            private Block lastChild;

            public IEnumerable<Block> Children
            {
                get
                {
                    if (this.firstChild == null)
                    {
                        yield break;
                    }
                    else
                    {
                        for (var block = this.firstChild; block != null;  block = block.next)
                        {
                            yield return block;
                        }
                    }
                }
            }

            protected void AppendBlock(Block block)
            {
                if (this.firstChild == null)
                {
                    Debug.Assert(this.lastChild == null);
                    this.firstChild = block;
                    this.lastChild = this.firstChild;
                }
                else
                {
                    this.lastChild.next = block;
                    this.lastChild = block;
                }
            }

            protected virtual Block CreateBlock(string line)
            {
                if (UnorderedListItemBlock.IsUnorderedListItemBlock(line))
                {
                    return new UnorderedListItemBlock(line);
                }
                else if (OrderedListItemBlock.IsOrderedListItemBlock(line))
                {
                    return new OrderedListItemBlock(line);
                }
                else
                {
                    return new TextBlock(line);
                }
            }

            protected virtual bool TryConsume(string line)
            {
                if (this.lastChild != null && this.lastChild.TryConsume(line))
                {
                    //
                    // Continuation of last block.
                    //
                    return true;
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    //
                    // An empty line always ends a block, but does
                    // not start a new one yet.
                    //
                    AppendBlock(new ParagraphBreak());
                    return false;
                }
                else
                {
                    //
                    // Last block is closed, append a new block.
                    //
                    AppendBlock(CreateBlock(line));
                    return true;
                }
            }

            public abstract string Value { get; }

            public override string ToString()
            {
                var buffer = new StringBuilder();
                
                void Visit(Block block, int level)
                {
                    buffer.Append(new string(' ', level));
                    buffer.Append(block.Value);
                    buffer.Append('\n');

                    foreach (var child in block.Children)
                    {
                        Visit(child, level + 1);
                    }
                }

                Visit(this, 0);

                return buffer.ToString(); ;
            }
        }

        /// <summary>
        /// A break between two pararaphs, typically created by an
        /// empty line.
        /// </summary>
        public class ParagraphBreak : Block
        {
            public override string Value => "[ParagraphBreak]";

            protected override bool TryConsume(string line)
            {
                return false;
            }
        }

        /// <summary>
        /// A heading.
        /// </summary>
        public class HeadingBlock : Block
        {
            public int Level { get; }
            public string Text { get; }

            public static bool IsHeadingBlock(string line)
            {
                var index = line.IndexOfAny(NonLineBreakingWhitespace);
                return index > 0 && line.Substring(0, index).All(c => c == '#');
            }

            public HeadingBlock(string line)
            {
                Debug.Assert(IsHeadingBlock(line));

                var whitespaceIndex = line.IndexOfAny(NonLineBreakingWhitespace);
                this.Level = line.Substring(0, whitespaceIndex).Count();
                this.Text = line.Substring(whitespaceIndex).Trim();
            }

            protected override bool TryConsume(string line)
            {
                //
                // Headings are always single-line.
                //
                return false;
            }

            public override string Value => $"[Heading level={this.Level}] {this.Text}";
        }

        /// <summary>
        /// Inline text block. The text might contain links and emphasis,
        /// but we don't parse these at this stage.
        /// </summary>
        public class TextBlock : Block
        {
            public string Text { get; private set; }

            public TextBlock(string text)
            {
                this.Text = text;
            }

            protected override bool TryConsume(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return false;
                }
                else
                {
                    this.Text += " " + line;
                    return true;
                }
            }

            public override string Value => "[Text] " + this.Text;
        }

        /// <summary>
        /// Ordered list item.
        /// </summary>
        public class OrderedListItemBlock : Block
        {
            public string Indent { get; }

            public static bool IsOrderedListItemBlock(string line)
            {
                var dotIndex = line.IndexOf('.');

                return dotIndex > 0 &&
                    dotIndex < line.Length - 1 &&
                    line[dotIndex + 1] == ' ' &&
                    line.Substring(0, dotIndex).All(char.IsDigit);
            }

            public OrderedListItemBlock(string line)
            {
                Debug.Assert(IsOrderedListItemBlock(line));

                var indent = line.IndexOf(' ');
                while (line[indent] == ' ')
                {
                    indent++;
                }

                this.Indent = new string(' ', indent);

                AppendBlock(new TextBlock(line.Substring(indent)));
            }

            protected override bool TryConsume(string line)
            {
                if (string.IsNullOrEmpty(line))
                {
                    AppendBlock(new ParagraphBreak());
                    return true;
                }
                else if (!line.StartsWith(this.Indent))
                {
                    //
                    // Line doesn't have the minimum amount of indentation,
                    // so it can't be a continuation.
                    //
                    // NB. We don't support lazy continations.
                    //
                    return false;
                }
                else
                {
                    return base.TryConsume(line.Substring(this.Indent.Length));
                }
            }

            public override string Value 
                => $"[OrderedListItem indent={this.Indent.Length}]";
        }

        /// <summary>
        /// Unodered list item.
        /// </summary>
        public class UnorderedListItemBlock : Block
        {
            public char Bullet { get;}
            public string Indent { get; }

            public static bool IsUnorderedListItemBlock(string line)
            {
                return line.Length >= 3 && 
                    UnorderedListBullets.Contains(line[0]) && 
                    NonLineBreakingWhitespace.Contains(line[1]);
            }


            public UnorderedListItemBlock(string line)
            {
                Debug.Assert(IsUnorderedListItemBlock(line));

                this.Bullet = line[0];

                var indent = 1;
                while (line[indent] == ' ')
                {
                    indent++;
                }

                this.Indent = new string(' ', indent);

                AppendBlock(new TextBlock(line.Substring(indent)));
            }

            protected override bool TryConsume(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    AppendBlock(new ParagraphBreak());
                    return true;
                }
                else if (!line.StartsWith(this.Indent))
                {
                    //
                    // Line doesn't have the minimum amount of indentation,
                    // so it can't be a continuation.
                    //
                    // NB. We don't support lazy continations.
                    //
                    return false;
                }
                else
                {
                    return base.TryConsume(line.Substring(this.Indent.Length));
                }
            }

            public override string Value
                => $"[UnorderedListItem bullet={this.Bullet} indent={this.Indent.Length}]";
        }

        /// <summary>
        /// Document, this forms the root of the tree.
        /// </summary>
        public class DocumentBlock : Block
        {
            protected override Block CreateBlock(string line)
            {
                if (HeadingBlock.IsHeadingBlock(line))
                {
                    return new HeadingBlock(line);
                }
                else
                {
                    return base.CreateBlock(line);
                }
            }

            public override string Value => "[Document]";

            public static DocumentBlock Parse(TextReader reader)
            {
                var document = new DocumentBlock();
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    else
                    {
                        document.TryConsume(line);
                    }
                }

                return document;
            }

            public static DocumentBlock Parse(string markdown)
            {
                using (var reader = new StringReader(markdown))
                {
                    return Parse(reader);
                }
            }
        }
    }
}
