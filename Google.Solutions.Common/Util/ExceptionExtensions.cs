//
// Copyright 2020 Google LLC
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

using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Util
{
    public static class ExceptionExtensions
    {
        public static Exception Unwrap(this Exception e)
        {
            if (e is AggregateException aggregate)
            {
                e = aggregate.InnerException;
            }

            if (e is TargetInvocationException target)
            {
                e = target.InnerException;
            }

            return e;
        }

        public static bool Is<T>(this Exception e) where T : Exception
        {
            return e.Unwrap() is T;
        }

        public static bool IsCancellation(this Exception e)
        {
            return e.Is<TaskCanceledException>() || e.Is<OperationCanceledException>();
        }

        public static bool IsReauthError(this Exception e)
        {
            // The TokenResponseException might be hiding in an AggregateException
            e = e.Unwrap();

            if (e is TokenResponseException tokenException)
            {
                return tokenException.Error.Error == "invalid_grant";
            }
            else
            {
                return false;
            }
        }
    }
}
