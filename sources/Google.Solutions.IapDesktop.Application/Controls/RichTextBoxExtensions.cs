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

using Google.Solutions.IapDesktop.Application.Views;
using System.Windows.Forms;

#pragma warning disable CA1806 // Do not ignore method results

namespace Google.Solutions.IapDesktop.Application.Controls
{
    public static class RichTextBoxExtensions
    {
        public static void SetPadding(this RichTextBox textBox, int padding)
        {
            var rect = new UnsafeNativeMethods.RECT();
            UnsafeNativeMethods.SendMessageRect(
                textBox.Handle, 
                UnsafeNativeMethods.EM_GETRECT, 
                0, 
                ref rect);

            var newRect = new UnsafeNativeMethods.RECT(
                padding,
                padding, 
                rect.Right  - padding * 2, 
                rect.Bottom - padding * 2);

            UnsafeNativeMethods.SendMessageRect(
                textBox.Handle,
                UnsafeNativeMethods.EM_SETRECT,
                0,
                ref newRect);
        }
    }
}
