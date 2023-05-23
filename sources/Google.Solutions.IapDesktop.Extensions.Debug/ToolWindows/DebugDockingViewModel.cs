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

using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Debug.ToolWindows
{
    [Service]
    public class DebugDockingViewModel : ViewModelBase
    {
        public DebugDockingViewModel()
        {
            this.LogOutput = ObservableProperty.Build(string.Empty);
            this.Snapshot = ObservableProperty.Build(new StateSnapshot());
            this.SelectedObject = ObservableProperty.Build(this.Snapshot, s => (object)s);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> LogOutput { get; }

        public ObservableProperty<StateSnapshot> Snapshot { get; }

        public ObservableFunc<object> SelectedObject { get; }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void WriteOutput(string text)
        {
            System.Diagnostics.Debug.Write(text);
            this.LogOutput.Value += text;
        }

        public void ClearOutput()
        {
            this.LogOutput.Value = string.Empty;
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public class StateSnapshot
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
    }
}
