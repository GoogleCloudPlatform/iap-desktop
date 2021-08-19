﻿//
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

using Google.Solutions.Common.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface ICertificateStoreAdapter
    {
        IEnumerable<X509Certificate2> ListComputerCertitficates();

        IEnumerable<X509Certificate2> ListUserCertitficates();
    }

    public class CertificateStoreAdapter : ICertificateStoreAdapter
    {
        internal void AddUserCertitficate(
            X509Certificate2 certificate)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
            }
        }
        internal void RemoveUserCertitficate(
            X509Certificate2 certificate)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Remove(certificate);
            }
        }

        private IEnumerable<X509Certificate2> ListCertitficates(
            StoreLocation storeLocation)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(storeLocation))
            using (var store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                return store.Certificates
                    .Cast<X509Certificate2>();
            }
        }

        public IEnumerable<X509Certificate2> ListUserCertitficates()
            => ListCertitficates(StoreLocation.CurrentUser);

        public IEnumerable<X509Certificate2> ListComputerCertitficates()
            => ListCertitficates(StoreLocation.LocalMachine);
    }
}
