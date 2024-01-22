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


using System;
using System.Linq;
using System.Text;

namespace Google.Solutions.Common.Util
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Return the full type name in angle-bracket notation.
        /// </summary>
        public static string FullName(this Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var name = new StringBuilder();
            name.Append(type.Name.Substring(0, type.Name.IndexOf('`')));
            name.Append('<');

            name.Append(string.Join(
                ",",
                type.GetGenericArguments().Select(t => t.FullName())));

            name.Append('>');
            return name.ToString();
        }
    }
}
