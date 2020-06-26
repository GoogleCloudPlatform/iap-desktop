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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Util
{
    public class PropertyMapper<TData>
    {
        public IEnumerable<MappedPropertyAttribute> GetMappings()
        {
            return typeof(TData).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(property => property.GetCustomAttributes(true))
                .OfType<MappedPropertyAttribute>()
                .Where(attribute => attribute.Name != null);
        }

        private PropertyInfo GetPropertyByMapping(MappedPropertyAttribute mapping)
        {
            return typeof(TData).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(property => property
                    .GetCustomAttributes()
                    .OfType<MappedPropertyAttribute>()
                    .Any(attribute => attribute.Name == mapping.Name));
        }

        private object ReadMappedProperty(TData obj, PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentException(nameof(property));
            }
            else
            {
                return property.GetCustomAttributes()
                    .OfType<MappedPropertyAttribute>()
                    .FirstOrDefault()
                    .GetValue(obj, property);
            }
        }

        private void WriteMappedProperty(TData obj, PropertyInfo property, object value)
        {
            Utilities.ThrowIfNull(obj, nameof(obj));
            Utilities.ThrowIfNull(property, nameof(property));

            property.GetCustomAttributes()
                .OfType<MappedPropertyAttribute>()
                .FirstOrDefault()
                .SetValue(obj, property, value);
        }

        public void ReadMappedProperties(
            TData obj,
            Action<MappedPropertyAttribute, object> readFunc)
        {
            Utilities.ThrowIfNull(obj, nameof(obj));
            Utilities.ThrowIfNull(readFunc, nameof(readFunc));

            foreach (var binding in GetMappings())
            {
                var property = GetPropertyByMapping(binding);
                object value = ReadMappedProperty(obj, property);

                readFunc(binding, value);
            }
        }

        public void WriteMappedProperties(
            TData obj,
            Func<MappedPropertyAttribute, object> writeFunc)
        {
            Utilities.ThrowIfNull(obj, nameof(obj));
            Utilities.ThrowIfNull(writeFunc, nameof(writeFunc));

            foreach (var binding in GetMappings())
            {
                var property = GetPropertyByMapping(binding);
                WriteMappedProperty(obj, property, writeFunc(binding));
            }
        }
    }
}