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
        private readonly DeviceCapabilities deviceCaps;

        public DpiAwarenessRuleset()
        {
            //
            // Get system DPI and use this for scaling operations.
            //
            this.deviceCaps = DeviceCapabilities.Get();
        }

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
            imageList.ImageSize = this.deviceCaps.ScaleToSystemDpi(imageList.ImageSize);

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
            if (c is ContainerControl container && (
                container.Controls.OfType<GroupBox>().Any() ||
                container.Controls.OfType<Button>().Any() ||
                container.Controls.OfType<RadioButton>().Any()))
            {
                //
                // GroupBoxes and certain other controls don't auto-scale
                // properly in DPI mode, so the container must use:
                //
                //   AutoScaleMode = Font
                //   CurrentAutoScaleDimensions = DpiAwareness.DefaultFont
                //
                Debug.Assert(container.AutoScaleMode == AutoScaleMode.Font);
                Debug.Assert(container.CurrentAutoScaleDimensions.Width >= DpiAwareness.DefaultFontSize.Width);
            }
            else if (c is Form form && !(form is INestedForm))
            {
                //
                // Forms must use:
                //
                //   AutoScaleMode = DPI
                //   CurrentAutoScaleDimensions = DpiAwareness.DefaultDpi
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
                    // adjusted correctly..
                    //
                    Debug.Assert(form.ControlBox);
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

        private void StyleToolStrip(ToolStrip toolStrip)
        {
            //toolStrip.AutoSize = false;
            toolStrip.ImageScalingSize 
                = this.deviceCaps.ScaleToSystemDpi(toolStrip.ImageScalingSize);
            
            //if (toolStrip is MenuStrip)
            //{
            //    return;
            //}

            //foreach (var item in toolStrip.Items.Cast<ToolStripItem>())
            //{
            //    item.Padding = this.deviceCaps.ScaleToSystemDpi(item.Padding);
            //    item.Margin = this.deviceCaps.ScaleToSystemDpi(item.Margin);
            //}

            //// TODO: Handle items that are added later.
            //// TODO: Handle sub-menus
            
            //toolStrip.GripMargin = this.deviceCaps.ScaleToSystemDpi(toolStrip.GripMargin);
            //toolStrip.Padding = this.deviceCaps.ScaleToSystemDpi(toolStrip.Padding);
            //toolStrip.Margin = this.deviceCaps.ScaleToSystemDpi(toolStrip.Margin);
        }
        private void StyleToolStripItem(ToolStripItem item)
        {
            //item.Padding = this.deviceCaps.ScaleToSystemDpi(item.Padding);
            item.Margin = this.deviceCaps.ScaleToSystemDpi(item.Margin);
        }

        private void StyleTextBox(TextBoxBase textBox)
        {
            //if (textBox.Multiline)
            //{
            //    // Quirk: adjust height
            //    textBox.Height /= 4; //TODO: What's the right factor here?
            //}
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
                column.Width = this.deviceCaps.ScaleToSystemDpi(column.Width);
            }
        }

        private void ForceRescaleForm(Form c)
        {
            if (c.Parent != null)
            {
                //
                // Top-level window. Force scaling and relayout
                // (after the form's handle has been created).
                //
                // Avoid doing the same for child window as that
                // would cause duplicate scaling.
                //
                //c.Font = this.UiFont;
                c.PerformAutoScale();
            }
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            if (this.deviceCaps.IsHighDpiEnabled)
            {
                //
                // Ensure that controls are properly configured
                // before their handle is created.
                //
                controlTheme.AddRule<Control>(VerifyScalingSettings);
                controlTheme.AddRule<PictureBox>(StylePictureBox);
                controlTheme.AddRule<ToolStrip>(StyleToolStrip);
                controlTheme.AddRule<TextBoxBase>(StyleTextBox);
                controlTheme.AddRule<TreeView>(StyleTreeView);
                controlTheme.AddRule<ListView>(StyleListView);

                var menuTheme = new ToolStripItemTheme(true);
                menuTheme.AddRule(i => StyleToolStripItem(i));
                controlTheme.AddRules(menuTheme);

                //
                // Force scaling once the handle has been created.
                //
                controlTheme.AddRule<Form>(ForceRescaleForm, ControlTheme.Options.ApplyWhenHandleCreated);
            }
        }
    }
}
