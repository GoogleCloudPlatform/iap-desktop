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

using Google.Solutions.Common.Util;
using System;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// Email address of a service account.
    /// </summary>
    public class ServiceAccountEmail : IEquatable<ServiceAccountEmail>
    {
        public ServiceAccountEmail(string value)
        {
            Precondition.ExpectNotEmpty(value, nameof(value));
            Precondition.Expect(value.Contains("@"), nameof(value));
            this.Value = value;
        }

        /// <summary>
        /// Email address.
        /// </summary>
        public string Value { get; }

        public override string ToString()
        {
            return this.Value;
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        public override bool Equals(object obj)
        {
            return obj is ServiceAccountEmail other && Equals(other);
        }

        public bool Equals(ServiceAccountEmail? other)
        {
            return other != null && Equals(this.Value, other.Value);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public static bool operator ==(ServiceAccountEmail? obj1, ServiceAccountEmail? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(ServiceAccountEmail? obj1, ServiceAccountEmail? obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
