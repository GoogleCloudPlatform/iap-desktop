//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Diagnostics;
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Platform;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Diagnostics
{
    public class BugReport
    {
        private readonly Exception? exception;
        private readonly Type? source;

        public BugReport()
            : this(null, null)
        {
        }

        public BugReport(
            Type? source,
            Exception? exception)
        {
            this.source = source;
            this.exception = exception;
        }

        /// <summary>
        /// Window that produced the issue, if any.
        /// </summary>
        public IWin32Window? SourceWindow { get; set; }

        /// <summary>
        /// Trace of recent window messages, if any.
        /// </summary>
        public MessageTrace? WindowMessageTrace { get; set; }

        public override string ToString()
        {
            var text = new StringBuilder();

            if (this.exception != null)
            {
                for (var ex = this.exception; ex != null; ex = ex.InnerException)
                {
                    text.Append(ex.ToString(ExceptionFormatOptions.IncludeOffsets));
                    foreach (DictionaryEntry dataItem in ex.Data)
                    {
                        text.Append($"\n   {dataItem.Key}: {dataItem.Value}");
                    }

                    text.Append("\n\n");
                }

                if (this.exception is ReflectionTypeLoadException tle)
                {
                    text.Append("\nLoader Exceptions:\n");
                    foreach (var e in tle.LoaderExceptions)
                    {
                        text.Append(e.ToString(ExceptionFormatOptions.IncludeOffsets));
                        text.Append("\n\n");
                    }
                }
            }

            if (this.WindowMessageTrace != null)
            {
                text.Append("\nMessage history:\n");
                text.Append(this.WindowMessageTrace);
                text.AppendLine();
            }

            if (this.SourceWindow == null)
            {
                //
                // Ignore.
                //
            }
            else if (this.SourceWindow is Control control &&
                control.FindForm() is var form &&
                form != null)
            {
                text.Append($"Window: {form.Name} ({control.GetType().Name})\n");
                text.Append($"Control: {control.Name} ({control.GetType().Name})\n");
            }
            else if (this.SourceWindow.Handle == Process.GetCurrentProcess().MainWindowHandle)
            {
                text.Append("Window: main\n");
            }

            text.Append($"Source: {this.source?.Name ?? string.Empty}\n");
            text.Append($"Version: {GetType().Assembly.GetName().Version}\n");
            text.Append($"Runtime: {ClrVersion.Version} ({ProcessEnvironment.ProcessArchitecture})\n");
            text.Append($"OS: {Environment.OSVersion} ({ProcessEnvironment.NativeArchitecture})\n");
            text.Append($"DPI: {DeviceCapabilities.Current.Dpi}/{DeviceCapabilities.System.Dpi}\n");

            return text.ToString();
        }
    }
}
