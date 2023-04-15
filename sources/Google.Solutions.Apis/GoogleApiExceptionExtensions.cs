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
using Google.Solutions.Common.Util;
using System;

namespace Google.Solutions.Apis
{
    public static class GoogleApiExceptionExtensions
    {
        public static bool IsConstraintViolation(this GoogleApiException e)
            => e.Error != null && e.Error.Code == 412;

        public static bool IsAccessDenied(this GoogleApiException e)
            => e.Error != null && e.Error.Code == 403;

        public static bool IsNotFound(this GoogleApiException e)
            => e.Error != null && e.Error.Code == 404;

        public static bool IsBadRequest(this GoogleApiException e)
            => e.Error != null && e.Error.Code == 400 && e.Error.Message == "BAD REQUEST";

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

        public static bool IsAccessDeniedError(this Exception e)
        {
            return e.Unwrap() is GoogleApiException apiEx && apiEx.Error.Code == 403;
        }
    }
}
