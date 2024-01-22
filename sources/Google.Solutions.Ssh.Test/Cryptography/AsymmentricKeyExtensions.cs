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
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    internal static class AsymmentricKeyExtensions
    {
        internal static bool IsDisposed(this ECDsa key)
        {
            try
            {
                key.SignData(
                    new byte[] { 1, 2, 3 },
                    HashAlgorithmName.SHA256);
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }
        internal static bool IsDisposed(this RSA key)
        {
            try
            {
                key.SignData(
                    new byte[] { 1, 2, 3 },
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }
    }
}
