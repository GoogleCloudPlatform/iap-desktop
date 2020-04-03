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

using Google.Apis.Util;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.IapDesktop.Application.Registry
{
    /// <summary>
    /// Defines a data binding between a property and a registry value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class RegistryValueAttribute : Attribute
    {
        public string Name { get; }
        public RegistryValueKind Kind { get; }

        protected RegistryValueAttribute(string name, RegistryValueKind kind)
        {
            Utilities.ThrowIfNullOrEmpty(name, nameof(name));

            this.Name = name;
            this.Kind = kind;
        }

        protected bool IsPropertyCompatibleWithValueKind<TValueKind>(PropertyInfo property)
        {
            bool propIsNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;

            //
            // Check if the property can be used for the given value kind.
            //
            if (!propIsNullable && property.PropertyType == typeof(TValueKind))
            {
                // Property matches the expected value kind.
                return true;
            }
            else if (propIsNullable && Nullable.GetUnderlyingType(property.PropertyType) == typeof(TValueKind))
            {
                // Property is nullable, but the underlying type matches the expected value kind.
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool IsValueCompatibleWithValueKind<TValueKind>(object value)
        {
            if (value == null)
            {
                return true;
            }

            bool valueIsNullable = Nullable.GetUnderlyingType(value.GetType()) != null;

            //
            // Check if the value can be used for the given value kind.
            //
            if (!valueIsNullable && value.GetType() == typeof(TValueKind))
            {
                // Value matches the expected value kind.
                return true;
            }
            else if (valueIsNullable && Nullable.GetUnderlyingType(value.GetType()) == typeof(TValueKind))
            {
                // Value is nullable, but the underlying type matches the expected value kind.
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetValue<TValueKind>(object obj, PropertyInfo property, object value)
        {
            //
            // Check if the property can be used for the given value kind.
            //
            if (!IsPropertyCompatibleWithValueKind<TValueKind>(property))
            {
                throw new InvalidCastException(
                    $"Property {property.PropertyType.Name} cannot be bound to a registry value of kind {this.Kind}");
            }

            //
            // Check if the value can be used for the given value kind.
            //
            if (!IsValueCompatibleWithValueKind<TValueKind>(value))
            {
                throw new InvalidCastException(
                    $"Value cannot be bound to a registry value of kind {this.Kind}");
            }

            if (value == null)
            {
                // Nothing to do.
                return;
            }

            bool propIsNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
            bool valueIsNullable = Nullable.GetUnderlyingType(value.GetType()) != null;

            //
            // Convert value to Nullable if necessary.
            //
            if (propIsNullable == valueIsNullable)
            {
                // Straight assignment should work.

            }
            else if (propIsNullable && !valueIsNullable)
            {
                // Value needs to be wrapped.
                value = Activator.CreateInstance(
                    typeof(Nullable<>).MakeGenericType(value.GetType()),
                    value);
            }
            else
            {
                throw new InvalidCastException(
                    $"Value of type {property.PropertyType.Name} cannot be assigned to {value.GetType().Name}");
            }

            if (!property.PropertyType.IsAssignableFrom(value.GetType()))
            {
                throw new ArgumentException(
                    $"Property {property.Name} must be of type {typeof(TValueKind).Name} (or a nullable thereof) to bind");
            }

            property.SetValue(obj, value);
        }

        public abstract void SetValue(object obj, PropertyInfo property, object value);

        public object GetValue<TValueKind>(object obj, PropertyInfo property)
        {
            //
            // Check if the property can be used for the given value kind.
            //
            if (!IsPropertyCompatibleWithValueKind<TValueKind>(property))
            {
                throw new InvalidCastException(
                    $"Property {property.PropertyType.Name} cannot be bound to a registry value of kind {this.Kind}");
            }

            return property.GetValue(obj);
        }

        public abstract object GetValue(object obj, PropertyInfo property);
    }

    /// <summary>
    /// Defines a data binding between a property and a DWORD registry value.
    /// The property must be of type Int32 or Int32?.
    /// </summary>
    public class DwordRegistryValueAttribute : RegistryValueAttribute
    {
        public DwordRegistryValueAttribute(string name) : base(name, RegistryValueKind.DWord)
        {
        }

        public override void SetValue(object obj, PropertyInfo property, object value)
        {
            base.SetValue<Int32>(obj, property, value);
        }

        public override object GetValue(object obj, PropertyInfo property)
        {
            return base.GetValue<Int32>(obj, property);
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a AWORD registry value.
    /// The property must be of type Int64 or Int64?.
    /// </summary>
    public class QwordRegistryValueAttribute : RegistryValueAttribute
    {
        public QwordRegistryValueAttribute(string name) : base(name, RegistryValueKind.QWord)
        {
        }

        public override void SetValue(object obj, PropertyInfo property, object value)
        {
            base.SetValue<Int64>(obj, property, value);
        }

        public override object GetValue(object obj, PropertyInfo property)
        {
            return base.GetValue<Int64>(obj, property);
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a DWORD registry value.
    /// The property must be of type bool or bool?.
    /// </summary>
    public class BoolRegistryValueAttribute : RegistryValueAttribute
    {
        public BoolRegistryValueAttribute(string name) : base(name, RegistryValueKind.DWord)
        {
        }

        public override void SetValue(object obj, PropertyInfo property, object value)
        {
            if (value != null)
            {
                base.SetValue<bool>(obj, property, ((Int32)value) > 0);
            }
        }

        public override object GetValue(object obj, PropertyInfo property)
        {
            var value = base.GetValue<bool>(obj, property);
            return (value != null && ((bool)value)) ? 1 : 0;
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a String registry value.
    /// The property must be of type String.
    /// </summary>
    public class StringRegistryValueAttribute : RegistryValueAttribute
    {
        public StringRegistryValueAttribute(string name) : base(name, RegistryValueKind.String)
        {
        }

        public override void SetValue(object obj, PropertyInfo property, object value)
        {
            base.SetValue<string>(obj, property, value);
        }

        public override object GetValue(object obj, PropertyInfo property)
        {
            return base.GetValue<string>(obj, property);
        }
    }

    /// <summary>
    /// Defines a data binding between a property and a DPAPI-encrypted registry value.
    /// The property must be of type SecureString.
    /// </summary>
    public class SecureStringRegistryValueAttribute : RegistryValueAttribute
    {
        private readonly DataProtectionScope scope;
        public SecureStringRegistryValueAttribute(string name, DataProtectionScope scope)
            : base(name, RegistryValueKind.Binary)
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

                base.SetValue<SecureString>(
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
            var secureString = (SecureString)base.GetValue<SecureString>(obj, property);
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
