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

using Google.Solutions.Mvvm.Binding;
using System;

namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    internal class DebugOptionsSheetViewModel : PropertiesSheetViewModelBase
    {
        public DebugOptionsSheetViewModel() : base("Debug")
        {
            this.IsDirty = ObservableProperty.Build(false);
            this.FailToApplyChanges = ObservableProperty.Build(false);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public override ObservableProperty<bool> IsDirty { get; }
        public ObservableProperty<bool> FailToApplyChanges { get; }


        //---------------------------------------------------------------------
        // Apply changes.
        //---------------------------------------------------------------------

        protected override void ApplyChanges()
        {
            if (this.FailToApplyChanges.Value)
            {
                throw new InvalidOperationException("Applying changes failed");
            }
            else
            {
                this.IsDirty.Value = false;
            }
        }
    }
}
