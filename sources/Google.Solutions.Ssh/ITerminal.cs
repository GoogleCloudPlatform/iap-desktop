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

using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    public interface ITerminal
    {
        /// <summary>
        /// Return terminal type ($TERM), such as "xterm".
        /// </summary>
        string TerminalType { get; }

        /// <summary>
        /// Language ($LC_ALL) of terminal.
        /// </summary>
        CultureInfo Locale { get; }

        /// <summary>
        /// Process decoded data received from remote peer.
        /// </summary>
        void OnDataReceived(string data);

        /// <summary>
        /// Handle communication errór.
        /// </summary>
        void OnError(Exception exception);
    }

    public interface IRawTerminal
    {
        /// <summary>
        /// Process raw data received from remote peer.
        /// </summary>
        void OnDataReceived(
            byte[] data,
            uint offset,
            uint length);

        /// <summary>
        /// Handle communication error.
        /// </summary>
        void OnError(Exception exception);
    }

    public static class TerminalExtensions
    {
        private class TerminalDataDecoder : IRawTerminal
        {
            private readonly ITerminal receiver;
            private readonly StreamingDecoder decoder;

            public TerminalDataDecoder(
                ITerminal receiver,
                Encoding encoding)
            {
                Utilities.ThrowIfNull(receiver, nameof(receiver));
                Utilities.ThrowIfNull(encoding, nameof(encoding));

                this.receiver = receiver;
                this.decoder = new StreamingDecoder(
                    encoding,
                    s => receiver.OnDataReceived(s));
            }

            public void OnDataReceived(byte[] data, uint offset, uint length)
            {
                this.decoder.Decode(data, (int)offset, (int)length);
            }

            public void OnError(Exception exception)
            {
                this.receiver.OnError(exception);
            }
        }

        public static IRawTerminal ToRawTerminal(
            this ITerminal terminal,
            Encoding encoding)
        {
            return new TerminalDataDecoder(terminal, encoding);
        }
    }
}
