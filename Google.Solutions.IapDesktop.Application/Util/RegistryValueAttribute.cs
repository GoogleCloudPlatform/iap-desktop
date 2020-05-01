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

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.IapDesktop.Application.Util
{
    /// <summary>
    /// Defines a data binding between a property and a registry value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class RegistryValueAttribute : MappedPropertyAttribute
    {
        public abstract RegistryValueKind Kind { get; }

        protected RegistryValueAttribute(string name, Type propertyType)
            : base(name, propertyType)
        {
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a DWORD registry value.
    /// The property must be of type Int32 or Int32?.
    /// </summary>
    public class DwordRegistryValueAttribute : RegistryValueAttribute
    {
        public override RegistryValueKind Kind => RegistryValueKind.DWord;

        public DwordRegistryValueAttribute(string name) : base(name, typeof(Int32))
        {
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a AWORD registry value.
    /// The property must be of type Int64 or Int64?.
    /// </summary>
    public class QwordRegistryValueAttribute : RegistryValueAttribute
    {
        public override RegistryValueKind Kind => RegistryValueKind.QWord;

        public QwordRegistryValueAttribute(string name) : base(name, typeof(Int64))
        {
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a DWORD registry value.
    /// The property must be of type bool or bool?.
    /// </summary>
    public class BoolRegistryValueAttribute : RegistryValueAttribute
    {
        public override RegistryValueKind Kind => RegistryValueKind.DWord;

        public BoolRegistryValueAttribute(string name) : base(name, typeof(bool))
        {
        }

        public override void SetValue(object obj, PropertyInfo property, object value)
        {
            if (value != null)
            {
                base.SetValue(obj, property, ((Int32)value) > 0);
            }
        }

        public override object GetValue(object obj, PropertyInfo property)
        {
            var value = base.GetValue(obj, property);
            return (value != null && ((bool)value)) ? 1 : 0;
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a String registry value.
    /// The property must be of type String.
    /// </summary>
    public class StringRegistryValueAttribute : RegistryValueAttribute
    {
        public override RegistryValueKind Kind => RegistryValueKind.String;

        public StringRegistryValueAttribute(string name) : base(name, typeof(string))
        {
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a DPAPI-encrypted registry value.
    /// The property must be of type SecureString.
    /// </summary>
    public class SecureStringRegistryValueAttribute : RegistryValueAttribute
    {
        private readonly DataProtectionScope scope;

        public override RegistryValueKind Kind => RegistryValueKind.Binary;

        public SecureStringRegistryValueAttribute(string name, DataProtectionScope scope)
            : base(name, typeof(SecureString))
        {
            this.scope = scope;
        }

        public override void SetValue(object obj, PropertyInfo property, object encryptedValue)
        {
            if (encryptedValue == null)
            {
                return;
            }

            Debug.Assert(encryptedValue is byte[]);

            try
            {
                var plaintextString = Encoding.UTF8.GetString(
                    ProtectedData.Unprotect(
                        (byte[])encryptedValue,
                        Encoding.UTF8.GetBytes(property.Name),
                        this.scope));

                base.SetValue(
                    obj,
                    property,
                    SecureStringExtensions.FromClearText(plaintextString));
            }
            catch (CryptographicException)
            {
                // Value cannot be decrypted. This can happen if it was
                // written by a different user or if the current user's
                // key has changed (for example, because its credentials
                // been reset on GCE).
            }
        }

        public override object GetValue(object obj, PropertyInfo property)
        {
            var secureString = (SecureString)base.GetValue(obj, property);
            if (secureString == null)
            {
                return null;
            }

            var plaintextString = secureString.AsClearText();

            return ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plaintextString),
                Encoding.UTF8.GetBytes(property.Name),
                this.scope);
        }
    }
}
