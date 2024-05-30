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
using Google.Solutions.Mvvm.Controls;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for DPI-awareness.
    /// </summary>
    public class DpiAwarenessRuleset : ControlTheme.IRuleSet
    {
        //---------------------------------------------------------------------
        // Theming checks.
        //---------------------------------------------------------------------

#if DEBUG
        private void AssertControlStyle(Control c)
        {
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

                Debug.Assert(
                    form.AutoScaleMode == AutoScaleMode.Dpi ||
                    form.AutoScaleMode == AutoScaleMode.Inherit);
                if (form.AutoScaleMode == AutoScaleMode.Dpi)
                {
                    Debug.Assert(form.CurrentAutoScaleDimensions.Width >= DpiAwareness.DefaultDpi.Width);
                    Debug.Assert(form.CurrentAutoScaleDimensions.Width == form.CurrentAutoScaleDimensions.Height);
                }

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

                Debug.Assert(userControl.AutoScaleMode == AutoScaleMode.Dpi ||
                             userControl.AutoScaleMode == AutoScaleMode.Inherit);

                if (userControl.AutoScaleMode == AutoScaleMode.Dpi)
                {
                    Debug.Assert(userControl.CurrentAutoScaleDimensions.Width >= DpiAwareness.DefaultDpi.Width);
                    Debug.Assert(userControl.CurrentAutoScaleDimensions.Width == userControl.CurrentAutoScaleDimensions.Height);
                }

                bool isLaterallyAnchored(Control c)
                {
                    return 
                        c.Dock == DockStyle.Fill ||
                        c.Anchor.HasFlag(AnchorStyles.Top | AnchorStyles.Bottom) ||
                        c.Anchor.HasFlag(AnchorStyles.Left | AnchorStyles.Right);
                }

                //
                // If the UserControl is anchored and contains controls
                // that are also anchored, then it must use the
                // DpiAwareUserControl mitigation.
                //
                if (!(userControl is DpiAwareUserControl) &&
                    isLaterallyAnchored(userControl) &&
                    userControl.Controls.OfType<Control>().Any(isLaterallyAnchored)) 
                {
                    Debug.Assert(false, "User control should be derived from " + nameof(DpiAwareUserControl));
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
            else if (c is CheckBox checkBox)
            {
                //
                // Auto-scaling is prone to cause alignment issues.
                //
                Debug.Assert(!checkBox.AutoSize, $"{checkBox.Name} should use AutoScale = false");
            }
        }

        private void AssertToolStripItemStyle(ToolStripItem item)
        {
            Debug.Assert(item.ImageScaling == ToolStripItemImageScaling.SizeToFit);
        }
#endif
        //---------------------------------------------------------------------
        // Helper methods.
        //---------------------------------------------------------------------

        private void ScaleImageList(ImageList imageList)
        {
            if (imageList == null)
            {
                return;
            }

            var images = imageList
                .Images
                .Cast<Image>()
                .Select(i => (Image)i.Clone())
                .ToArray();

            //
            // Change the size. If the handle has been created already,
            // this causes the imagelist to reset the contained images.
            //
            imageList.ImageSize
                = DeviceCapabilities.Current.ScaleToDpi(imageList.ImageSize);

            if (imageList.Images.Count != images.Length)
            {
                //
                // Re-add images.
                //
                imageList.Images.AddRange(images);
            }
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

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

        private void StyleTreeView(TreeView treeView)
        {
            ScaleImageList(treeView.ImageList);
            ScaleImageList(treeView.StateImageList);
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            if (DeviceCapabilities.Current.IsHighDpi)
            {
#if DEBUG
                controlTheme.AddRule<Control>(AssertControlStyle);
                var menuTheme = new ToolStripItemTheme(true);
                menuTheme.AddRule(i => AssertToolStripItemStyle(i));
                controlTheme.AddRules(menuTheme);
#endif

                controlTheme.AddRule<PictureBox>(StylePictureBox);
                controlTheme.AddRule<ListView>(StyleListView);
                controlTheme.AddRule<TreeView>(StyleTreeView);
            }
        }
    }
}
