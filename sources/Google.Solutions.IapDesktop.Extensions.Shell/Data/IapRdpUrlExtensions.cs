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
using System;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Data
{
    internal static class IapRdpUrlExtensions
    {
        public static void ApplyUrlParameterIfSet<TEnum>(
            this RdpSessionParameters parameters,
            IapRdpUrl url,
            string queryParameterName,
            Action<RdpSessionParameters, TEnum> apply)
            where TEnum : struct
        {
            Precondition.ExpectNotNull(parameters, nameof(parameters));
            Precondition.ExpectNotNull(url, nameof(url));
            Precondition.ExpectNotNull(queryParameterName, nameof(queryParameterName));
            Precondition.ExpectNotNull(apply, nameof(apply));

            var value = url.Parameters.Get(queryParameterName);
            if (!string.IsNullOrWhiteSpace(value) &&
                Enum.TryParse<TEnum>(value, out var parsedValue) &&
                Enum.IsDefined(typeof(TEnum), parsedValue))
            {
                apply(parameters, parsedValue);
            }
        }
    }
}
