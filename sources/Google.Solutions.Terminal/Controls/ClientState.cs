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

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Connection state of a client.
    /// </summary>
    public enum ClientState
    {
        /// <summary>
        /// Client not connected yet or an existing connection has 
        /// been lost.
        /// </summary>
        NotConnected,

        /// <summary>
        /// Client is in the process of connecting.
        /// </summary>
        Connecting,

        /// <summary>
        /// Client connected, but user log on hasn't completed yet.
        /// </summary>
        Connected,

        /// <summary>
        /// Client is disconnecting.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// User logged on, session is ready to use.
        /// </summary>
        LoggedOn
    }
}
