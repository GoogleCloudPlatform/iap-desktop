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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    public class ToolStripItemTheme
    {
        private readonly LinkedList<Action<ToolStripItem>> rules
            = new LinkedList<Action<ToolStripItem>>();


        private void ApplyTo(ToolStripItem item)
        {
            //
            // Apply Rules to current item.
            //
            foreach (var rule in this.rules)
            {
                rule(item);
            }

            //
            // Recursively visit child items.
            //
            if (item is ToolStripDropDownItem dropDownItem)
            {
                //
                // Consider current child items...
                //
                List<WeakReference<ToolStripItem>>? appliedItems = null;
                if (this.IncludeControlsAddedLater)
                {
                    appliedItems = new List<WeakReference<ToolStripItem>>();
                }

                if (dropDownItem.DropDownItems != null)
                {
                    foreach (var subItem in dropDownItem.DropDownItems.OfType<ToolStripItem>())
                    {
                        ApplyTo(subItem);

                        if (this.IncludeControlsAddedLater && appliedItems != null)
                        {
                            //
                            // Memoize items we've applied already so that
                            // we don't reapply them again later.
                            //
                            appliedItems.Add(new WeakReference<ToolStripItem>(subItem));
                        }
                    }
                }

                //
                // ...and future items.
                //
                // There's no ItemAdded event, so we have to check for new items
                // every time the menu pops up.
                //
                if (this.IncludeControlsAddedLater)
                {
                    dropDownItem.DropDownOpening += OnDropDownOpening;
                }

                void OnDropDownOpening(object sender, EventArgs __)
                {
                    var openingItem = (ToolStripDropDownItem)sender;
                    if (openingItem.DropDownItems != null)
                    {
                        foreach (var subItem in openingItem.DropDownItems.OfType<ToolStripItem>())
                        {
                            if (!appliedItems.Any(i =>
                                i.TryGetTarget(out var appliedItem) && appliedItem == subItem))
                            {
                                ApplyTo(subItem);
                            }

                            Debug.Assert(appliedItems != null, "cannot be null here");
                            appliedItems!.Add(new WeakReference<ToolStripItem>(subItem));

                            Debug.Assert(
                                appliedItems.Count < 256,
                                "Cache should not grow excessively large");

                            if (subItem is ToolStripDropDownItem dropDownSubItem)
                            {
                                dropDownSubItem.DropDownOpening += OnDropDownOpening;
                            }
                        }

                    }

                    //
                    // Only allow each event to be fired once, otherwise
                    // the appliedItem list will grow too large.
                    //
                    openingItem.DropDownOpening -= OnDropDownOpening;
                }
            }
        }

        public ToolStripItemTheme(bool includeControlsAddedLater)
        {
            this.IncludeControlsAddedLater = includeControlsAddedLater;
        }

        //---------------------------------------------------------------------
        // Pubics.
        //---------------------------------------------------------------------

        public bool IncludeControlsAddedLater { get; }

        public void AddRule(Action<ToolStripItem> apply)
        {
            this.rules.AddLast(apply);
        }

        public void ApplyTo(ToolStrip toolStrip)
        {
            if (toolStrip == null)
            {
                return;
            }

            //
            // Apply theme to current items...
            //
            foreach (var item in toolStrip.Items.OfType<ToolStripItem>())
            {
                ApplyTo(item);
            }

            //
            // ...and future items.
            //
            if (this.IncludeControlsAddedLater)
            {
                toolStrip.ItemAdded += (_, args) =>
                {
                    ApplyTo(args.Item);
                };
            }
        }
    }

    public static class ToolStripItemThemeExtensions
    {
        /// <summary>
        /// Register rules so that menus and context menus
        /// are themed.
        /// </summary>
        public static void AddRules(
            this ControlTheme controlTheme,
            ToolStripItemTheme toolStripItemTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));
            toolStripItemTheme.ExpectNotNull(nameof(toolStripItemTheme));

            controlTheme.AddRule<ToolStrip>(c => toolStripItemTheme.ApplyTo(c));
        }
    }
}
