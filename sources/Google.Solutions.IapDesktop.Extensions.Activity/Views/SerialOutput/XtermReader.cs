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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VtNetCore.Exceptions;
using VtNetCore.VirtualTerminal;
using VtNetCore.XTermParser;
using VtNetCore.XTermParser.SequenceType;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.SerialOutput
{
    /// <summary>
    /// Reader that extracts pure text out of a string containing
    /// ANSI escape sequences.
    /// </summary>
    public class XtermReader : IAsyncReader<string>
    {
        private readonly IAsyncReader<string> reader;
        private readonly VirtualTerminalController controller;
        private readonly XTermInputBuffer buffer;

        public XtermReader(IAsyncReader<string> reader)
        {
            this.reader = reader;
            this.controller = new VirtualTerminalController();
            this.buffer = new XTermInputBuffer();
        }

        private IEnumerable<TerminalSequence> ParseSequences(string rawData)
        {
            this.buffer.Add(Encoding.UTF8.GetBytes(rawData));

            var sequences = new List<TerminalSequence>();
            this.controller.ClearChanges();

            while (!this.buffer.AtEnd)
            {
                try
                {
                    var terminalSequence = XTermSequenceReader.ConsumeNextSequence(
                        this.buffer,
                        this.controller.IsUtf8());
                    if (terminalSequence.ProcessFirst != null)
                    {
                        sequences.AddRange(terminalSequence.ProcessFirst);
                    }

                    sequences.Add(terminalSequence);
                }
                catch (IndexOutOfRangeException)
                {
                    this.buffer.PopAllStates();
                    break;
                }
                catch (ArgumentException)
                {
                    if (!this.buffer.AtEnd)
                    {
                        this.buffer.ReadRaw();
                    }

                    this.buffer.Commit();
                }
                catch (EscapeSequenceException)
                {
                    if (!this.buffer.AtEnd)
                    {
                        this.buffer.ReadRaw();
                    }

                    this.buffer.Commit();
                }
                catch (Exception ex4)
                {
                    throw new InvalidOperationException("Failed to process xterm input", ex4);
                }
            }

            this.buffer.Flush();

            return sequences;
        }

        public async Task<string> ReadAsync(CancellationToken token)
        {
            var chunk = await this.reader
                .ReadAsync(token)
                .ConfigureAwait(false);

            return new string(ParseSequences(chunk)
                .OfType<CharacterSequence>()
                .Select(s => s.Character)
                .ToArray());
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
}
