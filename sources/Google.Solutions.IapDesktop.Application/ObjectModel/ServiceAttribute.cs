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

using System;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
    /// <summary>
    /// Declare that a class should be registered as service.
    /// Only valid in extension DLLs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        public Type ServiceInterface { get; }
        public ServiceLifetime Lifetime { get; }

        public ServiceAttribute(Type serviceInterface, ServiceLifetime lifetime)
        {
            if (serviceInterface != null && !serviceInterface.IsInterface)
            {
                throw new ArgumentException("Type must be an interface type");
            }

            this.ServiceInterface = serviceInterface;
            this.Lifetime = lifetime;
        }

        public ServiceAttribute(Type serviceInterface)
            : this(serviceInterface, ServiceLifetime.Transient)
        {
        }

        public ServiceAttribute(ServiceLifetime lifetime)
            : this(null, lifetime)
        {
        }
        public ServiceAttribute()
            : this(null, ServiceLifetime.Transient)
        {
        }
    }

    public enum ServiceLifetime
    {
        Transient,
        Singleton
    }
}
