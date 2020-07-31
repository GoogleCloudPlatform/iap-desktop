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

using Google.Solutions.IapDesktop.Application.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;

namespace Google.Solutions.IapDesktop.Application.Controls
{
    public static class ListViewExtensions
    {
        private static void CopyToClipboard(
            IEnumerable<ColumnHeader> headers,
            IEnumerable<ListViewItem> items)
        {
            //
            // Add contents to clipboard in tab-separated format. This format is
            // understood by both Google Sheets and Excel.
            //

            var tsvBuffer = new StringBuilder();
            tsvBuffer.Append(string.Join(
                "\t",
                headers.Select(h => "\"" + h.Text + "\"")));
            tsvBuffer.Append("\r\n");

            foreach (var item in items)
            {
                tsvBuffer.Append(string.Join(
                    "\t",
                    item.SubItems.Cast<ListViewSubItem>().Select(s => "\"" + s.Text + "\"")));
                tsvBuffer.Append("\r\n");
            }

            Clipboard.SetText(tsvBuffer.ToString(), TextDataFormat.Text);
        }

        private static void CopyToClipboard(
            ListView listView,
            bool selectedItemsOnly)
        {
            CopyToClipboard(
                listView.Columns.Cast<ColumnHeader>(),
                selectedItemsOnly
                    ? listView.SelectedItems.Cast<ListViewItem>()
                    : listView.Items.Cast<ListViewItem>());
        }

        public static void AddCopyCommands(this ListView listView)
        {
            if (listView.ContextMenuStrip == null)
            {
                listView.ContextMenuStrip = new ContextMenuStrip();
            }

            listView.ContextMenuStrip.Items.Add(
                new ToolStripMenuItem(
                    "&Copy",
                    Resources.Copy_16x,
                    (sender, args) => CopyToClipboard(listView, true)));

            listView.ContextMenuStrip.Items.Add(
                new ToolStripMenuItem(
                    "Copy &all",
                    Resources.Copy_16x,
                    (sender, args) => CopyToClipboard(listView, false)));
        }
    }
}
