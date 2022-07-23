﻿//
// Copyright 2022 Google LLC
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

using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.Common.Diagnostics;
using WeifenLuo.WinFormsUI.Docking;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    [SkipCodeCoverage("For development purposes only")]
    public partial class DebugThemeWindow : ToolWindow
    {
        public DebugThemeWindow(IServiceProvider serviceProvider)
            : base(serviceProvider, WeifenLuo.WinFormsUI.Docking.DockState.DockLeft)
        {
            InitializeComponent();

            var palette = serviceProvider
                .GetService<IThemeService>()
                .ColorPalette;

            this.propertyGrid.SelectedObject = new ColorPaletteInspector(palette);
        }

        private class ColorPaletteInspector : CustomTypeDescriptor
        {
            private readonly DockPanelColorPalette palette;

            public ColorPaletteInspector(DockPanelColorPalette palette)
            {
                this.palette = palette;
            }

            public override PropertyDescriptorCollection GetProperties(
                Attribute[] attributes)
                => GetProperties();

            public override PropertyDescriptorCollection GetProperties()
            {
                return new PropertyDescriptorCollection(
                    DiscoverProperties().ToArray());
            }

            private IEnumerable<PropertyDescriptor> DiscoverProperties()
            {
                foreach (var subPaletteProperty in this.palette
                    .GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    var subPalette = subPaletteProperty.GetValue(this.palette);

                    if (subPalette == null)
                    {
                        continue;
                    }

                    foreach (var colorProperty in subPalette
                        .GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => p.PropertyType == typeof(Color)))
                    {
                        yield return new SimplePropertyDescriptor(
                            subPaletteProperty.Name,
                            colorProperty,
                            colorProperty.GetValue(subPalette));
                    }
                }
            }
        }

        private class SimplePropertyDescriptor : PropertyDescriptor
        {
            private readonly string category;
            private readonly PropertyInfo property;
            private readonly object value;

            public SimplePropertyDescriptor(
                string category,
                PropertyInfo property, 
                object value) : 
                base(property.Name, null)
            {
                this.category = category;
                this.property = property;
                this.value = value;
            }

            public override string Category => this.category;

            public override Type ComponentType
                => null;

            public override bool IsReadOnly => true;

            public override Type PropertyType 
                => this.property.PropertyType;

            public override bool CanResetValue(object component) 
                => false;

            public override object GetValue(object component) 
                => this.value;

            public override void ResetValue(object component)
                => throw new NotImplementedException();

            public override void SetValue(object component, object value)
                => throw new NotImplementedException();

            public override bool ShouldSerializeValue(object component)
                => false;
        }
    }
}
