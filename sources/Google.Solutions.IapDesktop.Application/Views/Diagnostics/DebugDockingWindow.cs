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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    [ComVisible(false)]
    [SkipCodeCoverage("For debug purposes only")]
    public partial class DebugDockingWindow : ToolWindow
    {
        private readonly DockPanel dockPanel;
        private readonly StateSnapshot snapshot = new StateSnapshot();
        private int eventId = 1;

        public DebugDockingWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            this.TabText = this.Text;
            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
        }

        public void ShowWindow()
        {
            Show(this.dockPanel, DockState.DockRightAutoHide);
        }

        private void WriteOutput(string text)
        {
            Debug.Write(text);
            this.output.AppendText(text);
        }

        private void OnDockStateUpdated([CallerMemberName] string eventName = null)
        {
            WriteOutput($"\r\n--[{eventId++}]: {eventName}--\r\n");

            WriteOutput($"{this.IsActivated}\r\n");
            WriteOutput($"{this.IsHidden}\r\n");
            WriteOutput($"{this.IsFloat}\r\n");
            WriteOutput($"{this.VisibleState}\r\n");
            WriteOutput($"{this.DockState}\r\n");
            WriteOutput($"{this.Pane.ActiveContent == this}\r\n");
            WriteOutput($"\r\n");

            if (this.IsActivated != this.snapshot.IsActivated)
            {
                WriteOutput($"IsActivated: {this.snapshot.IsActivated} -> {this.IsActivated}\r\n");
            }
            if (this.IsHidden != this.snapshot.IsHidden)
            {
                WriteOutput($"IsHidden: {this.snapshot.IsHidden} -> {this.IsHidden}\r\n");
            }
            if (this.IsFloat != this.snapshot.IsFloat)
            {
                WriteOutput($"IsFloat: {this.snapshot.IsFloat} -> {this.IsFloat}\r\n");
            }
            if (this.VisibleState != this.snapshot.VisibleState)
            {
                WriteOutput($"VisibleState: {this.snapshot.VisibleState} -> {this.VisibleState}\r\n");
            }
            if (this.DockState != this.snapshot.DockState)
            {
                WriteOutput($"DockState: {this.snapshot.DockState} -> {this.DockState}\r\n");
            }
            if (this.IsUserVisible != this.snapshot.IsUserVisible)
            {
                WriteOutput($"IsUserVisible: {this.snapshot.IsUserVisible} -> {this.IsUserVisible}\r\n");
            }

            // Update grid 
            this.snapshot.IsActivated = this.IsActivated;
            this.snapshot.IsHidden = this.IsHidden;
            this.snapshot.IsFloat = this.IsFloat;
            this.snapshot.VisibleState = this.VisibleState;
            this.snapshot.DockState = this.DockState;
            this.snapshot.IsActiveContent = this.Pane.ActiveContent == this;
            this.snapshot.IsUserVisible = this.IsUserVisible;

            this.grid.SelectedObject = this.snapshot;
        }

        internal class StateSnapshot
        {
            public bool IsActivated { get; set; }
            public bool IsHidden { get; set; }
            public bool IsFloat { get; set; }
            public bool IsVisible { get; set; }
            public DockState VisibleState { get; set; }
            public DockState DockState { get; set; }
            public bool IsActiveContent { get; set; }
            public bool IsUserVisible { get; set; }
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        protected override void OnUserVisibilityChanged(bool visible)
        {
            OnDockStateUpdated();
            base.OnUserVisibilityChanged(visible);
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.output.Clear();
        }
    }
}
