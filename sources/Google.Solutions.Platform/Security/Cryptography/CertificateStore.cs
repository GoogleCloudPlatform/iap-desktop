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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Platform.Security.Cryptography
{
    /// <summary>
    /// Windows certificate store.
    /// </summary>
    public interface ICertificateStore
    {
        /// <summary>
        /// List certificates from the machine certificate store.
        /// </summary>
        IEnumerable<X509Certificate2> ListMachineCertificates(
            Predicate<X509Certificate2> filter);

        /// <summary>
        /// List certificates from the user's certificate store.
        /// </summary>
        IEnumerable<X509Certificate2> ListUserCertificates(
            Predicate<X509Certificate2> filter);
    }

    public class CertificateStore : ICertificateStore
    {
        internal static void AddUserCertitficate(X509Certificate2 certificate)
        {
            using (PlatformTraceSource.Log.TraceMethod().WithoutParameters())
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
            }
        }

        internal static void RemoveUserCertitficate(X509Certificate2 certificate)
        {
            using (PlatformTraceSource.Log.TraceMethod().WithoutParameters())
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Remove(certificate);
            }
        }

        private static IEnumerable<X509Certificate2> ListCertitficates(
            StoreLocation storeLocation,
            Predicate<X509Certificate2> filter)
        {
            filter.ExpectNotNull(nameof(filter));

            using (PlatformTraceSource.Log.TraceMethod().WithParameters(storeLocation))
            using (var store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                foreach (var certificate in store.Certificates.Cast<X509Certificate2>())
                {
                    if (filter(certificate))
                    {
                        yield return certificate;
                    }
                    else
                    {
                        certificate.Dispose();
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // ICertificateStore.
        //---------------------------------------------------------------------

        public IEnumerable<X509Certificate2> ListUserCertificates(
            Predicate<X509Certificate2> filter)
        {
            return ListCertitficates(StoreLocation.CurrentUser, filter);
        }

        public IEnumerable<X509Certificate2> ListMachineCertificates(
            Predicate<X509Certificate2> filter)
        {
            return ListCertitficates(StoreLocation.LocalMachine, filter);
        }
    }
}
