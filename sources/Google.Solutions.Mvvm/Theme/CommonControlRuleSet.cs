//
// Copyright 2023 Google LLC
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
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for common controls.
    /// </summary>
    public class CommonControlRuleSet : ControlTheme.IRuleSet
    {
        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        internal void AutoSizeListViewColumns(ListView listView)
        {
            void ResizeLastColumnToFit()
            {
                if (listView.Columns.Count == 0)
                {
                    return;
                }

                int widthsOfAllButLastColumns = 0;
                for (int i = 0; i < listView.Columns.Count - 1; i++)
                {
                    widthsOfAllButLastColumns += listView.Columns[i].Width;
                }

                listView.Columns[listView.Columns.Count - 1].Width =
                    listView.ClientSize.Width - widthsOfAllButLastColumns - 4;
            }

            void OnColumnWidthChanged(object _, ColumnWidthChangedEventArgs e)
            {
                if (e.ColumnIndex != listView.Columns.Count - 1)
                {
                    ResizeLastColumnToFit();
                }
            }

            void OnSizeChanged(object _, EventArgs __)
            {
                ResizeLastColumnToFit();
            }

            listView.ColumnWidthChanged += OnColumnWidthChanged;
            listView.SizeChanged += OnSizeChanged;

            listView.Disposed += (_, __) =>
            {
                listView.ColumnWidthChanged -= OnColumnWidthChanged;
                listView.SizeChanged -= OnSizeChanged;
            };
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        /// <summary>
        /// Register rules.
        /// </summary>
        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));
            controlTheme.AddRule<ListView>(AutoSizeListViewColumns);
        }
    }
}
