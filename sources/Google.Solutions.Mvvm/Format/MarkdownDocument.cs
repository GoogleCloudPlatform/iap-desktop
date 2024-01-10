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
            // we're using here:
            //
            // 1. Block-level parsing: We break a document into its major
            //    blocks: Headings, List items, Spans. Spans are blocks
            //    that contain formatted text, and these need additional
            //    processing.
            // 2. Span-level-parsing: Each span is parsed to identify links,
            //    emphasized text, etc.
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
        //
        // Block-level parsing
        //
        //---------------------------------------------------------------------

        /// <summary>
        /// A node in a Markdown document tree.
        /// </summary>
        public abstract class Node
        {
            private Node? next;
            private Node? firstChild;
            protected Node? lastChild;

            /// <summary>
            /// List direct children of this node.
            /// </summary>
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
                        for (var block = this.firstChild; block != null; block = block.next)
                        {
                            yield return block;
                        }
                    }
                }
            }

            /// <summary>
            /// Append a child to this node.
            /// </summary>
            /// <param name="block"></param>
            protected void AppendNode(Node block)
            {
                Debug.Assert((this.firstChild == null) == (this.lastChild == null));

                if (this.firstChild == null)
                {
                    Debug.Assert(this.lastChild == null);
                    this.firstChild = block;
                    this.lastChild = this.firstChild;
                }
                else
                {
                    Debug.Assert(this.lastChild != null);

                    this.lastChild!.next = block;
                    this.lastChild = block;
                }
            }

            /// <summary>
            /// Create a new node for a given line. Only nodes that
            /// are allowed as children are considered.
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
                    return new SpanNode(line);
                }
            }

            /// <summary>
            /// Try to extend the node by consuming an additional line.
            /// <returns>true if line was consumed, false otherwise.</returns>
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

            /// <summary>
            /// Summary string describing this node.
            /// </summary>
            protected abstract string Summary { get; }

            /// <summary>
            /// Create string representation of this node and its
            /// children.
            /// </summary>
            public override string ToString()
            {
                var buffer = new StringBuilder();

                void Visit(Node block, int level)
                {
                    buffer.Append(new string(' ', level));
                    buffer.Append(block.Summary);
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
            protected override string Summary => "[ParagraphBreak]";

            protected override bool TryConsume(string line)
            {
                return false;
            }
        }

        /// <summary>
        /// Heading.
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

            protected override string Summary => $"[Heading level={this.Level}] {this.Text}";
        }

        /// <summary>
        /// Ordered list item.
        /// </summary>
        public class OrderedListItemNode : Node
        {
            private readonly string indent;

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

                //
                // Determine the necessary indent for subsequent
                // lines.
                //
                var indent = line.IndexOf(' ');
                while (indent < line.Length && line[indent] == ' ')
                {
                    indent++;
                }

                this.indent = new string(' ', indent);

                AppendNode(new SpanNode(line.Substring(indent)));
            }

            protected override bool TryConsume(string line)
            {
                if (string.IsNullOrEmpty(line))
                {
                    AppendNode(new ParagraphBreak());
                    return true;
                }
                else if (!line.StartsWith(this.indent))
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
                    return base.TryConsume(line.Substring(this.indent.Length));
                }
            }

            protected override string Summary
                => $"[OrderedListItem indent={this.indent.Length}]";
        }

        /// <summary>
        /// Unodered list item.
        /// </summary>
        public class UnorderedListItemNode : Node
        {
            private readonly string indent;
            private readonly char bullet;

            public static bool IsUnorderedListItemNode(string line)
            {
                return line.Length >= 3 &&
                    UnorderedListBullets.Contains(line[0]) &&
                    NonLineBreakingWhitespace.Contains(line[1]);
            }


            public UnorderedListItemNode(string line)
            {
                Debug.Assert(IsUnorderedListItemNode(line));

                this.bullet = line[0];

                //
                // Determine the necessary indent for subsequent
                // lines.
                //
                var indent = 1;
                while (indent < line.Length && line[indent] == ' ')
                {
                    indent++;
                }

                this.indent = new string(' ', indent);

                AppendNode(new SpanNode(line.Substring(indent)));
            }

            protected override bool TryConsume(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    AppendNode(new ParagraphBreak());
                    return true;
                }
                else if (!line.StartsWith(this.indent))
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
                    return base.TryConsume(line.Substring(this.indent.Length));
                }
            }

            protected override string Summary
                => $"[UnorderedListItem bullet={this.bullet} indent={this.indent.Length}]";
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

            protected override string Summary => "[Document]";

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
        //
        // Span-level parsing
        //
        //---------------------------------------------------------------------

        internal enum TokenType
        {
            Text,
            Delimiter
        }

        /// <summary>
        /// A token within a text span. Tokens are separated by delimiters.
        /// Delimiters may indicate a new kind of span, but sometimes they
        /// can just be text (for ex, a * in the middle of a sentence doesn't
        /// begin an emphasis).
        /// </summary>
        internal class Token
        {
            public TokenType Type { get; }
            public string Value { get; }

            internal Token(TokenType type, string value)
            {
                Debug.Assert(type != TokenType.Delimiter || value.Length <= 2);

                this.Type = type;
                this.Value = value;
            }

            public static IEnumerable<Token> Tokenize(string text)
            {
                var textStart = -1;
                for (var i = 0; i < text.Length; i++)
                {
                    switch (text[i])
                    {
                        case '*':
                            {
                                if (textStart >= 0 && i - textStart > 0)
                                {
                                    //
                                    // Flush previous text token, if non-empty.
                                    //
                                    yield return new Token(TokenType.Text, text.Substring(textStart, i - textStart));
                                    textStart = -1;
                                }

                                if (i + 1 < text.Length && text[i + 1] == '*')
                                {
                                    i++;
                                    yield return new Token(TokenType.Delimiter, "**");
                                }
                                else
                                {
                                    yield return new Token(TokenType.Delimiter, "*");
                                }
                                break;
                            }

                        case '_':
                        case '`':
                        case '[':
                        case ']':
                        case '(':
                        case ')':
                            //
                            // Delimiter.
                            //
                            if (textStart >= 0 && i - textStart > 0)
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

            public static bool operator ==(Token lhs, Token rhs)
            {
                if (lhs is null)
                {
                    return rhs is null;
                }
                else
                {
                    return lhs.Equals(rhs);
                }
            }

            public static bool operator !=(Token lhs, Token rhs) => !(lhs == rhs);

            public override int GetHashCode()
            {
                return this.Value.GetHashCode();
            }
        }

        /// <summary>
        /// Container for formatted text.
        /// </summary>
        public class SpanNode : Node
        {
            protected override string Summary => $"[Span]";

            internal SpanNode(string text)
            {
                TryConsume(text);
            }

            protected SpanNode()
            {
            }

            protected SpanNode CreateSpanNode(Token token, IEnumerable<Token> remainder)
            {
                if (token.Type == TokenType.Text)
                {
                    return new TextNode(token.Value, true);
                }

                if ((token.Value == "_" || token.Value == "*" || token.Value == "**" || token.Value == "`") &&
                    remainder.FirstOrDefault() is Token next &&
                    next != null! &&
                    next.Type == TokenType.Text &&
                    next.Value.Length >= 1 &&
                    !NonLineBreakingWhitespace.Contains(next.Value[0]))
                {
                    return new EmphasisNode(token.Value);
                }
                else if (token.Value == "[" &&
                    remainder
                        .SkipWhile(t => t != new Token(TokenType.Delimiter, "]"))
                        .Skip(1)
                        .FirstOrDefault() == new Token(TokenType.Delimiter, "("))
                {
                    return new LinkNode();
                }
                else
                {
                    return new TextNode(token.Value, false);
                }
            }

            protected override sealed bool TryConsume(string line)
            {
                if (this.lastChild != null && string.IsNullOrWhiteSpace(line))
                {
                    //
                    // A blank line indicates a new paragraph, so we must
                    // not consume this.
                    //
                    return false;
                }
                else if (this.lastChild != null)
                {
                    //
                    // Inject a space to compensate for the line break.
                    //
                    line = " " + line;
                }

                //
                // Break the line into tokens and build a tree
                // that represents the formatting.
                //
                var tokens = Token.Tokenize(line);
                while (tokens.Any())
                {
                    var token = tokens.First();
                    var remainder = tokens.Skip(1);
                    TryConsumeToken(token, remainder);
                    tokens = remainder;
                }

                return true;
            }

            protected virtual bool TryConsumeToken(Token token, IEnumerable<Token> remainder)
            {
                if (this.lastChild != null &&
                    ((SpanNode)this.lastChild).TryConsumeToken(token, remainder))
                {
                    //
                    // Continuation of last span.
                    //
                    return true;
                }
                else
                {
                    //
                    // Last block is closed, append a new block.
                    //
                    AppendNode(CreateSpanNode(token, remainder));
                    return true;
                }
            }
        }

        /// <summary>
        /// Unformatted text.
        /// </summary>
        public class TextNode : SpanNode
        {
            private readonly bool space;
            public string Text { get; protected set; }

            protected override string Summary => $"[Text] {this.Text}";

            public TextNode(string text, bool space)
            {
                this.Text = text;
                this.space = space;
            }

            protected override bool TryConsumeToken(Token token, IEnumerable<Token> remainder)
            {
                if (token.Type == TokenType.Delimiter)
                {
                    return false;
                }
                else
                {
                    if (this.space)
                    {
                        this.Text = this.Text + token.Value;
                    }
                    else
                    {
                        this.Text += token.Value;
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Strong or normal emphasis
        /// </summary>
        public class EmphasisNode : SpanNode
        {
            private readonly string delimiter;
            private bool bodyCompleted = false;

            public string Text { get; protected set; } = string.Empty;

            public bool IsStrong => this.delimiter == "**";
            public bool IsCode => this.delimiter == "`";

            public EmphasisNode(string delimiter)
            {
                this.delimiter = delimiter;
            }

            protected override string Summary => $"[Emphasis delimiter={this.delimiter}] {this.Text}";

            protected override bool TryConsumeToken(Token token, IEnumerable<Token> remainder)
            {
                if (this.bodyCompleted)
                {
                    return false;
                }
                else if (token.Type == TokenType.Delimiter && token.Value == this.delimiter)
                {
                    //
                    // Eat the delimiter.
                    //
                    this.bodyCompleted = true;
                    return true;
                }
                else if (token.Type == TokenType.Text)
                {
                    this.Text += token.Value;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Link. Links can contain formatted text.
        /// </summary>
        public class LinkNode : SpanNode
        {
            private bool linkBodyCompleted = false;
            private bool linkHrefCompleted = false;
            protected override string Summary => $"[Link href={this.Href}]";
            public string Href { get; protected set; } = string.Empty;

            protected override bool TryConsumeToken(Token token, IEnumerable<Token> remainder)
            {
                if (this.linkHrefCompleted)
                {
                    //
                    // Link completed.
                    //
                    return false;
                }
                else if (this.linkBodyCompleted)
                {
                    //
                    // Building the link href.
                    //
                    if (this.Href == string.Empty && token == new Token(TokenType.Delimiter, "("))
                    {
                        return true;
                    }
                    else if (token == new Token(TokenType.Delimiter, ")"))
                    {
                        this.linkHrefCompleted = true;
                        return true;
                    }
                    else
                    {
                        this.Href += token.Value;
                        return true;
                    }
                }
                else
                {
                    //
                    // Building the link body/text.
                    //
                    if (token == new Token(TokenType.Delimiter, "]"))
                    {
                        this.linkBodyCompleted = true;
                        return true;
                    }
                    else
                    {
                        return base.TryConsumeToken(token, remainder);
                    }
                }
            }
        }
    }
}
