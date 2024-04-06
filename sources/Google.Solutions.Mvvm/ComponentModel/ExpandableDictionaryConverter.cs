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
using System.Linq;

namespace Google.Solutions.Mvvm.ComponentModel
{
    /// <summary>
    /// Allows dictionaries to be expanded, similar to 
    /// ExpandableObjectConverter.
    /// </summary>
    public class ExpandableDictionaryConverter : TypeConverter
    {
        public override PropertyDescriptorCollection GetProperties(
            ITypeDescriptorContext? context,
            object value,
            Attribute[] attributes)
        {
            if (value is IDictionary dictionary)
            {
                var readOnly = context == null ||
                    context.PropertyDescriptor
                        .Attributes
                        .OfType<ReadOnlyAttribute>()
                        .Where(a => a.IsReadOnly)
                        .Any();

                var properties = new List<ItemDescriptor>();
                foreach (DictionaryEntry item in dictionary)
                {
                    properties.Add(new ItemDescriptor(dictionary, item.Key, readOnly));
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
            if (destinationType == typeof(string) && value is IDictionary dictionary)
            {
                return $"{dictionary.Count} items";
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

        private class ItemDescriptor : PropertyDescriptor
        {
            private readonly bool readOnly;
            private readonly IDictionary dictionary;
            private readonly object key;

            internal ItemDescriptor(IDictionary dictionary, object key, bool readOnly)
                : base(key?.ToString(), null)
            {
                this.dictionary = dictionary.ExpectNotNull(nameof(dictionary));
                this.key = key.ExpectNotNull(nameof(key));
                this.readOnly = readOnly;
            }

            public override Type PropertyType
            {
                get => this.dictionary[this.key].GetType();
            }

            public override void SetValue(object component, object value)
            {
                this.dictionary[this.key] = value;
            }

            public override object GetValue(object component)
            {
                return this.dictionary[this.key];
            }

            public override bool IsReadOnly
            {
                get => this.readOnly;
            }

            public override Type ComponentType
            {
                get => typeof(IDictionary);
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