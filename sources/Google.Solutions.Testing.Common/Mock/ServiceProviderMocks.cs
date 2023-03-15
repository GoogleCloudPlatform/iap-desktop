﻿//
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

using Moq;
using System;

namespace Google.Solutions.Testing.Common.Mocks
{
    public static class ServiceProviderMocks
    {
        public static void Add<TService>(
            this Mock<IServiceProvider> serviceProvider,
            TService service)
            where TService : class
        {
            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(TService))))
                .Returns(service);
        }

        public static Mock<TService> AddMock<TService>(
            this Mock<IServiceProvider> serviceProvider)
            where TService : class
        {
            var service = new Mock<TService>();
            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(TService))))
                .Returns(service.Object);
            return service;
        }
    }
}
