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
using System.Collections.Generic;

namespace Google.Solutions.Common.Text
{
    public static class TypographicQuotes
    {
        private static readonly Dictionary<char, char> conversionTable = new Dictionary<char, char>()
        {
            { '\u00AB', '\"' }, // left-pointing double angle quotation mark
            { '\u00BB', '\"' }, // right-pointing double angle quotation mark
            { '\u2018', '\'' }, // left single quotation mark
            { '\u2019', '\'' }, // right single quotation mark
            { '\u201A', '\'' }, // single low-9 quotation mark
            { '\u201B', '\'' }, // single high-reversed-9 quotation mark
            { '\u201C', '\"' }, // left double quotation mark
            { '\u201D', '\"' }, // right double quotation mark
            { '\u201E', '\"' }, // double low-9 quotation mark
            { '\u201F', '\"' }, // double high-reversed-9 quotation mark
            { '\u2039', '\'' }, // single left-pointing angle quotation mark
            { '\u203A', '\'' }, // single right-pointing angle quotation mark
            { '\u2E42', '\'' }, // double low-reversed-9 quotation mark
        };

        public static string ToAsciiQuotes(string s)
        {
            Precondition.ExpectNotNull(s, nameof(s));

            var sanitized = new char[s.Length];
            for (var i = 0; i < s.Length; i++)
            {
                if (conversionTable.TryGetValue(s[i], out var replacement))
                {
                    sanitized[i] = replacement;
                }
                else
                {
                    sanitized[i] = s[i];
                }
            }

            return new string(sanitized);
        }
    }
}
