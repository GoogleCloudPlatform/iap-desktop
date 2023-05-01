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


using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Data
{
    internal static class IapRdpUrlExtensions
    {
        public static bool TryGetParameter(
            this IapRdpUrl url,
            string queryParameterName,
            out ushort value)
        {
            value = 0;

            var rawValue = url.Parameters.Get(queryParameterName);
            return !string.IsNullOrWhiteSpace(rawValue) &&
                ushort.TryParse(rawValue, out value);
        }
        public static bool TryGetParameter(
            this IapRdpUrl url,
            string queryParameterName,
            out string value)
        {
            value = url.Parameters.Get(queryParameterName);
            return !string.IsNullOrWhiteSpace(value);
        }

        public static bool TryGetParameter<TEnum>(
            this IapRdpUrl url,
            string queryParameterName,
            out TEnum value)
            where TEnum : struct
        {
            var rawValue = url.Parameters.Get(queryParameterName);
            if (!string.IsNullOrWhiteSpace(rawValue) &&
                Enum.TryParse<TEnum>(rawValue, out var parsedValue) &&
                Enum.IsDefined(typeof(TEnum), parsedValue))
            {
                value = parsedValue;
                return true;
            }
            else
            {
                value = default(TEnum);
                return false;
            }
        }

        public static void ApplyUrlParameterIfSet<TEnum>(
            this RdpSessionParameters context,
            IapRdpUrl url,
            string queryParameterName,
            Action<RdpSessionParameters, TEnum> apply)
            where TEnum : struct
        {
            Precondition.ExpectNotNull(context, nameof(context));
            Precondition.ExpectNotNull(url, nameof(url));
            Precondition.ExpectNotNull(queryParameterName, nameof(queryParameterName));
            Precondition.ExpectNotNull(apply, nameof(apply));

            if (TryGetParameter<TEnum>(url, queryParameterName, out var value))
            {
                apply(context, value);
            }
        }
    }
}
