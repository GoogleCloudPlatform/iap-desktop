//
// Copyright 2023 Google LLC
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

using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Core.Transport
{
    /// <summary>
    /// Brokers transport objects, creating them as necessary,
    /// and sharing existing transports when possible.
    /// 
    /// * IAP transports are always shared.
    /// * VPC transports are never shared.
    /// 
    /// Callers must dispose transports when they're done. Once a
    /// transport's reference count drops to zero, it's closed.
    /// 
    /// Disposing the broker force-closes all transports.
    /// </summary>
    public interface ITransportBroker : ITransportFactory, IDisposable
    {
        /// <summary>
        /// Return a list of all currently active transports.
        /// </summary>
        IEnumerable<ITransport> Active { get; }
    }
}
