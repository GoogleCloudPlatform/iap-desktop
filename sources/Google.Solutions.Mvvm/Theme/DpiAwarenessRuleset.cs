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
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for DPI-awareness.
    /// </summary>
    public class DpiAwarenessRuleset : ControlTheme.IRuleSet
    {

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private void VerifyScalingSettings(Control c)
        {
#if DEBUG
            if (c is Form form)
            {
                //
                // Forms must use:
                //
                //   AutoScaleMode = DPI
                //   CurrentAutoScaleDimensions = 96x96
                //
                // INestedForm (ToolWindows) are special and must follow
                // the conventions for ContainerControls.
                //

                Debug.Assert(form.AutoScaleMode == AutoScaleMode.Dpi);
                Debug.Assert(form.CurrentAutoScaleDimensions.Width >= DpiAwareness.DefaultDpi.Width);
                Debug.Assert(form.CurrentAutoScaleDimensions.Width == form.CurrentAutoScaleDimensions.Height);

                if (form.FormBorderStyle == FormBorderStyle.FixedDialog)
                {
                    //
                    // If the Control box is hidden, the size of the form isn't
                    // adjusted correctly.
                    //
                    Debug.Assert(form.ControlBox);
                }
            }
            else if (c is UserControl userControl)
            {
                //
                // UserControls must use:
                //
                //   AutoScaleMode = DPI
                //   CurrentAutoScaleDimensions = 96x96
                //
                // INestedForm (ToolWindows) are special and must follow
                // the conventions for ContainerControls.
                //

                Debug.Assert(userControl.AutoScaleMode == AutoScaleMode.Dpi);
                Debug.Assert(userControl.CurrentAutoScaleDimensions.Width >= DpiAwareness.DefaultDpi.Width);
                Debug.Assert(userControl.CurrentAutoScaleDimensions.Width == userControl.CurrentAutoScaleDimensions.Height);

                if (userControl.Dock == DockStyle.Fill ||
                    userControl.Anchor.HasFlag(AnchorStyles.Top | AnchorStyles.Bottom) ||
                    userControl.Anchor.HasFlag(AnchorStyles.Left | AnchorStyles.Right))
                {
                    //
                    // Winforms scales user controls, but doesn't rearrange
                    // nested controls.
                    //
                    // This is a known and unfixed bug in NetFx, see
                    // https://github.com/dotnet/winforms/issues/6381.
                    //
                    Debug.Assert(false, "User control auto-scaling is not implemented on NetFx");
                }
            }
            else if (c is PropertyGrid)
            {
                //
                // PropertyGrid uses Mode = None, and that's ok.
                //
            }
            else if (c is ContainerControl otherContainer)
            { 
                //
                // Other containers should use Mode = Inherit.
                //
                Debug.Assert(otherContainer.AutoScaleMode == AutoScaleMode.Inherit);
            }
#endif
        }

        private void StylePictureBox(PictureBox pictureBox)
        {
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void StyleListView(ListView listView)
        {
            //
            // ListView doesn't scale columns automatically.
            //
            foreach (ColumnHeader column in listView.Columns)
            {
                column.Width = DeviceCapabilities.Current.ScaleToDpi(column.Width);
            }
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            if (DeviceCapabilities.Current.IsHighDpi)
            {
                //
                // Ensure that controls are properly configured
                // before their handle is created.
                //
                controlTheme.AddRule<Control>(VerifyScalingSettings);
                controlTheme.AddRule<PictureBox>(StylePictureBox);
                controlTheme.AddRule<ListView>(StyleListView);
            }
        }
    }
}
