//
// Copyright 2019 Google LLC
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

using Google.Solutions.IapDesktop.Application.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Controls
{
    public static class TextBoxExtensions
    {
        public static Button AddOverlayButton(
            this TextBox textBox,
            Image buttonImage)
        {
            var searchButton = new Button
            {
                Size = new Size(16, 16)
            };
            searchButton.Location = new Point(textBox.ClientSize.Width - searchButton.Width - 4, 2);
            searchButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            searchButton.FlatStyle = FlatStyle.Flat;
            searchButton.FlatAppearance.BorderSize = 0;
            searchButton.FlatAppearance.MouseOverBackColor = searchButton.BackColor;
            searchButton.BackColorChanged += (s, _) =>
            {
                searchButton.FlatAppearance.MouseOverBackColor = searchButton.BackColor;
            };
            searchButton.TabStop = false;
            searchButton.Image = buttonImage;
            searchButton.Cursor = Cursors.Default;
            textBox.Controls.Add(searchButton);

            // Send EM_SETMARGINS to prevent text from disappearing underneath the button
            UnsafeNativeMethods.SendMessage(
                textBox.Handle,
                UnsafeNativeMethods.EM_SETMARGINS,
                (IntPtr)2,
                (IntPtr)(searchButton.Width << 16));

            return searchButton;
        }
    }
}
