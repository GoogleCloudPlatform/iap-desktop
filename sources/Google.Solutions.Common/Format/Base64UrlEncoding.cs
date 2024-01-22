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

namespace Google.Solutions.Common.Format
{
    /// <summary>
    /// Base64 URL encoding.
    /// </summary>
    public class Base64UrlEncoding
    {
        public static string Encode(byte[] data)
        {
            var base64 = Convert.ToBase64String(data);

            //
            // Strip trailing '='.
            //
            base64 = base64.Split('=')[0];

            return base64
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static byte[] Decode(string encoded)
        {
            var base64 = encoded
                .Replace('-', '+')
                .Replace('_', '/');

            //
            // Add trailing '='.
            //
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}
