//
// Copyright 2024 Google LLC
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
using System.Collections.Generic;
using System.Security;

namespace Google.Solutions.Settings
{
    /// <summary>
    /// Accessor for a dictionary value that automatically performs
    /// the necessary type conversions.
    /// </summary>
    internal abstract class DictionaryValueAccessor<T>
        : IValueAccessor<IDictionary<string, string>, T>
    {
        internal string Name { get; }

        protected DictionaryValueAccessor(string name)
        {
            this.Name = name.ExpectNotNull(nameof(name));
        }

        public abstract bool TryRead(IDictionary<string, string> dictionary, out T value);

        public virtual void Write(IDictionary<string, string> dictionary, T value)
        {
            dictionary.ExpectNotNull(nameof(dictionary))[this.Name] = value?.ToString();
        }

        public void Delete(IDictionary<string, string> dictionary)
        {
            dictionary
                .ExpectNotNull(nameof(dictionary))
                .Remove(this.Name);
        }

        public virtual bool IsValid(T value)
        {
            return true;
        }
    }

    internal static class DictionaryValueAccessor
    {
        /// <summary>
        /// Create an accessor that's specialized for the given type.
        /// </summary>
        public static DictionaryValueAccessor<T> Create<T>(string name)
        {
            if (typeof(T) == typeof(bool))
            {
                return (DictionaryValueAccessor<T>)(object)new BoolValueAccessor(name);
            }
            else if (typeof(T) == typeof(int))
            {
                return (DictionaryValueAccessor<T>)(object)new IntValueAccessor(name);
            }
            else if (typeof(T) == typeof(long))
            {
                return (DictionaryValueAccessor<T>)(object)new LongValueAccessor(name);
            }
            else if (typeof(T) == typeof(string))
            {
                return (DictionaryValueAccessor<T>)(object)new StringValueAccessor(name);
            }
            else if (typeof(T) == typeof(SecureString))
            {
                return (DictionaryValueAccessor<T>)(object)new SecureStringValueAccessor(name);
            }
            else if (typeof(T).IsEnum)
            {
                return (DictionaryValueAccessor<T>)(object)new EnumValueAccessor<T>(name);
            }
            else
            {
                throw new ArgumentException(
                    $"Dictionary value cannot be mapped to {typeof(T).Name}");
            }
        }

        private class StringValueAccessor : DictionaryValueAccessor<string>
        {
            public StringValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(IDictionary<string, string> dictionary, out string value)
            {
                return dictionary
                    .ExpectNotNull(nameof(dictionary))
                    .TryGetValue(this.Name, out value);
            }
        }

        private class SecureStringValueAccessor : DictionaryValueAccessor<SecureString>
        {
            public SecureStringValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(IDictionary<string, string> dictionary, out SecureString value)
            {
                //
                // Refuse to read.
                //
                value = null;
                return false;
            }

            public override void Write(IDictionary<string, string> dictionary, SecureString value)
            {
                //
                // Refuse to write.
                //
            }
        }

        private class BoolValueAccessor : DictionaryValueAccessor<bool>
        {
            public BoolValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(IDictionary<string, string> dictionary, out bool value)
            {
                value = default;
                return dictionary
                    .ExpectNotNull(nameof(dictionary))
                    .TryGetValue(this.Name, out var stringValue) &&
                    bool.TryParse(stringValue, out value);
            }
        }

        private class IntValueAccessor : DictionaryValueAccessor<int>
        {
            public IntValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(IDictionary<string, string> dictionary, out int value)
            {
                value = default;
                return dictionary
                    .ExpectNotNull(nameof(dictionary))
                    .TryGetValue(this.Name, out var stringValue) &&
                    int.TryParse(stringValue, out value);
            }
        }

        private class LongValueAccessor : DictionaryValueAccessor<long>
        {
            public LongValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(IDictionary<string, string> dictionary, out long value)
            {
                value = default;
                return dictionary
                    .ExpectNotNull(nameof(dictionary))
                    .TryGetValue(this.Name, out var stringValue) &&
                    long.TryParse(stringValue, out value);
            }
        }

        private class EnumValueAccessor<TEnum> : DictionaryValueAccessor<TEnum>
        {
            public EnumValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(IDictionary<string, string> dictionary, out TEnum value)
            {
                if (dictionary
                    .ExpectNotNull(nameof(dictionary))
                    .TryGetValue(this.Name, out var stringValue) &&
                    int.TryParse(stringValue, out var intValue))
                {
                    value = (TEnum)(object)intValue;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            public override void Write(IDictionary<string, string> dictionary, TEnum value)
            {
                dictionary.ExpectNotNull(nameof(dictionary))[this.Name]
                    = ((int)(object)value).ToString();
            }

            public override bool IsValid(TEnum value)
            {
                return value.IsValidFlagCombination();
            }
        }
    }
}