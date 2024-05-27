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

using Google.Solutions.Mvvm.Theme;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// UserControl that implements a workaround for 
    // https://github.com/dotnet/winforms/issues/6381.
    /// </summary>
    public class DpiAwareUserControl : UserControl
    {
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            if (this.Parent == null || !DeviceCapabilities.Current.IsHighDpi)
            {
                base.ScaleControl(factor, specified);
                return;
            }

            //
            // UserControl has been parented and we're in High-DPI mode. Winforms
            // autoscales the UserControl and _should_ resize any nested controls
            // that are anchored to scale horizontaly or vertically. But that doesn't
            // happen due to  https://github.com/dotnet/winforms/issues/6381.
            //
            // As a workaround, we look for affected controls and resize them
            // explicitly.
            //

            var horizontallyAnchoredControls = this.Controls
                .OfType<Control>()
                .Where(c => c.Anchor.HasFlag(AnchorStyles.Left | AnchorStyles.Right))
                .Select(c => new {
                    Control = c,
                    RightMargin = this.Width - c.Location.X - c.Width
                })
                .ToList();
            var verticallyAnchoredControls = this.Controls
                .OfType<Control>()
                .Where(c => c.Anchor.HasFlag(AnchorStyles.Top | AnchorStyles.Bottom))
                .Select(c => new {
                    Control = c,
                    BottomMargin = this.Width - c.Location.X - c.Width
                })
                .ToList();

            base.ScaleControl(factor, specified);

            foreach (var c in horizontallyAnchoredControls)
            {
                c.Control.Width = this.Width - c.Control.Location.X - c.RightMargin;
            }

            foreach (var c in verticallyAnchoredControls)
            {
                c.Control.Height = this.Height - c.Control.Location.Y - c.BottomMargin;
            }
        }
    }
}
