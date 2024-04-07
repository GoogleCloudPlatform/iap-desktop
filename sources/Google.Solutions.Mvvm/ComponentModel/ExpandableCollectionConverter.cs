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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Google.Solutions.Mvvm.ComponentModel
{
    /// <summary>
    /// Allows collecion-typed properties to be expanded for viewing. 
    /// Similar to ExpandableObjectConverter, but read-only.
    /// </summary>
    public class ExpandableCollectionConverter : TypeConverter
    {
        public override PropertyDescriptorCollection GetProperties(
            ITypeDescriptorContext? context,
            object value,
            Attribute[] attributes)
        {
            if (value is IDictionary dictionary)
            {
                var properties = new List<ItemDescriptor<IDictionary>>();
                foreach (DictionaryEntry item in dictionary)
                {
                    properties.Add(new ItemDescriptor<IDictionary>(
                        item.Key.ToString(), 
                        item.Value));
                }

                return new PropertyDescriptorCollection(properties.ToArray());
            }
            else if (value is ICollection collection)
            {
                var properties = new List<ItemDescriptor<IDictionary>>();
                foreach (var item in collection)
                {
                    properties.Add(new ItemDescriptor<IDictionary>(item));
                }

                return new PropertyDescriptorCollection(properties.ToArray());
            }
            else
            {
                return base.GetProperties(context, value, attributes);
            }
        }

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            if (destinationType == typeof(string) && value is ICollection collection)
            {
                return $"{collection.Count} items";
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private class ItemDescriptor<TCollection> : PropertyDescriptor
        {
            private const string EmptyName = " ";
            private readonly object value;

            internal ItemDescriptor(string name, object value)
                : base(name, null)
            {
                this.value = value.ExpectNotNull(nameof(value));
            }

            internal ItemDescriptor(object value)
                : this(EmptyName, value)
            { }

            public override Type PropertyType
            {
                get => this.value.GetType();
            }

            public override void SetValue(object component, object value)
            {
            }

            public override object GetValue(object component)
            {
                return this.value;
            }

            public override bool IsReadOnly
            {
                get => true;
            }

            public override Type ComponentType
            {
                get => typeof(TCollection);
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override void ResetValue(object component)
            {
            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }
    }
}