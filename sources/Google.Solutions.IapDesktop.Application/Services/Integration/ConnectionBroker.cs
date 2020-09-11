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

using Google.Solutions.Common.Locator;
using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Services.Integration
{
    public interface IConnectionBroker
    {
        bool IsConnected(InstanceLocator vmInstance);

        bool TryActivate(InstanceLocator vmInstance);
    }

    public interface IGlobalConnectionBroker : IConnectionBroker
    {
        void Register(IConnectionBroker broker);
    }

    /// <summary>
    /// Meta-broker that maintains a list of connection brokers
    /// and forwards requests to these.
    /// </summary>
    public class GlobalConnectionBroker : IGlobalConnectionBroker
    {
        private readonly LinkedList<IConnectionBroker> brokers =
            new LinkedList<IConnectionBroker>();
        private readonly object brokersLock = new object();

            
        public void Register(IConnectionBroker broker)
        {
            lock (this.brokersLock)
            {
                this.brokers.AddLast(broker);
            }
        }

        public bool IsConnected(InstanceLocator vmInstance)
        {
            lock (this.brokersLock)
            {
                foreach (var broker in this.brokers)
                {
                    if (broker.IsConnected(vmInstance))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryActivate(InstanceLocator vmInstance)
        {
            lock (this.brokersLock)
            {
                foreach (var broker in this.brokers)
                {
                    if (broker.TryActivate(vmInstance))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    //-------------------------------------------------------------------------
    // Events.
    //-------------------------------------------------------------------------

    public abstract class ConnectionBrokerEventBase
    {
        public InstanceLocator Instance { get; }

        public ConnectionBrokerEventBase(InstanceLocator vmInstance)
        {
            this.Instance = vmInstance;
        }
    }

    public class ConnectionSuceededEvent : ConnectionBrokerEventBase
    {
        public ConnectionSuceededEvent(InstanceLocator vmInstance) : base(vmInstance)
        {
        }
    }

    public class ConnectionFailedEvent : ConnectionBrokerEventBase
    {
        public Exception Exception { get; }

        public ConnectionFailedEvent(InstanceLocator vmInstance, Exception exception)
            : base(vmInstance)
        {
            this.Exception = exception;
        }
    }

    public class ConnectionClosedEvent : ConnectionBrokerEventBase
    {
        public Exception Exception { get; }

        public ConnectionClosedEvent(InstanceLocator vmInstance) : base(vmInstance)
        {
        }
    }
}
