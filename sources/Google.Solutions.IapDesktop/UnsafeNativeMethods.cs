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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop
{
    internal class UnsafeNativeMethods
    {

        internal enum PROCESS_MITIGATION_POLICY
        {
            ProcessDEPPolicy = 0,
            ProcessASLRPolicy = 1,
            ProcessDynamicCodePolicy = 2,
            ProcessStrictHandleCheckPolicy = 3,
            ProcessSystemCallDisablePolicy = 4,
            ProcessMitigationOptionsMask = 5,
            ProcessExtensionPointDisablePolicy = 6,
            ProcessControlFlowGuardPolicy = 7,
            ProcessSignaturePolicy = 8,
            ProcessFontDisablePolicy = 9,
            ProcessImageLoadPolicy = 10,
            MaxProcessMitigationPolicy = 11
        }

        internal struct PROCESS_MITIGATION_FONT_DISABLE_POLICY
        {
            public uint Flags;

            public bool DisableNonSystemFonts
            {
                get => (this.Flags & 0x1u) != 0;
                set
                {
                    if (value)
                    {
                        this.Flags |= 0x1u;
                    }
                    else
                    {
                        this.Flags &= ~0x1u;
                    }
                }
            }
        }

        internal struct PROCESS_MITIGATION_IMAGE_LOAD_POLICY
        {
            public uint Flags;

            public bool NoRemoteImages
            {
                get => (this.Flags & 0x1u) != 0;
                set
                {
                    if (value)
                    {
                        this.Flags |= 0x1u;
                    }
                    else
                    {
                        this.Flags &= ~0x1u;
                    }
                }
            }

            public bool NoLowMandatoryLabelImages
            {
                get => (this.Flags & 0x2u) != 0;
                set
                {
                    if (value)
                    {
                        this.Flags |= 0x2u;
                    }
                    else
                    {
                        this.Flags &= ~0x2u;
                    }
                }
            }

            public bool PreferSystem32Images
            {
                get => (this.Flags & 0x4u) != 0;
                set
                {
                    if (value)
                    {
                        this.Flags |= 0x4u;
                    }
                    else
                    {
                        this.Flags &= ~0x4u;
                    }
                }
            }

        }

        [DllImport("kernel32.dll")]
        internal static extern bool SetProcessMitigationPolicy(
            PROCESS_MITIGATION_POLICY mitigationPolicy,
            ref PROCESS_MITIGATION_FONT_DISABLE_POLICY lpBuffer,
            int dwLength);

        [DllImport("kernel32.dll")]
        internal static extern bool SetProcessMitigationPolicy(
            PROCESS_MITIGATION_POLICY mitigationPolicy,
            ref PROCESS_MITIGATION_IMAGE_LOAD_POLICY lpBuffer,
            int dwLength);
    }
}
