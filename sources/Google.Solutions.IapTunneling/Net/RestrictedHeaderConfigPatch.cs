//
// Copyright 2019 Google LLC
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
using System.Collections;
using System.Net;
using System.Reflection;

namespace Google.Solutions.IapTunneling.Net
{
    public static class RestrictedHeaderConfigPatch
    {
        /// <summary>
        /// This changes the global configuration, is not thread safe, and 
        /// should be called only once during application initialization.
        /// </summary>
        public static void SetHeaderRestriction(string header, bool restricted)
        {
            // Headers like "User-Agent" are considered a "restricted header" by .NET.
            // While most HTTP clases allow you to specify the header by using a 
            // special property, the ClientWebSocket class does not.
            // 
            // To overcome this limitation, the only option is to patch
            // the libraries internal configuration. As this requires
            // accessing internal and private fields, it might break at any time.
            // 
            // It is therefore important that any code using restricted headers
            // is written so that it gracefully handles failures.

            // The list of restricted headers is stored in a global 
            // HeaderInfoTable object.
            if (typeof(WebHeaderCollection).GetField(
                "HInfo",
                BindingFlags.NonPublic | BindingFlags.Static) is FieldInfo hInfoField)
            {
                var headerInfoTable = hInfoField.GetValue(null);

                // Get the HeaderHashTable field (instance of Hashtable).
                if (headerInfoTable.GetType().GetField(
                    "HeaderHashTable",
                    BindingFlags.NonPublic | BindingFlags.Static) is FieldInfo headersField)
                {
                    if (headersField.GetValue(null) is Hashtable headers)
                    {
                        // Modify the HeaderInfo object stored in the hashtable.
                        var headerInfo = headers[header];
                        if (headerInfo != null &&
                            headerInfo.GetType().GetField(
                                "IsRequestRestricted",
                                BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo restrictedField)
                        {
                            restrictedField.SetValue(headerInfo, restricted);
                            return;
                        }
                    }
                }
            }

            throw new InvalidOperationException("Failed to un-restrict header");
        }
    }
}
