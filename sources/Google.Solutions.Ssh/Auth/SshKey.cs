//
// Copyright 2021 Google LLC
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

namespace Google.Solutions.Ssh.Auth
{
    public enum SshKeyType
    {
        Rsa3072,
        EcdsaNistp256,
        EcdsaNistp384,
        EcdsaNistp521
    }

    public static class SshKey // TODO: rename to factory
    {
        public static ISshKey NewEphemeralKey(SshKeyType sshKeyType)
        {
            switch (sshKeyType)
            {
                case SshKeyType.Rsa3072:
                    return RsaSshKey.NewEphemeralKey(3072);

                case SshKeyType.EcdsaNistp256:
                    return ECDsaSshKey.NewEphemeralKey(256);

                case SshKeyType.EcdsaNistp384:
                    return ECDsaSshKey.NewEphemeralKey(384);

                case SshKeyType.EcdsaNistp521:
                    return ECDsaSshKey.NewEphemeralKey(521);

                default:
                    throw new ArgumentException("Unsupported key type");
            }
        }
    }
}
