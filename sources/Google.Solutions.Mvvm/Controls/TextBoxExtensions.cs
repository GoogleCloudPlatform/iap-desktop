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

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
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

            Debug.Assert(textBox.Height > searchButton.Height);
            Debug.Assert(textBox.Width > searchButton.Width);

            searchButton.Location = new Point(
                textBox.ClientSize.Width - searchButton.Width - 4,
                (textBox.Height - searchButton.Height) / 2 - 1);
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
            NativeMethods.SendMessage(
                textBox.Handle,
                NativeMethods.EM_SETMARGINS,
                (IntPtr)2,
                (IntPtr)(searchButton.Width << 16));

            return searchButton;
        }

        public static void SetCueBanner(
            this TextBox textBox,
            string text,
            bool alsoShowOnFocus)
        {
            Debug.Assert(!textBox.Multiline);

            var result = NativeMethods.SendMessage(
                textBox.Handle,
                NativeMethods.EM_SETCUEBANNER,
                alsoShowOnFocus ? 1 : 0,
                text);
            Debug.Assert(result != IntPtr.Zero);
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal const int EM_SETMARGINS = 0xd3;
            internal const int EM_SETCUEBANNER = 0x1501;

            [DllImport("user32.dll")]
            internal static extern IntPtr SendMessage(
                IntPtr
                hWnd,
                int msg,
                IntPtr wp,
                IntPtr lp);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            internal static extern IntPtr SendMessage(
                IntPtr hWnd,
                int msg,
                int wParam,
                [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        }
    }
}
