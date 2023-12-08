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
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Diagnostics
{
    /// <summary>
    /// Debug only: Throttles window messages by introducing a sleep
    /// between each message.
    /// 
    /// Can be enabled and disabled by pressing F12.
    /// </summary>
    public sealed class DebugMessageThrottle : IMessageFilter, IDisposable
    {
        private readonly int delay;

        public DebugMessageThrottle(TimeSpan delay)
        {
            this.delay = (int)delay.TotalMilliseconds;

#if DEBUG
            Application.AddMessageFilter(this);
#endif
        }

        public bool IsThrottled { get; private set; } = false;

        public bool PreFilterMessage(ref Message m)
        {
            if ((WindowMessage)m.Msg == WindowMessage.WM_KEYDOWN &&
                (Keys)m.WParam.ToInt32() == Keys.F12)
            {
                //
                // Toggle throttled state.
                //
                this.IsThrottled = !this.IsThrottled;
            }

            if (this.IsThrottled)
            {
                Thread.Sleep(this.delay);
            }

            return false;
        }

        public void Dispose()
        {
#if DEBUG
            Application.RemoveMessageFilter(this);
#endif
        }
    }
}
