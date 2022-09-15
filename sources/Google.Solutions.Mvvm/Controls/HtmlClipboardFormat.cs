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

using System.Diagnostics;
using System.Text;

namespace Google.Solutions.Mvvm.Controls
{
    public static class HtmlClipboardFormat
    {
        private const string Prolog = "<!DOCTYPE><html><body><!--StartFragment -->";
        private const string Epilog = "<!--EndFragment --></body></html>";
        private const string Header =
            "Version:0.9\r\n" +
            "StartHTML:A0000000\r\n" +
            "EndHTML:B0000000\r\n" +
            "StartFragment:C0000000\r\n" +
            "EndFragment:D0000000";

        public static string Format(string htmlFragment)
        {
            Debug.Assert(!htmlFragment.Contains("<html"));

            var headerUtf8 = Encoding.UTF8.GetBytes(Header);
            var prologUtf8 = Encoding.UTF8.GetBytes(Prolog);
            var fragmentUtf8 = Encoding.UTF8.GetBytes(htmlFragment);
            var epilogUtf8 = Encoding.UTF8.GetBytes(Epilog);

            var htmlStart = headerUtf8.Length;
            var fragmentStart = htmlStart + prologUtf8.Length;
            var fragmentEnd = fragmentStart + fragmentUtf8.Length;
            var htmlEnd = fragmentEnd + epilogUtf8.Length;

            // Now patch the header to include the offsets.
            // Oddly, the offsets have to be in UTF8 offsets,
            // although the content is returned and placed
            // into the clipboard as (UCS-2) string.
            var patchedHeader = Header
                .Replace("A0000000", $"{htmlStart:00000000}")
                .Replace("B0000000", $"{htmlEnd:00000000}")
                .Replace("C0000000", $"{fragmentStart:00000000}")
                .Replace("D0000000", $"{fragmentEnd:00000000}");

            return patchedHeader + Prolog + htmlFragment + Epilog;
        }
    }
}
