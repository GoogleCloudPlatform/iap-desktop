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
using Google.Solutions.IapDesktop.Application.Util;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;

namespace Google.Solutions.IapDesktop.Application.Controls
{
    public static class ListViewExtensions
    {
        public static string ToTabSeparatedText(
            this ListView listView,
            bool selectedItemsOnly)
        {
            var headers = listView.Columns.Cast<ColumnHeader>();
            var items = selectedItemsOnly
                    ? listView.SelectedItems.Cast<ListViewItem>()
                    : listView.Items.Cast<ListViewItem>();

            var buffer = new StringBuilder();
            buffer.Append(string.Join(
                "\t",
                headers.Select(h => $"\"{h.Text.Replace("\"", "'")}\"")));
            buffer.Append("\r\n");

            foreach (var item in items)
            {
                buffer.Append(string.Join(
                    "\t",
                    item.SubItems
                        .Cast<ListViewSubItem>()
                        .Select(s => $"\"{s.Text.Replace("\"", "'")}\"")));
                buffer.Append("\r\n");
            }

            return buffer.ToString();
        }

        public static string ToHtml(
            this ListView listView,
            bool selectedItemsOnly)
        {
            var headers = listView.Columns.Cast<ColumnHeader>();
            var items = selectedItemsOnly
                    ? listView.SelectedItems.Cast<ListViewItem>()
                    : listView.Items.Cast<ListViewItem>();

            var buffer = new StringBuilder();
            buffer.AppendLine("<table>");

            buffer.AppendLine("<tr>");
            buffer.AppendLine(string.Join(
                string.Empty,
                headers.Select(h => $"<th>{HttpUtility.HtmlEncode(h.Text)}</th>")));
            buffer.AppendLine("</tr>");

            foreach (var item in items)
            {
                buffer.AppendLine("<tr>");
                buffer.AppendLine(string.Join(
                    string.Empty,
                    item.SubItems
                        .Cast<ListViewSubItem>()
                        .Select(s => $"<td>{HttpUtility.HtmlEncode(s.Text)}</td>")));
                buffer.AppendLine("</tr>");
            }

            buffer.AppendLine("</table>");

            return buffer.ToString();
        }

        private static void CopyToClipboard(
            ListView listView,
            bool selectedItemsOnly)
        {
            //
            // Add contents to clipboard in tab-separated and HTML format.
            // Tab-separated format is understood by Excel and Sheets,
            // HTML is for Docs and Word.
            //
            var dataObject = new DataObject();
            dataObject.SetData(
                DataFormats.Text,
                listView.ToTabSeparatedText(selectedItemsOnly));
            dataObject.SetData(
                DataFormats.Html,
                HtmlClipboardFormat.Format(listView.ToHtml(selectedItemsOnly)));

            Clipboard.SetDataObject(dataObject);
        }

        public static void AddCopyCommands(this ListView listView)
        {
            if (listView.ContextMenuStrip == null)
            {
                listView.ContextMenuStrip = new ContextMenuStrip();
            }

            var copy = new ToolStripMenuItem(
                "&Copy",
                Resources.Copy_16x,
                (sender, args) => CopyToClipboard(listView, true));
            listView.ContextMenuStrip.Items.Add(copy);

            var copyAll = new ToolStripMenuItem(
                "Copy &all",
                Resources.Copy_16x,
                (sender, args) => CopyToClipboard(listView, false));
            listView.ContextMenuStrip.Items.Add(copyAll);

            listView.ContextMenuStrip.Opening += (sender, args) =>
            {
                copy.Enabled = listView.SelectedIndices.Count > 0;
                copyAll.Enabled = listView.Items.Count > 0;
            };
        }
    }
}
