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
using System.ComponentModel;
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

        private void StyleToolStrip(ToolStrip toolStrip)
        {
            toolStrip.ImageScalingSize 
                = DeviceCapabilities.Current.ScaleToDpi(toolStrip.ImageScalingSize);
        }

        private void StyleToolStripItem(ToolStripItem item)
        {
            item.Margin = DeviceCapabilities.Current.ScaleToDpi(item.Margin);
        }

        private void StyleTreeView(TreeView treeView)
        {
            ScaleImageList(treeView.ImageList);
            ScaleImageList(treeView.StateImageList);
        }

        private void StyleListView(ListView listView)
        {
            ScaleImageList(listView.SmallImageList);
            ScaleImageList(listView.LargeImageList);
            ScaleImageList(listView.StateImageList);

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
                controlTheme.AddRule<ToolStrip>(StyleToolStrip);
                controlTheme.AddRule<TreeView>(StyleTreeView);
                controlTheme.AddRule<ListView>(StyleListView);

                var menuTheme = new ToolStripItemTheme(true);
                menuTheme.AddRule(i => StyleToolStripItem(i));
                controlTheme.AddRules(menuTheme);
            }
        }
    }
}
