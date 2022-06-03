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

using System;

namespace Google.Solutions.IapDesktop.Application.Util
{
    public static class StringExtensions
    {
        public static int IndexOf(this string str, Predicate<char> predicate)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (predicate(str[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LastIndexOf(this string str, Predicate<char> predicate)
        {
            for (int i = str.Length - 1; i >= 0; i--)
            {
                if (predicate(str[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars
                ? value
                : value.Substring(0, maxChars) + "...";
        }

        public static string NullIfEmpty(this string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }

        public static string NullIfEmptyOrWhitespace(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
    }
}
