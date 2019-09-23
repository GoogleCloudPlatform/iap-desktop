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

using Google.Apis.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    internal static class PageHelper
    {
        internal static async Task<IEnumerable<TValue>> JoinPagesAsync<TRequest, TResponse, TValue>(
            TRequest request,
            Func<TResponse, IEnumerable<TValue>> mapFunc,
            Func<TResponse, string> getNextPageTokenFunc,
            Action<TRequest, string> setPageTokenFunc)
            where TRequest : IClientServiceRequest<TResponse>
        {
            TResponse response;
            var allValues = new List<TValue>();
            do
            {
                response = await request.ExecuteAsync();

                IEnumerable<TValue> pageValues = mapFunc(response);
                if (pageValues != null)
                {
                    allValues.AddRange(pageValues);
                }

                setPageTokenFunc(request, getNextPageTokenFunc(response));
            }
            while (getNextPageTokenFunc(response) != null);

            return allValues;
        }
    }
}
