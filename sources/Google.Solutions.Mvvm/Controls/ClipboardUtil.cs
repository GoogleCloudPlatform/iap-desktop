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

using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public static class ClipboardUtil
    {
        public static string GetText()
        {
            try
            {
                return Clipboard.GetText();
            }
            catch (ExternalException)
            {
                //
                // Clipboard busy, ignore.
                //
                return string.Empty;
            }
        }

        public static void SetText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (ExternalException)
            {
                //
                // Clipboard busy, ignore.
                //
            }
        }

        public static void Clear()
        {
            try
            {
                Clipboard.Clear();
            }
            catch (ExternalException)
            {
                //
                // Clipboard busy, ignore.
                //
            }
        }

        public static void SetDataObject(object data)
        {
            try
            {
                Clipboard.SetDataObject(data);
            }
            catch (ExternalException)
            {
                //
                // Clipboard busy, ignore.
                //
            }
        }
    }
}
