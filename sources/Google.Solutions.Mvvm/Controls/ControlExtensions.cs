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

using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public static class ControlExtensions
    {
        /// <summary>
        /// List all controls, including any nested controls.
        /// </summary>
        public static IEnumerable<Control> AllControls(this Control control)
        {
            return Enumerable
                .Repeat(control, 1)
                .Concat(control.Controls
                    .Cast<Control>()
                    .EnsureNotNull()
                    .SelectMany(child => child.AllControls()));
        }

        public static void CenterHorizontally(
            this Control control,
            Form form)
        {
            control.Location = new Point(
                (form.Width - control.Width) / 2,
                control.Location.Y);
        }

        /// <summary>
        /// Check that tab indexes have been properly assigned.
        /// </summary>
        [Conditional("DEBUG")]
        public static void ValidateTabIndexes(this ContainerControl control)
        {
            var duplicateTabIndexes = control
                .AllControls()
                .Where(c => c != control && c.TabStop)
                .GroupBy(c => c.TabIndex)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Debug.Assert(
                !duplicateTabIndexes.Any(),
                $"{control} has duplicate tab indexes: {string.Join(", ", duplicateTabIndexes)}");
        }

        /// <summary>
        /// Attach the lifetime of a disposable object to that of a control.
        /// </summary>
        public static void AttachDisposable(
            this IComponent component,
            IDisposable disposable)
        {
            component.Disposed += (_, __) => disposable.Dispose();
        }

        /// <summary>
        /// Converts a Logical DPI value to its equivalent DeviceUnit DPI value.
        /// </summary>
        public static Size LogicalToDeviceUnits(this Control c, Size s)
        {
            return new Size(
                c.LogicalToDeviceUnits(s.Width),
                c.LogicalToDeviceUnits(s.Height));
        }

        /// <summary>
        /// Converts a Logical DPI value to its equivalent DeviceUnit DPI value.
        /// </summary>
        public static Point LogicalToDeviceUnits(this Control c, Point p)
        {
            return new Point(
                c.LogicalToDeviceUnits(p.X),
                c.LogicalToDeviceUnits(p.Y));
        }
    }
}
