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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;

namespace Google.Solutions.IapDesktop.Application.Services.Integration
{
    public interface ISession : IDisposable
    {
        void Close();
    }

    public interface ISessionBroker
    {
        /// <summary>
        /// Return active session, or null if no session is active.
        /// </summary>
        ISession ActiveSession { get; }

        bool IsConnected(InstanceLocator vmInstance);

        bool TryActivate(InstanceLocator vmInstance);
    }

    public interface IGlobalSessionBroker : ISessionBroker
    {
    }

    /// <summary>
    /// Meta-broker that maintains a list of connection brokers
    /// and forwards requests to these.
    /// </summary>
    public class GlobalSessionBroker : IGlobalSessionBroker
    {
        private readonly IServiceCategoryProvider serviceProvider;

        public GlobalSessionBroker(IServiceCategoryProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public bool IsConnected(InstanceLocator vmInstance)
        {
            foreach (var broker in this.serviceProvider
                .GetServicesByCategory<ISessionBroker>())
            {
                if (broker.IsConnected(vmInstance))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryActivate(InstanceLocator vmInstance)
        {
            foreach (var broker in this.serviceProvider
                .GetServicesByCategory<ISessionBroker>())
            {
                if (broker.TryActivate(vmInstance))
                {
                    return true;
                }
            }

            return false;
        }

        public ISession ActiveSession
        {
            get
            {
                foreach (var broker in this.serviceProvider
                    .GetServicesByCategory<ISessionBroker>())
                {
                    var session = broker.ActiveSession;
                    if (session != null)
                    {
                        return session;
                    }
                }

                return null;
            }
        }
    }

    //-------------------------------------------------------------------------
    // Events.
    //-------------------------------------------------------------------------

    public abstract class SessionBrokerEventBase
    {
        public InstanceLocator Instance { get; }

        protected SessionBrokerEventBase(InstanceLocator vmInstance)
        {
            this.Instance = vmInstance;
        }
    }

    public class SessionStartedEvent : SessionBrokerEventBase
    {
        public SessionStartedEvent(InstanceLocator vmInstance) : base(vmInstance)
        {
        }
    }

    public class SessionAbortedEvent : SessionBrokerEventBase
    {
        public Exception Exception { get; }

        public SessionAbortedEvent(InstanceLocator vmInstance, Exception exception)
            : base(vmInstance)
        {
            this.Exception = exception;
        }
    }

    public class SessionEndedEvent : SessionBrokerEventBase
    {
        public Exception Exception { get; }

        public SessionEndedEvent(InstanceLocator vmInstance) : base(vmInstance)
        {
        }
    }
}
