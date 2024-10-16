//
// Copyright 2024 Google LLC
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

using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// UserControl that lets derive classes react to form
    /// close-events.
    /// </summary>
    public abstract class ParentedUserControl : UserControl
    {
        protected ParentedUserControl()
        {
            //
            // As a user control, we don't get a FormClosing event,
            // so attach to the parent form. The parent form might change
            // as a result of a docking operation.
            //
            this.VisibleChanged += (_, __) =>
            {
                if (this.CurrentParentForm == null && FindForm() is Form form)
                {
                    this.CurrentParentForm = form;
                    this.CurrentParentForm.FormClosing += OnFormClosing;

                    OnCurrentParentFormChanged();
                }
            };
            this.ParentChanged += (_, __) =>
            {
                if (this.CurrentParentForm != null)
                {
                    this.CurrentParentForm.FormClosing -= OnFormClosing;
                }

                if (this.Parent?.FindForm() is Form newParent)
                {
                    this.CurrentParentForm = newParent;
                    this.CurrentParentForm.FormClosing += OnFormClosing;

                    OnCurrentParentFormChanged();
                }
            };
        }

        //---------------------------------------------------------------------
        // Parent tracking.
        //---------------------------------------------------------------------

        /// <summary>
        /// Form that currently hosts the user control. The form might change
        /// as a result of a docking operation.
        /// </summary>
        protected Form? CurrentParentForm { get; private set; }

        /// <summary>
        /// Invoked when the parent has changed.
        /// </summary>
        protected virtual void OnCurrentParentFormChanged() { }

        /// <summary>
        /// Invoked when the current parent form is closed.
        /// </summary>
        protected virtual void OnFormClosing(
            object sender,
            FormClosingEventArgs args)
        { }
    }
}
