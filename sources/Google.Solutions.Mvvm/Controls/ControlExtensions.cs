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

using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public static class ControlExtensions
    {
        public static Task InvokeAsync(
            this Control control,
            Action action)
        {
            if (control.InvokeRequired)
            {
                var completionSource = new TaskCompletionSource<object>();
                control.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        action();
                        completionSource.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        completionSource.SetException(e);
                    }
                }));

                return completionSource.Task;
            }
            else
            {
                action();
                return Task.CompletedTask;
            }
        }

        public static void InvokeAndForget(
            this ISynchronizeInvoke control,
            Action action)
        {
            control.BeginInvoke(action, null);
        }

        /// <summary>
        /// List all controls, including any nested controls.
        /// </summary>
        public static IEnumerable<Control> AllControls(this Control control)
        {
            return Enumerable
                .Repeat(control, 1)
                .Concat(control.Controls
                    .Cast<Control>()
                    .EnsureNotNull()
                    .SelectMany(child => child.AllControls()));
        }


        /// <summary>
        /// Check that tab indexes have been properly assigned.
        /// </summary>
        [Conditional("DEBUG")]
        public static void ValidateTabIndexes(this ContainerControl control)
        {
            var duplicateTabIndexes = control
                .AllControls()
                .Where(c => c != control && c.TabStop)
                .GroupBy(c => c.TabIndex)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Debug.Assert(
                !duplicateTabIndexes.Any(),
                $"{control} has duplicate tab indexes: {string.Join(", ", duplicateTabIndexes)}");
        }
    }
}
