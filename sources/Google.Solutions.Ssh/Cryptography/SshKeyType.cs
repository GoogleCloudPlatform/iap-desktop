﻿//
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

using Google.Solutions.Common.Diagnostics;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    public enum SshKeyType : int //TODO: Move to session
    {
        //
        // NB. These values are used for persistence and
        // must be kept constant.
        //

        [Display(Name = "RSA (3072 bit)")]
        Rsa3072 = 0x01,

        [Display(Name = "ECDSA NIST P-256")]
        EcdsaNistp256 = 0x11,

        [Display(Name = "ECDSA NIST P-384")]
        EcdsaNistp384 = 0x12,

        [Display(Name = "ECDSA NIST P-521")]
        EcdsaNistp521 = 0x13
    }
}
