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

using System;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// List view that automatically sizes columns to fit.
    /// </summary>
    public class AutosizeListView : ListView
    {
        //private void ResizeLastColumnToFit()
        //{
        //    if (this.Columns.Count == 0)
        //    {
        //        return;
        //    }

        //    int widthsOfAllButLastColumns = 0;
        //    for (int i = 0; i < this.Columns.Count - 1; i++)
        //    {
        //        widthsOfAllButLastColumns += this.Columns[i].Width;
        //    }

        //    this.Columns[this.Columns.Count - 1].Width =
        //        this.ClientSize.Width - widthsOfAllButLastColumns - 4;
        //}

        ////---------------------------------------------------------------------
        //// Event overrides.
        ////---------------------------------------------------------------------

        //protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
        //{
        //    if (e.ColumnIndex != this.Columns.Count - 1)
        //    {
        //        ResizeLastColumnToFit();
        //    }

        //    base.OnColumnWidthChanged(e);
        //}

        //protected override void OnSizeChanged(EventArgs e)
        //{
        //    ResizeLastColumnToFit();
        //    base.OnSizeChanged(e);
        //}
    }
}
