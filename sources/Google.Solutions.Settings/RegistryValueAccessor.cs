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

using Google.Solutions.Common.Security;
using Google.Solutions.Common.Util;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.Settings
{
    /// <summary>
    /// Accessor for a registry value that automatically performs
    /// the necessary type conversions.
    /// </summary>
    internal abstract class RegistryValueAccessor<T> : IValueAccessor<RegistryKey, T>
    {
        internal string Name { get; }

        protected RegistryValueAccessor(string name)
        {
            this.Name = name.ExpectNotNull(nameof(name));
        }

        public abstract bool TryRead(RegistryKey key, out T value);

        public abstract void Write(RegistryKey key, T value);

        public void Delete(RegistryKey key)
        {
            key
                .ExpectNotNull(nameof(key))
                .DeleteValue(this.Name, false);
        }

        protected bool TryReadRaw<TRawValue>(
            RegistryKey key,
            out TRawValue value)
        {
            var data = key
                .ExpectNotNull(nameof(key))
                .GetValue(this.Name);
            if (data == null)
            {
                value = default;
                return false;
            }
            else if (data is TRawValue typedData)
            {
                value = typedData;
                return true;
            }
            else
            {
                throw new InvalidCastException(
                    $"The registry data is of type {data.GetType().Name}, " +
                    $"which cannot be converted to {typeof(T).Name}");
            }
        }

        protected void WriteRaw(
            RegistryKey key,
            object value,
            RegistryValueKind kind)
        {
            key
                .ExpectNotNull(nameof(key))
                .SetValue(
                    this.Name,
                    value,
                    kind);
        }

        public virtual bool IsValid(T value)
        {
            return true;
        }
    }

    internal static class RegistryValueAccessor
    {
        /// <summary>
        /// Create an accessor that's specialized for the given type.
        /// </summary>
        public static RegistryValueAccessor<T> Create<T>(string name)
        {
            if (typeof(T) == typeof(bool))
            {
                return (RegistryValueAccessor<T>)(object)new BoolValueAccessor(name);
            }
            else if (typeof(T) == typeof(int))
            {
                return (RegistryValueAccessor<T>)(object)new DwordValueAccessor(name);
            }
            else if (typeof(T) == typeof(long))
            {
                return (RegistryValueAccessor<T>)(object)new QwordValueAccessor(name);
            }
            else if (typeof(T) == typeof(string))
            {
                return (RegistryValueAccessor<T>)(object)new StringValueAccessor(name);
            }
            else if (typeof(T) == typeof(SecureString))
            {
                return (RegistryValueAccessor<T>)(object)new SecureStringValueAccessor(
                    name,
                    DataProtectionScope.CurrentUser);
            }
            else if (typeof(T).IsEnum)
            {
                return (RegistryValueAccessor<T>)(object)new EnumValueAccessor<T>(name);
            }
            else
            {
                throw new ArgumentException(
                    $"Registry value cannot be mapped to {typeof(T).Name}");
            }
        }

        private class StringValueAccessor : RegistryValueAccessor<string>
        {
            public StringValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(RegistryKey key, out string value)
            {
                return TryReadRaw<string>(key, out value);
            }

            public override void Write(RegistryKey key, string value)
            {
                WriteRaw(
                    key,
                    value,
                    RegistryValueKind.String);
            }
        }

        private class SecureStringValueAccessor : RegistryValueAccessor<SecureString>
        {
            private readonly DataProtectionScope protectionScope;

            public SecureStringValueAccessor(
                string name,
                DataProtectionScope protectionScope) : base(name)
            {
                this.protectionScope = protectionScope;
            }

            public override bool TryRead(RegistryKey key, out SecureString value)
            {
                if (TryReadRaw<byte[]>(key, out var blob))
                {
                    try
                    {
                        var plaintextString = Encoding.UTF8.GetString(
                            ProtectedData.Unprotect(
                                blob,
                                Encoding.UTF8.GetBytes(this.Name), // Entropy
                                this.protectionScope));

                        value = SecureStringExtensions.FromClearText(plaintextString);
                        return true;
                    }
                    catch (CryptographicException)
                    {
                        //
                        // Value cannot be decrypted. This can happen if it was
                        // written by a different user or if the current user's
                        // key has changed (for example, because its credentials
                        // been reset on GCE).
                        //
                        value = null;
                        return false;
                    }
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            public override void Write(RegistryKey key, SecureString value)
            {
                byte[] blob = null;
                if (value != null)
                {
                    blob = ProtectedData.Protect(
                        Encoding.UTF8.GetBytes(value.ToClearText()),
                        Encoding.UTF8.GetBytes(this.Name), // Entropy
                        this.protectionScope);
                }

                WriteRaw(key, blob, RegistryValueKind.Binary);
            }
        }

        private class BoolValueAccessor : RegistryValueAccessor<bool>
        {
            public BoolValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(RegistryKey key, out bool value)
            {
                if (TryReadRaw<int>(key, out var intValue))
                {
                    value = intValue != 0;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            public override void Write(RegistryKey key, bool value)
            {
                WriteRaw(
                    key,
                    value ? 1 : 0,
                    RegistryValueKind.DWord);
            }
        }

        private class DwordValueAccessor : RegistryValueAccessor<int>
        {
            public DwordValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(RegistryKey key, out int value)
            {
                return TryReadRaw<int>(key, out value);
            }

            public override void Write(RegistryKey key, int value)
            {
                WriteRaw(
                    key,
                    value,
                    RegistryValueKind.DWord);
            }
        }

        private class QwordValueAccessor : RegistryValueAccessor<long>
        {
            public QwordValueAccessor(string name) : base(name)
            {
            }

            public override bool TryRead(RegistryKey key, out long value)
            {
                return TryReadRaw<long>(key, out value);
            }

            public override void Write(RegistryKey key, long value)
            {
                WriteRaw(
                    key,
                    value,
                    RegistryValueKind.QWord);
            }
        }

        private class EnumValueAccessor<TEnum> : RegistryValueAccessor<TEnum>
        {
            public EnumValueAccessor(string name) : base(name)
            {
                Debug.Assert(typeof(TEnum).IsEnum);
            }

            public override bool TryRead(RegistryKey key, out TEnum value)
            {
                if (TryReadRaw<int>(key, out var intValue))
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

            public override void Write(RegistryKey key, TEnum value)
            {
                WriteRaw(
                    key,
                    value,
                    RegistryValueKind.DWord);
            }

            public override bool IsValid(TEnum value)
            {
                return value.IsValidFlagCombination();
            }
        }
    }
}