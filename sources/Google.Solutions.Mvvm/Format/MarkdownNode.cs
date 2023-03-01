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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Google.Solutions.Mvvm.Format
{
    /// <summary>
    /// A Markdown document.
    /// </summary>
    internal class MarkdownDocument
    {
        public DocumentNode Root { get; }

        private static readonly char[] NonLineBreakingWhitespace = new char[] { ' ', '\t' };
        private static readonly char[] UnorderedListBullets = new char[] { '*', '-', '+' };

        public static MarkdownDocument Parse(TextReader reader)
        {
            //
            // There's no proper grammar for Markdown, so we can't use a classic
            // lexer/parser architecture to read Markdown.
            //
            // CommonMark suggests a 2-phase parsing approach [1], which is what
            // we're using here.
            //
            // - In the first stage, we dissect the document into blocks (headings,
            //   paragraphs, list items, etc)
            // - In the second stage, we parse text blocks to resolve emphases,
            //   links, etc.
            //
            // Note that we're only supporting a subset of Markdown syntax
            // features here.
            //
            // [1] https://spec.commonmark.org/0.30/#appendix-a-parsing-strategy
            //
            return new MarkdownDocument(DocumentNode.Parse(reader));
        }

        public static MarkdownDocument Parse(string markdown)
        {
            using (var reader = new StringReader(markdown))
            {
                return Parse(reader);
            }
        }

        private MarkdownDocument(DocumentNode root)
        {
            this.Root = root;
        }

        public override string ToString()
        {
            return this.Root.ToString();
        }

        //---------------------------------------------------------------------
        // Inner classes for blocks.
        //---------------------------------------------------------------------

        /// <summary>
        /// A node in a Markdown document tree.
        /// </summary>
        public abstract class Node
        {
            private Node next;
            private Node firstChild;
            private Node lastChild;

            public virtual IEnumerable<Node> Children
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

            protected void AppendNode(Node block)
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

            protected virtual Node CreateNode(string line)
            {
                if (UnorderedListItemNode.IsUnorderedListItemNode(line))
                {
                    return new UnorderedListItemNode(line);
                }
                else if (OrderedListItemNode.IsOrderedListItemNode(line))
                {
                    return new OrderedListItemNode(line);
                }
                else
                {
                    return new TextNode(line);
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
                    AppendNode(new ParagraphBreak());
                    return false;
                }
                else
                {
                    //
                    // Last block is closed, append a new block.
                    //
                    AppendNode(CreateNode(line));
                    return true;
                }
            }

            public abstract string Value { get; }

            public override string ToString()
            {
                var buffer = new StringBuilder();
                
                void Visit(Node block, int level)
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
        public class ParagraphBreak : Node
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
        public class HeadingNode : Node
        {
            public int Level { get; }
            public string Text { get; }

            public static bool IsHeadingNode(string line)
            {
                var index = line.IndexOfAny(NonLineBreakingWhitespace);
                return index > 0 && line.Substring(0, index).All(c => c == '#');
            }

            public HeadingNode(string line)
            {
                Debug.Assert(IsHeadingNode(line));

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
        public class TextNode : Node
        {
            public string Text { get; private set; }

            public TextNode(string text)
            {
                this.Text = text;
            }

            // TODO: Override Children, parse text

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
        public class OrderedListItemNode : Node
        {
            public string Indent { get; }

            public static bool IsOrderedListItemNode(string line)
            {
                var dotIndex = line.IndexOf('.');

                return dotIndex > 0 &&
                    dotIndex < line.Length - 1 &&
                    line[dotIndex + 1] == ' ' &&
                    line.Substring(0, dotIndex).All(char.IsDigit);
            }

            public OrderedListItemNode(string line)
            {
                Debug.Assert(IsOrderedListItemNode(line));

                var indent = line.IndexOf(' ');
                while (line[indent] == ' ')
                {
                    indent++;
                }

                this.Indent = new string(' ', indent);

                AppendNode(new TextNode(line.Substring(indent)));
            }

            protected override bool TryConsume(string line)
            {
                if (string.IsNullOrEmpty(line))
                {
                    AppendNode(new ParagraphBreak());
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
        public class UnorderedListItemNode : Node
        {
            public char Bullet { get;}
            public string Indent { get; }

            public static bool IsUnorderedListItemNode(string line)
            {
                return line.Length >= 3 && 
                    UnorderedListBullets.Contains(line[0]) && 
                    NonLineBreakingWhitespace.Contains(line[1]);
            }


            public UnorderedListItemNode(string line)
            {
                Debug.Assert(IsUnorderedListItemNode(line));

                this.Bullet = line[0];

                var indent = 1;
                while (line[indent] == ' ')
                {
                    indent++;
                }

                this.Indent = new string(' ', indent);

                AppendNode(new TextNode(line.Substring(indent)));
            }

            protected override bool TryConsume(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    AppendNode(new ParagraphBreak());
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
        public class DocumentNode : Node
        {
            protected override Node CreateNode(string line)
            {
                if (HeadingNode.IsHeadingNode(line))
                {
                    return new HeadingNode(line);
                }
                else
                {
                    return base.CreateNode(line);
                }
            }

            public override string Value => "[Document]";

            public static DocumentNode Parse(TextReader reader)
            {
                var document = new DocumentNode();
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
        }

        //---------------------------------------------------------------------
        // Inner classes for spans.
        //---------------------------------------------------------------------

        internal enum TokenType
        {
            Text,
            Delimiter
        }

        internal class Token
        {
            public TokenType Type { get; }
            public string Value { get; }

            internal Token(TokenType type, string value)
            {
                Debug.Assert(type != TokenType.Delimiter || value.Length == 1);

                this.Type = type;
                this.Value = value;
            }

            public static IEnumerable<Token> Tokenize(string text)
            {
                var textStart = -1;
                for (int i = 0; i < text.Length; i++)
                {
                    switch (text[i])
                    {
                        case '*':
                        case '_':
                        case '[':
                        case ']':
                        case '(':
                        case ')':
                            //
                            // Delimeter.
                            //
                            if (textStart >= 0 && i - textStart > 0 )
                            {
                                //
                                // Flush previous text token, if non-empty.
                                //
                                yield return new Token(TokenType.Text, text.Substring(textStart, i - textStart));
                                textStart = -1;
                            }

                            yield return new Token(TokenType.Delimiter, text[i].ToString());
                            break;

                        default:
                            //
                            // Text.
                            //
                            if (textStart == -1)
                            {
                                textStart = i;
                            }
                            break;
                    }
                }

                if (textStart >= 0)
                {
                    yield return new Token(TokenType.Text, text.Substring(textStart));
                }
            }

            public override string ToString()
            {
                return $"{this.Type}: {this.Value}";
            }

            public override bool Equals(object obj)
            {
                return obj is Token token &&
                    token.Type == this.Type &&
                    token.Value == this.Value;
            }

            public override int GetHashCode()
            {
                return this.Value.GetHashCode();
            }
        }

        internal class Lexer { 
        
        }

        public class TextSpanNode : Node
        {
            public string Text { get; }

            public override string Value => $"[TextSpan] {this.Text}";

            protected override sealed bool TryConsume(string line)
            {
                Lexer lexer = null; // parse
                return TryConsume(lexer);
            }

            protected virtual bool TryConsume(Lexer leyer)
            {
                // Use same TryConsume/Create node approach
                return false;
            }
        }

        public class EmphasisSpanNode : TextSpanNode
        {
            public override string Value => $"[EmphasisSpan] {this.Text}";

            public EmphasisSpanNode(Lexer lexer)
            {
            }

            public static bool IsEmphasis(Lexer lexer)
            {
                return false;
            }
        }

        public class StrongEmphasisSpanNode : TextSpanNode
        {
            public override string Value => $"[StrongEmphasisSpan] {this.Text}";

            public StrongEmphasisSpanNode(Lexer lexer)
            {
            }

            public static bool IsStrongEmphasis(Lexer lexer)
            {
                return false;
            }
        }

        public class LinkSpanNode : TextSpanNode
        {
            public string Href { get; }

            public override string Value => $"[LinkSpan href={this.Href}] {this.Text}";

            public LinkSpanNode(Lexer lexer)
            {
            }

            public static bool IsLink(Lexer lexer)
            {
                return false;
            }
        }
    }
}
