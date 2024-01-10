//
// Copyright 2023 Google LLC
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
using Google.Solutions.Mvvm.Binding;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Listview that support simple data binding.
    /// </summary>
    public class BindableComboBox : ComboBox
    {
        /// <summary>
        /// Bind to an enum-typed property.
        /// </summary>
        public void BindObservableProperty<TEnum>(
            IObservableWritableProperty<TEnum> property,
            IBindingContext bindingContext)
            where TEnum : struct
        {
            Precondition.ExpectNotNull(property, nameof(property));

            var adapter = new SelectionAdapter<TEnum>(property);

            //
            // Show the friendly value, not the technical enum value.
            //
            this.FormattingEnabled = true;
            this.Format += (_, e) =>
            {
                var v = ((Enum)e.Value);
                if (v != null)
                {
                    e.Value = v.GetAttribute<DisplayAttribute>()?.Name ?? v.ToString();
                }
            };

            //
            // ComboBox only has events for SelectedIndex,
            // so we need an adapter to translate that to a proper
            // enum value.
            //
            this.Items.AddRange(adapter.Options.Cast<object>().ToArray());
            this.BindProperty(
                c => c.SelectedIndex,
                adapter,
                m => m.SelectedIndex,
                bindingContext);
        }

        internal class SelectionAdapter<TEnum> : INotifyPropertyChanged
        {
            private readonly IObservableWritableProperty<TEnum> property;

            public event PropertyChangedEventHandler? PropertyChanged;

            private bool IsEnumValueDisplayed(string enumValue)
            {
                return typeof(TEnum)
                    .GetMember(enumValue)
                    .FirstOrDefault()?
                    .GetCustomAttribute<DisplayAttribute>()?.Name != null;
            }

            public SelectionAdapter(IObservableWritableProperty<TEnum> property)
            {
                this.property = property;

                //
                // Only consider options that have a DisplayAttribute.
                //
                this.Options = Enum
                    .GetNames(typeof(TEnum))
                    .Where(name => IsEnumValueDisplayed(name))
                    .Select(val => (TEnum)Enum.Parse(typeof(TEnum), val))
                    .ToArray();
            }

            public TEnum[] Options { get; }

            public int SelectedIndex
            {
                get
                {
                    return Array.IndexOf(this.Options, this.property.Value);
                }
                set
                {
                    Debug.Assert(value >= 0);
                    Debug.Assert(value < this.Options.Length);

                    this.property.Value = this.Options[value];
                    this.PropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs(nameof(SelectedItem)));
                }
            }
        }
    }
}
