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
using System.Linq;
using System.Reflection;

namespace Google.Solutions.Common.Util
{
    public static class EnumExtensions
    {
        private static bool IsPowerOfTwo(int v)
        {
            return v != 0 && (v & (v - 1)) == 0;
        }

        /// <summary>
        /// Check if the value has only a single flag set.
        /// </summary>
        public static bool IsSingleFlag<TEnum>(this TEnum enumValue)
            where TEnum : struct
        {
            return IsPowerOfTwo((int)(object)enumValue);
        }

        /// <summary>
        /// Check if the value has more than one flag set.
        /// </summary>
        public static bool IsFlagCombination<TEnum>(this TEnum enumValue)
            where TEnum : struct
        {
            var v = (int)(object)enumValue;
            return v != 0 && !IsPowerOfTwo(v);
        }

        public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue)
                where TAttribute : Attribute
        {
            return enumValue
                .GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault()?
                .GetCustomAttribute<TAttribute>();
        }

        /// <summary>
        /// Check all flags set represent valid enum values.
        /// </summary>
        public static bool IsDefinedFlagCombination<TEnum>(this TEnum enumValue) // TODO: test
        {
            var numericValue = Convert.ToInt64(enumValue);

            //
            // Create a bit field with all flags on.
            //
            var max = Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                .Select(v => Convert.ToInt64(v))
                .Aggregate((e1, e2) => e1 | e2);

            return (max & numericValue) == numericValue;
        }
    }
}
