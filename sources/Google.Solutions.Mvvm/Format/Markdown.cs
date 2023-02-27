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
using System.Linq;

namespace Google.Solutions.Mvvm.Format
{
    internal class Markdown
    {
        /// <summary>
        /// Reader that allows peeking ahead and behind.
        /// </summary>
        internal class Reader
        {
            private readonly char[] buffer;
            private int nextIndex;

            public Reader(string input)
            {
                // Normalize line endings
                this.buffer = input.Replace("\r", "").ToArray();
            }

            public bool CanPeekBehind(ushort delta)
            {
                return this.nextIndex - delta >= 0;
            }

            public char PeekBehind(ushort delta = 1)
            {
                if (!CanPeekBehind(delta))
                {
                    throw new InvalidOperationException("BOF");
                }

                return this.buffer[this.nextIndex - delta];
            }

            public char PeekAhead()
            {
                if (this.IsEof)
                {
                    throw new InvalidOperationException("EOF");
                }

                return this.buffer[this.nextIndex];
            }

            public char Consume()
            {
                var c = PeekAhead();
                this.nextIndex++;
                return c;
            }


            public bool IsBof => this.nextIndex == 0;
            public bool IsEof => this.nextIndex == this.buffer.Length;
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
        /// Lexer for a subset of Markdown. We support the following:
        /// 
        /// - AtxHeading1-6
        /// </summary>
        internal class MarkdownLexer
        {

            private readonly List<Token> tokens = new List<Token>();

            public IEnumerable<Token> Tokens => this.tokens;

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

            public MarkdownLexer(Reader reader)
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
                                this.tokens.Add(new Token(TokenType.ParagraphBreak));
                            }
                            break;

                        case ' ':
                            if (!reader.IsEof && reader.PeekAhead() == ' ')
                            {
                                this.tokens.Add(new Token(TokenType.Indent));
                                reader.Consume();
                            }
                            else
                            {
                                goto default;
                            }
                            break;

                        case '\t':
                            this.tokens.Add(new Token(TokenType.Indent));
                            break;

                        case '[':
                            this.tokens.Add(new Token(TokenType.LeftBrace));
                            break;

                        case ']':
                            this.tokens.Add(new Token(TokenType.RightBrace));
                            break;

                        case '(':
                            this.tokens.Add(new Token(TokenType.LeftParanthesis));
                            break;

                        case ')':
                            this.tokens.Add(new Token(TokenType.RightParanethesis));
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

                                this.tokens.Add(new Token(TokenType.AtxHeading1 + hashes - 1, restOfLine.Trim()));
                                break;
                            }

                        case '+':
                        case '-':
                            if (!reader.IsEof && IsSpaceOrTab(reader.PeekAhead()))
                            {
                                reader.Consume(); // Eat the whitespace.
                                this.tokens.Add(new Token(TokenType.ListItem, c.ToString()));
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
                                this.tokens.Add(new Token(TokenType.ListItem, c.ToString()));
                                break;
                            }
                            else if (!reader.IsEof && reader.PeekAhead() == '*')
                            {
                                reader.Consume();
                                this.tokens.Add(new Token(TokenType.DoubleAsterisk));
                                break;
                            }
                            else
                            {
                                this.tokens.Add(new Token(TokenType.Asterisk));
                                break;
                            }

                        case '_':
                            this.tokens.Add(new Token(TokenType.Underscore));
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

                                this.tokens.Add(new Token(TokenType.Text, text));
                                break;
                            }
                    }
                }
            }
        }
    }

    public class MarkdownException : FormatException
    {
        public MarkdownException(string message) : base(message)
        {
        }
    }
}
