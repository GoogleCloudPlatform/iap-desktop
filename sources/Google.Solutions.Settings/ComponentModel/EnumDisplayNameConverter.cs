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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Google.Solutions.Settings.ComponentModel
{
    /// <summary>
    /// Converts between enum member names and strings based on
    /// DisplayName attributes.
    /// </summary>
    public class EnumDisplayNameConverter : EnumConverter
    {
        public EnumDisplayNameConverter(Type type) : base(type)
        {
            Precondition.Expect(type.IsEnum, "Type must be an enum");
            Precondition.Expect(type.GetFields().Any(), "Enum must have at least one member");

            Precondition.Expect(
                !type
                    .GetFields()
                    .SelectMany(m => m.GetCustomAttributes<DescriptionAttribute>())
                    .Any() ||
                type
                    .GetFields()
                    .SelectMany(m => m.GetCustomAttributes<DescriptionAttribute>())
                    .GroupBy(m => m.Description)
                    .Max(g => g.Count()) == 1,
                "Type contains duplicate display names");
        }

        public override bool CanConvertTo(
            ITypeDescriptorContext context,
            Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            Precondition.ExpectNotNull(value, "value");

            if (destinationType == typeof(string))
            {
                //
                // Lookup attribute by member name.
                //
                var description = this.EnumType
                    .GetField(value.ToString())
                    .GetCustomAttribute<DescriptionAttribute>()?
                    .Description;

                return description ?? value.ToString();
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public override object ConvertFrom(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value)
        {
            Precondition.ExpectNotNull(value, "value");

            //
            // Lookup member by attribute value.
            //
            var member = this.EnumType
                .GetFields()
                .Where(m => Equals(
                    m.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    value))
                .FirstOrDefault();

            if (member != null)
            {
                //
                // We found the member that matches the description.
                //
                return Enum.Parse(this.EnumType, member.Name);
            }
            else
            {
                //
                // The member might not have a description attribute,
                // so try to parse directly.
                //
                return Enum.Parse(this.EnumType, value.ToString());
            }
        }
    }
}
