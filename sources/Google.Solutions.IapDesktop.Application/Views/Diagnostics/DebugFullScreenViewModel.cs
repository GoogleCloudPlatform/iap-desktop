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
using System.ComponentModel.DataAnnotations;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    public class DebugFullScreenViewModel : ViewModelBase
    {
        public DebugFullScreenViewModel()
        {
            this.SizeLabel = ObservableProperty.Build(string.Empty);
            this.TabAccentColor = ObservableProperty.Build(AccentColor.None);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> SizeLabel { get; }

        public ObservableProperty<AccentColor> TabAccentColor { get; }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public void OnWindowSizeChanged(Form window)
        {
            if (window != null)
            {
                this.SizeLabel.Value =
                    $"Window: {window.GetType().Name}\n" +
                    $"Size: {window.Size}\n" +
                    $"ClientSize: {window.ClientSize}\n" +
                    $"DisplayRectangle:{window.DisplayRectangle}";
            }
        }

        public enum AccentColor
        {
            [Display(Name = "None")]
            None = TabAccentColorIndex.None,

            [Display(Name = "Hightlight 1")]
            Hightlight1 = TabAccentColorIndex.Hightlight1,

            [Display(Name = "Hightlight 2")]
            Hightlight2 = TabAccentColorIndex.Hightlight2,

            [Display(Name = "Hightlight 3")]
            Hightlight3 = TabAccentColorIndex.Hightlight3,

            [Display(Name = "Hightlight 4")]
            Hightlight4 = TabAccentColorIndex.Hightlight4,
        }
    }
}
