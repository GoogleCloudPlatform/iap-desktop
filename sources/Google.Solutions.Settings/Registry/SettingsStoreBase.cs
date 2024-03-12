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
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace Google.Solutions.Settings.Registry
{
    /// <summary>
    /// Delegate for validating if a given value falls
    /// within the permitted range of a setting.
    /// </summary>
    public delegate bool ValidateDelegate<T>(T value);

    /// <summary>
    /// Store for settings.
    /// </summary>
    public abstract class SettingsStoreBase<TSource>
    {
        /// <summary>
        /// Create a value accessor for a given type.
        /// </summary>
        private protected abstract IValueAccessor<TSource, T> CreateValueAccessor<T>(
            string valueName);

        /// <summary>
        /// Source for setting values.
        /// </summary>
        private protected abstract TSource ValueSource { get; }

        /// <summary>
        /// Read key value and map it to a settings object.
        /// </summary>
        public virtual ISetting<T> Read<T>(
            string name,
            string displayName,
            string description,
            string category,
            T defaultValue,
            ValidateDelegate<T> validate = null)
        {
            var accessor = CreateValueAccessor<T>(name);

            bool isSpecified = accessor
                .TryRead(this.ValueSource, out var readValue);

            return new MappedSetting<T>(
                name,
                displayName,
                description,
                category,
                isSpecified ? readValue : defaultValue,
                defaultValue,
                isSpecified,
                false,
                accessor,
                validate ?? accessor.IsValid);
        }

        /// <summary>
        /// Write value back to registry.
        /// </summary>
        public void Write(ISetting setting)
        {
            Debug.Assert(setting.IsDirty);
            Debug.Assert(!setting.IsReadOnly);

            ((IMappedSetting)setting).Write(this.ValueSource);
        }

        protected interface IMappedSetting
        {
            void Write(TSource key);
        }

        protected class MappedSetting<T> : SettingBase<T>, IMappedSetting // TODO: Merge into SettingBase
        {
            private readonly IValueAccessor<TSource, T> accessor;
            private readonly ValidateDelegate<T> validate;

            internal MappedSetting(
                string key,
                string title,
                string description,
                string category,
                T initialValue,
                T defaultValue,
                bool isSpecified,
                bool readOnly,
                IValueAccessor<TSource, T> accessor,
                ValidateDelegate<T> validate) 
                : base(key, 
                      title, 
                      description, 
                      category, 
                      initialValue,
                      defaultValue,
                      isSpecified, 
                      readOnly)
            {
                this.accessor = accessor.ExpectNotNull(nameof(accessor));
                this.validate= validate.ExpectNotNull(nameof(validate));    
            }

            protected override SettingBase<T> CreateNew(
                T value, 
                T defaultValue, 
                bool readOnly) // TODO: remove 
            {
                return new MappedSetting<T>(
                    this.Key,
                    this.Title,
                    this.Description,
                    this.Category,
                    value,
                    defaultValue,
                    Equals(value, defaultValue),
                    readOnly,
                    this.accessor,
                    this.validate);
            }

            internal SettingBase<T> CreateSimilar(
                T value,
                T defaultValue,
                bool isSpecified,
                bool readOnly)
            {
                return new MappedSetting<T>(
                    this.Key,
                    this.Title,
                    this.Description,
                    this.Category,
                    value,
                    defaultValue,
                    isSpecified,
                    readOnly,
                    this.accessor,
                    this.validate);
            }

            protected override bool IsValid(T value)
            {
                return this.validate(value);
            }

            protected override T Parse(string value)// TODO: remove
            {
                throw new NotImplementedException();
            }

            public void Write(TSource key)
            {
                if (this.IsDefault)
                {
                    this.accessor.Delete(key);
                }
                else
                {
                    this.accessor.Write(key, this.Value);
                }
            }

            public bool IsCurrentValueValid
            {
                get => IsValid(this.Value);
            }
        }
    }
}
