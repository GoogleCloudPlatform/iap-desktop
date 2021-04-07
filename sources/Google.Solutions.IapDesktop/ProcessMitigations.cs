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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop
{
    internal static class ProcessMitigations
    {
        internal static void Apply()
        {
            var fontPolicy = new UnsafeNativeMethods.PROCESS_MITIGATION_FONT_DISABLE_POLICY()
            {
                DisableNonSystemFonts = true
            };

            if (!UnsafeNativeMethods.SetProcessMitigationPolicy(
                UnsafeNativeMethods.PROCESS_MITIGATION_POLICY.ProcessFontDisablePolicy,
                ref fontPolicy,
                Marshal.SizeOf(fontPolicy)))
            {
                throw new Win32Exception("Setting font process mitigation policy failed");
            }

            var imagePolicy = new UnsafeNativeMethods.PROCESS_MITIGATION_IMAGE_LOAD_POLICY()
            {
                NoLowMandatoryLabelImages = true,
                NoRemoteImages = true
            };

            if (!UnsafeNativeMethods.SetProcessMitigationPolicy(
                UnsafeNativeMethods.PROCESS_MITIGATION_POLICY.ProcessImageLoadPolicy,
                ref imagePolicy,
                Marshal.SizeOf(imagePolicy)))
            {
                throw new Win32Exception("Setting image-load process mitigation policy failed");
            }
        }
    }
}
