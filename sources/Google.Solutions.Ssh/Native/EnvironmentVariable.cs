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

namespace Google.Solutions.Ssh.Native
{
    internal struct EnvironmentVariable : IEquatable<EnvironmentVariable>
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Required { get; set; }

        internal EnvironmentVariable(string name, string value, bool required)
        {
            Precondition.ExpectNotEmpty(name, nameof(name));
            Precondition.ExpectNotNull(value, nameof(value));

            this.Name = name;
            this.Value = value;
            this.Required = required;
        }

        public readonly override bool Equals(object obj)
        {
            return obj is EnvironmentVariable var && Equals(var);
        }

        public readonly bool Equals(EnvironmentVariable var)
        {
            return
                var.Name == this.Name &&
                var.Value == this.Value &&
                var.Required == this.Required;
        }

        public readonly override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public static bool operator ==(EnvironmentVariable left, EnvironmentVariable right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EnvironmentVariable left, EnvironmentVariable right)
        {
            return !(left == right);
        }
    }
}
