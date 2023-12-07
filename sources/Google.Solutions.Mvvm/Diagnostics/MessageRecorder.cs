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

using Google.Solutions.Mvvm.Interop;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Diagnostics
{
    public sealed class MessageRecorder : IMessageFilter, IDisposable
    {
        /// <summary>
        /// Circular buffer containing last N messages.
        /// </summary>
        private readonly RecordedMessage[] history;
        private int nextIndex = 0;

        public MessageRecorder(int count) 
        {
            if (count < 0) 
            {
                throw new ArgumentException(nameof(count));
            }

            this.history = new RecordedMessage[count];

            Application.AddMessageFilter(this);
        }

        internal void Record(ref Message m)
        {
            //
            // Record message in circular buffer. We're operating
            // on the STA thread, so there's no need for 
            // synchronization.
            //
            this.history[this.nextIndex].Initialize(m);
            this.nextIndex = (this.nextIndex + 1) % this.history.Length;
        }

        public bool PreFilterMessage(ref Message m)
        {
            Record(ref m);
            return false;
        }

        public IEnumerable<RecordedMessage> History
        {
            get
            {
                for (int i = 0; i < this.history.Length; i++) 
                {
                    yield return this.history[(this.nextIndex + i) % this.history.Length];
                }
            }
        }

        public void Dispose()
        {
            Application.RemoveMessageFilter(this);
        }

        public struct RecordedMessage
        {
            public int Msg;
            public IntPtr Lparam;
            public IntPtr Wparam;

            internal void Initialize(Message msg)
            {
                this.Msg = msg.Msg;
                this.Lparam = msg.LParam;
                this.Wparam = msg.WParam;
            }

            public override string ToString()
            {
                return 
                    $"0x{this.Msg:X8} (LParam: 0x{this.Lparam.ToInt64():X16}, " +
                    $"WParam: 0x{this.Wparam.ToInt64():X16}, {(WindowMessage)this.Msg})";
            }
        }
    }
}
