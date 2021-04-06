//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1032 // Implement standard exception constructors

namespace Google.Solutions.IapDesktop.Extensions.Activity.Views.SerialOutput
{
    public struct AnsiTextToken
    {
        public enum TokenType
        {
            Text,
            Command
        }

        public TokenType Type;
        public string Value;
    }

    /// <summary>
    /// Tokenizer for streams containing ANSI escape sequences. 
    /// </summary>
    public class AnsiTokenReader : IAsyncReader<IEnumerable<AnsiTextToken>>
    {
        public const char Escape = (char)0x1B;
        public const char ControlSequenceIntroducer = '[';

        private readonly IAsyncReader<string> reader;

        private string leftover = string.Empty;
        private bool lastInputRead = false;

        private enum ScannerState
        {
            InText,
            InEscapeSequence,
            InCsiSequence
        }

        public AnsiTokenReader(IAsyncReader<string> reader)
        {
            this.reader = reader;
        }

        private IEnumerable<AnsiTextToken> Tokenize(string text)
        {
            if (text == null)
            {
                Debug.Assert(this.leftover.Length > 0);

                // Return any leftover as text.
                yield return new AnsiTextToken()
                {
                    Type = AnsiTextToken.TokenType.Text,
                    Value = this.leftover
                };

                yield break;
            }

            // Restart scanning including the leftover.
            text = leftover + text;

            var buffer = new StringBuilder();
            var state = ScannerState.InText;

            foreach (char c in text)
            {
                switch (state)
                {
                    case ScannerState.InText:
                        {
                            if (c == Escape)
                            {
                                if (buffer.Length > 0)
                                {
                                    // Flush buffer.

                                    yield return new AnsiTextToken()
                                    {
                                        Type = AnsiTextToken.TokenType.Text,
                                        Value = buffer.ToString()
                                    };

                                    this.leftover = string.Empty;
                                    var value = buffer.ToString();
                                    buffer.Clear();
                                }

                                state = ScannerState.InEscapeSequence;
                                buffer.Append(c);
                            }
                            else
                            {
                                buffer.Append(c);
                            }

                            break;
                        }

                    case ScannerState.InEscapeSequence:
                        {
                            if (c == ControlSequenceIntroducer)
                            {
                                state = ScannerState.InCsiSequence;
                                buffer.Append(c);
                            }
                            else if (c == '%' || c == '(')
                            {
                                // Keep reading.
                                buffer.Append(c);
                            }
                            else if ((c >= '0' && c <= '_') || c == ')')
                            {
                                buffer.Append(c);

                                // Second (and last) character of sequence. Flush.
                                yield return new AnsiTextToken()
                                {
                                    Type = AnsiTextToken.TokenType.Command,
                                    Value = buffer.Remove(0, 1).ToString()
                                };

                                this.leftover = string.Empty;
                                buffer.Clear();
                                state = ScannerState.InText;
                            }
                            else
                            {
                                throw new AnsiFormatException($"Unrecognized escape sequence {buffer}{c}");
                            }

                            break;
                        }

                    case ScannerState.InCsiSequence:
                        {
                            Debug.Assert(buffer.Length >= 2);
                            Debug.Assert(buffer[0] == Escape);
                            Debug.Assert(buffer[1] == ControlSequenceIntroducer);

                            if (c >= '0' && c < '?')
                            {
                                // Parameter byte.
                                buffer.Append(c);
                            }
                            else if (c >= ' ' && c <= '/')
                            {
                                // Intermediate byte.
                                buffer.Append(c);
                            }
                            else if (c >= 'A' && c <= '~')
                            {
                                // Final byte.
                                buffer.Append(c);

                                // Flush.
                                yield return new AnsiTextToken()
                                {
                                    Type = AnsiTextToken.TokenType.Command,
                                    Value = buffer.Remove(0, 1).ToString()
                                };

                                this.leftover = string.Empty;
                                buffer.Clear();
                                state = ScannerState.InText;
                            }
                            else if (c == Escape)
                            {
                                // This is non-properly terminated CSI sequence. Drop it.
                                buffer.Clear();
                                state = ScannerState.InEscapeSequence;
                                buffer.Append(c);
                            }
                            else
                            {
                                throw new AnsiFormatException($"Unrecognized escape sequence {buffer}{c}");
                            }

                            break;
                        }
                }
            }

            // No more characters left to parse.
            switch (state)
            {
                case ScannerState.InText:
                    {
                        this.leftover = string.Empty;
                        if (buffer.Length > 0)
                        {
                            yield return new AnsiTextToken()
                            {
                                Type = AnsiTextToken.TokenType.Text,
                                Value = buffer.ToString()
                            };
                        }

                        break;
                    }

                case ScannerState.InEscapeSequence:
                case ScannerState.InCsiSequence:
                    {
                        // We are in the middle of a sequence here.
                        this.leftover = buffer.ToString();
                        break;
                    }
            }
        }

        public async Task<IEnumerable<AnsiTextToken>> ReadAsync(
            CancellationToken token)
        {
            if (this.lastInputRead)
            {
                return null;
            }

            var input = await this.reader
                .ReadAsync(token)
                .ConfigureAwait(false);
            if (input == null)
            {
                // End of stream. 
                this.lastInputRead = true;

                if (this.leftover.Length == 0)
                {
                    return null;
                }
            }

            // NB. Because Tokenize() maintains the 'leftover' state, re-evaluating
            // the Enumerable changes the state. Therefore, snapshot the resulting
            // Enumerable by turning it into a list.
            return Tokenize(input).ToList();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.reader.Dispose();
            }
        }
    }

    public class AnsiFormatException : Exception
    {
        public AnsiFormatException(string message) : base(message)
        {

        }
    }
}
