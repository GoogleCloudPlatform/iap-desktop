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
using System.Linq;

namespace Google.Solutions.Mvvm.Format
{
    internal class Markdown
    {
        /// <summary>
        /// Reader that allows peeking ahead and behind.
        /// </summary>
        internal abstract class ReaderBase<T>
        {
            protected readonly IList<T> items;
            private int nextIndex = 0;

            protected ReaderBase(IList<T> buffer)
            {
                this.items = buffer;
            }

            public bool CanPeekAhead(uint delta)
            {
                return this.nextIndex + (int)delta - 1< this.items.Count;
            }

            public bool CanPeekBehind(uint delta)
            {
                return this.nextIndex - delta >= 0;
            }

            public T PeekBehind(uint delta = 1)
            {
                if (!CanPeekBehind(delta))
                {
                    throw new InvalidOperationException("BOF");
                }

                return this.items[this.nextIndex - (int)delta];
            }

            public T PeekAhead(uint delta = 1)
            {
                if (!CanPeekAhead(delta))
                {
                    throw new InvalidOperationException("EOF");
                }

                return this.items[this.nextIndex + (int)delta - 1];
            }

            public T Consume()
            {
                var c = PeekAhead();
                this.nextIndex++;
                return c;
            }


            public bool IsBof => this.nextIndex == 0;
            public bool IsEof => this.nextIndex == this.items.Count;
        }

        internal class Reader : ReaderBase<char>
        {
            public Reader(string input)
                : base(input.Replace("\r", "").ToArray()) // Normalize line endings
            { 
            }
        }

        public enum TokenType
        {
            AtxHeading1,
            AtxHeading2,
            AtxHeading3,
            AtxHeading4,
            AtxHeading5,
            AtxHeading6,
            ParagraphBreak,
            Indent,
            LeftBrace,
            RightBrace,
            LeftParanthesis,
            RightParanethesis,
            ListItem,
            Text,
            Asterisk,
            DoubleAsterisk,
            Underscore
        }

        public struct Token
        {
            public TokenType Type;
            public string Lexeme;

            public Token(TokenType type) : this(type, null)
            {
            }

            public Token(TokenType type, string lexeme)
            {
                this.Type = type;
                this.Lexeme = lexeme;
            }

            public override string ToString()
            {
                var s = this.Type.ToString();

                if (this.Lexeme != null)
                {
                    s += $": '{this.Lexeme}'";
                }

                return s;
            }

            public override bool Equals(object obj)
            {
                return obj is Token token &&
                    token.Type == this.Type &&
                    token.Lexeme == this.Lexeme;
            }

            public override int GetHashCode()
            {
                return this.Type + this.Lexeme != null ? this.Lexeme.GetHashCode() : 0;
            }
        }

        /// <summary>
        /// Lexer for a subset of Markdown. 
        /// </summary>
        internal class Lexer : ReaderBase<Token>
        {
            private static bool IsSpaceOrTab(char c)
            {
                return c == ' ' || c == '\t';
            }

            private static bool IsAllWhitespaceBehindInLine(Reader reader)
            {
                //
                // Start by looking 2 behind since we already consumed an item.
                //
                for (ushort i = 2; reader.CanPeekBehind(i); i++)
                {
                    var c = reader.PeekBehind(i);
                    if (c == '\n')
                    {
                        return true;
                    }
                    else if (!IsSpaceOrTab(c))
                    {
                        return false;
                    }
                }

                return true;
            }

            public Lexer(Reader reader)
                : base(new List<Token>())
            {
                while (!reader.IsEof)
                {
                    var c = reader.Consume();
                    switch (c)
                    {
                        case '\n':
                            if (!reader.IsEof && reader.PeekAhead() == '\n')
                            {
                                reader.Consume();
                                this.items.Add(new Token(TokenType.ParagraphBreak));
                            }
                            break;

                        case ' ':
                            if (!reader.IsEof && reader.PeekAhead() == ' ')
                            {
                                this.items.Add(new Token(TokenType.Indent));
                                reader.Consume();
                            }
                            else
                            {
                                goto default;
                            }
                            break;

                        case '\t':
                            this.items.Add(new Token(TokenType.Indent));
                            break;

                        case '[':
                            this.items.Add(new Token(TokenType.LeftBrace));
                            break;

                        case ']':
                            this.items.Add(new Token(TokenType.RightBrace));
                            break;

                        case '(':
                            this.items.Add(new Token(TokenType.LeftParanthesis));
                            break;

                        case ')':
                            this.items.Add(new Token(TokenType.RightParanethesis));
                            break;

                        case '#':
                            {
                                int hashes = 1;
                                while (!reader.IsEof && reader.PeekAhead() == '#')
                                {
                                    hashes++;
                                    reader.Consume();
                                }

                                if (hashes > 6)
                                {
                                    hashes = 6;
                                }

                                var restOfLine = string.Empty;
                                while (!reader.IsEof && reader.PeekAhead() != '\n')
                                {
                                    restOfLine += reader.Consume();
                                }

                                this.items.Add(new Token(TokenType.AtxHeading1 + hashes - 1, restOfLine.Trim()));
                                break;
                            }

                        case '+':
                        case '-':
                            if (!reader.IsEof && IsSpaceOrTab(reader.PeekAhead()))
                            {
                                reader.Consume(); // Eat the whitespace.
                                this.items.Add(new Token(TokenType.ListItem, c.ToString()));
                                break;
                            }
                            else
                            {
                                goto default;
                            }

                        case '*':
                            if (!reader.IsEof && IsSpaceOrTab(reader.PeekAhead()) &&
                                IsAllWhitespaceBehindInLine(reader))
                            {
                                reader.Consume(); // Eat the whitespace.
                                this.items.Add(new Token(TokenType.ListItem, c.ToString()));
                                break;
                            }
                            else if (!reader.IsEof && reader.PeekAhead() == '*')
                            {
                                reader.Consume();
                                this.items.Add(new Token(TokenType.DoubleAsterisk));
                                break;
                            }
                            else
                            {
                                this.items.Add(new Token(TokenType.Asterisk));
                                break;
                            }

                        case '_':
                            this.items.Add(new Token(TokenType.Underscore));
                            break;


                        default:
                            {
                                var text = c.ToString();
                                while (!reader.IsEof &&
                                        reader.PeekAhead() != '\n' &&
                                        reader.PeekAhead() != '(' &&
                                        reader.PeekAhead() != ')' &&
                                        reader.PeekAhead() != '[' &&
                                        reader.PeekAhead() != ']' &&
                                        reader.PeekAhead() != '*' &&
                                        reader.PeekAhead() != '_')
                                {
                                    text += reader.Consume();
                                }

                                this.items.Add(new Token(TokenType.Text, text));
                                break;
                            }
                    }
                }
            }

            public IEnumerable<Token> Tokens => this.items;
        }


















        //// https://github.com/Domysee/MarkdownEbnf/blob/master/Markdown%20EBNF.txt
        //// https://spec.commonmark.org/0.30/
        //// https://alajmovic.com/posts/writing-a-markdown-ish-parser/


        //public abstract class NodeBase
        //{
        //}

        //public class Heading : NodeBase
        //{
        //    public readonly ushort Level;
        //    public readonly string Text;

        //    internal Heading(Lexer lexer)
        //    {
        //        Debug.Assert(!lexer.IsEof);
        //        Debug.Assert(IsHeadingAhead(lexer));

        //        var token = lexer.Consume();
        //        this.Level = (ushort)(token.Type - TokenType.AtxHeading1 + 1);
        //        this.Text = token.Lexeme.Trim();
        //    }

        //    internal static bool IsHeadingAhead(Lexer lexer)
        //    {
        //        return lexer.CanPeekAhead(1) &&
        //            lexer.PeekAhead(1).Type >= TokenType.AtxHeading1 &&
        //            lexer.PeekAhead(1).Type <= TokenType.AtxHeading6;
        //    }
        //}

        //public enum Format
        //{
        //    Regular,
        //    Emphasis,
        //    StrongEmphasis
        //}

        //public class InlineTextContent : NodeBase
        //{
        //    public readonly Format Format;
        //    public readonly string Text;

        //    internal InlineTextContent(Lexer lexer)
        //    {
        //        Debug.Assert(!lexer.IsEof);
        //        Debug.Assert(IsTextContentAhead(lexer, out var _));

        //        var token = lexer.Consume();
        //        if (token.Type == TokenType.Asterisk)
        //        {
        //            lexer.Consume(); // Eat *
        //            this.Format = Format.Emphasis;
        //            this.Text = lexer.Consume().Lexeme;
        //            lexer.Consume(); // Eat *
        //        }
        //        else if (token.Type == TokenType.DoubleAsterisk)
        //        {
        //            lexer.Consume(); // Eat **
        //            this.Format = Format.StrongEmphasis;
        //            this.Text = lexer.Consume().Lexeme;
        //            lexer.Consume(); // Eat **
        //        }
        //        else
        //        {
        //            this.Format = Format.Regular;
        //            this.Text = token.Lexeme;
        //        }
        //    }

        //    public static bool IsInlineTextContentAhead(
        //        Lexer lexer, 
        //        uint tokenOffset,
        //        out uint tokensToConsume)
        //    {
        //        if (lexer.CanPeekAhead(tokenOffset + 1) && lexer.PeekAhead(tokenOffset + 1).Type == TokenType.Text)
        //        {
        //            tokensToConsume = 1;
        //            return true;
        //        }
        //        else if (
        //            lexer.CanPeekAhead(tokenOffset + 3) &&
        //            (lexer.PeekAhead(tokenOffset + 1).Type == TokenType.Asterisk || 
        //                lexer.PeekAhead(tokenOffset + 1).Type == TokenType.DoubleAsterisk) &&
        //            lexer.PeekAhead(tokenOffset + 2).Type == TokenType.Text &&
        //            (lexer.PeekAhead(tokenOffset + 3).Type == TokenType.Asterisk || 
        //                lexer.PeekAhead(tokenOffset + 3).Type == TokenType.DoubleAsterisk))
        //        {
        //            tokensToConsume = 3;
        //            return true;
        //        }
        //        else
        //        {
        //            tokensToConsume = 0;
        //            return false;
        //        }
        //    }
        //}

        //public class TextContent : NodeBase
        //{
        //    public readonly List<InlineTextContent> content;

        //    internal TextContent(Lexer lexer)
        //    {
        //        Debug.Assert(!lexer.IsEof);

        //    }

        //    public static bool IsTextContentAhead(Lexer lexer, out uint tokensToConsume)
        //    {
        //        tokensToConsume = 0;

        //        while (InlineTextContent.IsInlineTextContentAhead(lexer, tokensToConsume, out var more))
        //        {
        //            tokensToConsume += more;
        //        }

        //        return tokensToConsume > 0;
        //    }
        //}


        //public class Link
        //{
        //    public readonly InlineTextContent Text;
        //    public readonly string Href;

        //    public Link(Lexer lexer)
        //    {
        //        Debug.Assert(!lexer.IsEof);
        //        Debug.Assert(IsLinkAhead(lexer));

        //        lexer.Consume(); // Eat [
        //        this.Text = new InlineTextContent(lexer);
        //        lexer.Consume(); // Eat ]

        //        lexer.Consume(); // Eat (
        //        this.Href = lexer.Consume().Lexeme;
        //        lexer.Consume(); // Eat )
        //    }

        //    internal static bool IsLinkAhead(Lexer lexer)
        //    {
        //        return lexer.CanPeekAhead(1) &&
        //            lexer.PeekAhead(1).Type == TokenType.LeftBrace &&
        //            TextContent.IsTextContentAhead(lexer, out var textContentTokens) &&
        //            lexer.CanPeekAhead(textContentTokens + 5) &&
        //            lexer.PeekAhead(1 + textContentTokens + 1).Type == TokenType.RightBrace &&
        //            lexer.PeekAhead(1 + textContentTokens + 2).Type == TokenType.LeftParanthesis &&
        //            lexer.PeekAhead(1 + textContentTokens + 3).Type == TokenType.Text &&
        //            lexer.PeekAhead(1 + textContentTokens + 4).Type == TokenType.RightParanethesis;
        //    }
        //}

        //public class LeafBlock : NodeBase
        //{
        //    internal LeafBlock(Lexer lexer)
        //    {
        //        var nodes = new List<NodeBase>();

        //        while (!lexer.IsEof)
        //        {
        //            if (Heading.IsHeadingAhead(lexer))
        //            {
        //                nodes.Add(new Heading(lexer));
        //            }
        //            else if (Link.IsLinkAhead(lexer))
        //            {
        //                nodes.Add(new Link(lexer));
        //            }

        //            switch (lexer.PeekAhead().Type)
        //            {
        //                case TokenType.AtxHeading1:
        //                case TokenType.AtxHeading2:
        //                case TokenType.AtxHeading3:
        //                case TokenType.AtxHeading4:
        //                case TokenType.AtxHeading5:
        //                case TokenType.AtxHeading6:
        //                    nodes.Add(new Heading(lexer));
        //                    break;

        //                case TokenType.ParagraphBreak:
        //                    nodes.Add(new Paragraph(lexer)):
        //                    break;


        //                case TokenType.LeftBrace:

        //                case TokenType.Indent:
        //                    break;

        //                case TokenType.RightBrace:
        //                case TokenType.LeftParanthesis:
        //                case TokenType.RightParanethesis:
        //                case TokenType.ListItem:
        //                    break;
        //            }
        //        }
        //    }
        //}

        //public class Parser
        //{
        //    private readonly Lexer lexer;

        //    public Parser(Lexer lexer)
        //    {
        //        this.lexer = lexer;

        //        while (!lexer.IsEof)
        //        {
        //            switch  (lexer.PeekAhead().Type)
        //            {
        //                case TokenType.AtxHeading1:
        //                case TokenType.AtxHeading2:
        //                case TokenType.AtxHeading3:
        //                case TokenType.AtxHeading4:
        //                case TokenType.AtxHeading5:
        //                case TokenType.AtxHeading6:
        //                    ret
        //                    break;

        //                case TokenType.ParagraphBreak:
        //                    break;

        //                case TokenType.Indent:
        //                    break;

        //                case TokenType.LeftBrace:
        //                case TokenType.RightBrace:
        //                case TokenType.LeftParanthesis:
        //                case TokenType.RightParanethesis:
        //                case TokenType.ListItem:
        //                case TokenType.Text:
        //                case TokenType.Asterisk:
        //                case TokenType.DoubleAsterisk:
        //                case TokenType.Underscore:
        //                    break;
        //            }
        //        }
        //    }

        //}
    }

    public class MarkdownException : FormatException
    {
        public MarkdownException(string message) : base(message)
        {
        }
    }
}
