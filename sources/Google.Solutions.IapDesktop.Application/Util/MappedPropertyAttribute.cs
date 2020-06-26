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
using System;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Util
{
    /// <summary>
    /// Defines a data binding between a property and an external value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class MappedPropertyAttribute : Attribute
    {
        public string Name { get; }
        public Type PropertyType { get; }

        protected MappedPropertyAttribute(string name, Type propertyType)
        {
            Utilities.ThrowIfNullOrEmpty(name, nameof(name));

            this.Name = name;
            this.PropertyType = propertyType;
        }

        //---------------------------------------------------------------------
        // Type compatibility checks.
        //---------------------------------------------------------------------

        protected bool IsPropertyOfExpectedType(PropertyInfo property)
        {
            bool propIsNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;

            //
            // Check if the property can be used for the given value kind.
            //
            if (!propIsNullable && property.PropertyType == this.PropertyType)
            {
                // Property matches the expected value kind.
                return true;
            }
            else if (propIsNullable && Nullable.GetUnderlyingType(property.PropertyType) == this.PropertyType)
            {
                // Property is nullable, but the underlying type matches the expected value kind.
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool IsValueOfExpectedType(object value)
        {
            if (value == null)
            {
                return true;
            }

            bool valueIsNullable = Nullable.GetUnderlyingType(value.GetType()) != null;

            //
            // Check if the value can be used for the given value kind.
            //
            if (!valueIsNullable && value.GetType() == this.PropertyType)
            {
                // Value matches the expected value kind.
                return true;
            }
            else if (valueIsNullable && Nullable.GetUnderlyingType(value.GetType()) == this.PropertyType)
            {
                // Value is nullable, but the underlying type matches the expected value kind.
                return true;
            }
            else
            {
                return false;
            }
        }

        //---------------------------------------------------------------------
        // Getters/setters.
        //---------------------------------------------------------------------

        public virtual void SetValue(object obj, PropertyInfo property, object value)
        {
            //
            // Check if the property can be used for the given value kind.
            //
            if (!IsPropertyOfExpectedType(property))
            {
                throw new InvalidCastException(
                    $"Property {property.PropertyType.Name} cannot be bound to a value of type {this.PropertyType}");
            }

            //
            // Check if the value can be used for the given value kind.
            //
            if (!IsValueOfExpectedType(value))
            {
                throw new InvalidCastException(
                    $"Value cannot be bound to a value of type {this.PropertyType}");
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
                    $"Property {property.Name} must be of type {this.PropertyType.Name} (or a nullable thereof) to bind");
            }

            property.SetValue(obj, value);
        }

        public virtual object GetValue(object obj, PropertyInfo property)
        {
            //
            // Check if the property can be used for the given value kind.
            //
            if (!IsPropertyOfExpectedType(property))
            {
                throw new InvalidCastException(
                    $"Property {property.PropertyType.Name} cannot be bound to a value of type {this.PropertyType}");
            }

            return property.GetValue(obj);
        }
    }
}
