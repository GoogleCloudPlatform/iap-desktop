//
// Copyright 2024 Google LLC
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
using Google.Solutions.Mvvm.Controls;
using System;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Event data for terminal input events.
    /// </summary>
    public class VirtualTerminalInputEventArgs : EventArgs
    {
        /// <summary>
        /// Xterm-encoded data.
        /// </summary>
        public string Data { get; }

        internal VirtualTerminalInputEventArgs(string data)
        {
            this.Data = data.ExpectNotNull(nameof(data));
        }

        public override string ToString()
        {
            return this.Data;
        }
    }

    /// <summary>
    /// Event data for terminal output events.
    /// </summary>
    public class VirtualTerminalOutputEventArgs : EventArgs
    {
        /// <summary>
        /// Xterm-encoded data.
        /// </summary>
        public string Data { get; }

        internal VirtualTerminalOutputEventArgs(string data)
        {
            this.Data = data.ExpectNotNull(nameof(data));
        }

        public override string ToString()
        {
            return this.Data;
        }
    }

    /// <summary>
    /// Event data for terminal error events.
    /// </summary>
    public class VirtualTerminalErrorEventArgs : ExceptionEventArgs
    {
        internal VirtualTerminalErrorEventArgs(Exception e) : base(e)
        {
        }

        public override string ToString()
        {
            return this.Exception.ToString();
        }
    }
}
