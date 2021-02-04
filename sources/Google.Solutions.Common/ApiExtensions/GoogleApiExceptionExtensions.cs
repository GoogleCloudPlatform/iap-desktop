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

namespace Google.Solutions.Common.ApiExtensions
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
    }
}
