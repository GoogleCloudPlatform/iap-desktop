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

using Google.Solutions.Common.Threading;
using System;
using System.Globalization;
using System.Threading;

namespace Google.Solutions.Ssh
{
    public interface ITextTerminal // TODO: remove ITextTerminal
    {
        /// <summary>
        /// Return terminal type ($TERM), such as "xterm".
        /// </summary>
        string TerminalType { get; }

        /// <summary>
        /// Language ($LC_ALL) of terminal.
        /// </summary>
        CultureInfo? Locale { get; }

        /// <summary>
        /// Process decoded data received from remote peer.
        /// </summary>
        void OnDataReceived(string data);

        /// <summary>
        /// Handle communication error.
        /// </summary>
        void OnError(
            TerminalErrorType errorType,
            Exception exception);
    }

    public enum TerminalErrorType
    {
        ConnectionFailed,
        ConnectionLost,
        TerminalIssue
    }

    public class SynchronizedTextTerminal : ITextTerminal
    {
        private readonly ITextTerminal terminal;
        private readonly SynchronizationContext context;

        public SynchronizedTextTerminal(ITextTerminal terminal, SynchronizationContext context)
        {
            this.terminal = terminal;
            this.context = context;
        }

        public string TerminalType => this.terminal.TerminalType;

        public CultureInfo? Locale => this.terminal.Locale;

        public void OnDataReceived(string data)
        {
            this.context.Post(() => this.terminal.OnDataReceived(data));
        }

        public void OnError(TerminalErrorType errorType, Exception exception)
        {
            this.context.Post(() => this.terminal.OnError(errorType, exception));
        }
    }
}
