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

using Google.Solutions.Mvvm.Drawing;
using System;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    internal static class TerminalFont  // TODO: move
    {
        public const string DefaultFontFamily = "Consolas";
        public const float DefaultSize = 9.75f;
        public const float MinimumSize = 4.0f;
        public const float MaximumSize = 36.0f;

        public static bool IsValidFont(Font font)
        {
            return font.IsMonospaced();
        }

        public static bool IsValidFont(string fontFamily)
        {
            try
            {
                using (var font = new Font(fontFamily, DefaultSize))
                {
                    return IsValidFont(font);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
