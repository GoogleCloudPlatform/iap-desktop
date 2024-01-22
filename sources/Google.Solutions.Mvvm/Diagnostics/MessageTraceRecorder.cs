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

using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Diagnostics
{
    public sealed class MessageTraceRecorder : IMessageFilter, IDisposable
    {
        /// <summary>
        /// Circular buffer containing last N messages.
        /// </summary>
        private readonly Message[] history;
        private int nextIndex = 0;

        public MessageTraceRecorder(int count)
        {
            if (count < 0)
            {
                throw new ArgumentException(nameof(count));
            }

            this.history = new Message[count];

            Application.AddMessageFilter(this);
        }

        internal void Record(ref Message m)
        {
            //
            // Record message in circular buffer. We're operating
            // on the STA thread, so there's no need for 
            // synchronization.
            //
            this.history[this.nextIndex] = m;
            this.nextIndex = (this.nextIndex + 1) % this.history.Length;
        }

        public bool PreFilterMessage(ref Message m)
        {
            Record(ref m);
            return false;
        }

        /// <summary>
        /// Capture a snapshot.
        /// </summary>
        /// <returns></returns>
        public MessageTrace Capture()
        {
            var orderedMessages = new Message[this.history.Length];

            for (var i = 0; i < this.history.Length; i++)
            {
                orderedMessages[i] = this.history[(this.nextIndex + i) % this.history.Length];
            }

            return new MessageTrace(orderedMessages);
        }

        public void Dispose()
        {
            Application.RemoveMessageFilter(this);
        }
    }

    public class MessageTrace
    {
        public IReadOnlyList<Message> History { get; }

        internal MessageTrace(IReadOnlyList<Message> messages)
        {
            this.History = messages.ExpectNotNull(nameof(messages));
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();

            foreach (var message in this.History)
            {
                buffer.Append(
                    $"0x{message.Msg:X8} (LParam: 0x{message.LParam.ToInt64():X16}, " +
                    $"WParam: 0x{message.WParam.ToInt64():X16}, {(WindowMessage)message.Msg})\n");
            }

            return buffer.ToString();
        }
    }
}
