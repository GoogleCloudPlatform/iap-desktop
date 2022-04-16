//
// Copyright 2022 Google LLC
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

using Google.Apis.Util;
using Google.Solutions.Common.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    public static class TerminalExtensions
    {
        private class TerminalDataDecoder : IRawTerminal
        {
            private readonly ITextTerminal receiver;
            private readonly StreamingDecoder decoder;

            public TerminalDataDecoder(
                ITextTerminal receiver,
                Encoding encoding)
            {
                Utilities.ThrowIfNull(receiver, nameof(receiver));
                Utilities.ThrowIfNull(encoding, nameof(encoding));

                this.receiver = receiver;
                this.decoder = new StreamingDecoder(
                    encoding,
                    s => receiver.OnDataReceived(s));
            }
            public string TerminalType => this.receiver.TerminalType;

            public CultureInfo Locale => this.receiver.Locale;

            public void OnDataReceived(byte[] data, uint offset, uint length)
            {
                this.decoder.Decode(data, (int)offset, (int)length);
            }

            public void OnError(Exception exception)
            {
                this.receiver.OnError(exception);
            }

        }

        private class SynchronizationContextBoundTerminal : ITextTerminal
        {
            private readonly ITextTerminal terminal;
            private readonly SynchronizationContext context;

            public SynchronizationContextBoundTerminal(
                ITextTerminal terminal,
                SynchronizationContext context)
            {
                this.terminal = terminal.ThrowIfNull(nameof(terminal));
                this.context = context.ThrowIfNull(nameof(context));
            }

            public string TerminalType
                => this.context.Send(() => this.terminal.TerminalType);

            public CultureInfo Locale
                => this.context.Send(() => this.terminal.Locale);

            public void OnDataReceived(string data)
            {
                this.context.Post(() =>
                {
                    try
                    {
                        this.terminal.OnDataReceived(data);
                    }
                    catch (Exception e)
                    {
                        this.terminal.OnError(e);
                    }
                });
            }

            public void OnError(Exception exception)
            {
                this.context.Post(() => this.terminal.OnError(exception));
            }
        }

        //---------------------------------------------------------------------
        // Extension methods.
        //---------------------------------------------------------------------

        /// <summary>
        /// Create a raw terminal that forwards callbacks
        /// to the terminal.
        /// </summary>
        public static IRawTerminal ToRawTerminal(
            this ITextTerminal terminal,
            Encoding encoding)
            => new TerminalDataDecoder(terminal, encoding);

        /// <summary>
        /// Create a terminal that runs callbacks on a specific
        /// synchronization context.
        /// </summary>
        public static ITextTerminal BindToSynchronizationContext(
            this ITextTerminal terminal,
            SynchronizationContext targetContext)
            => new SynchronizationContextBoundTerminal(
                terminal,
                targetContext);
    }
}
