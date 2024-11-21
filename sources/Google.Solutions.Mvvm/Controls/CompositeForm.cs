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

using Google.Solutions.Mvvm.Interop;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// A form that is drawn with WS_EX_COMPOSITED style.
    /// 
    /// If a form contains many consols, this style can
    /// provide a better (flicker-free) experience.
    /// </summary>
    public class CompositeForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                //
                // Draw all controls at once. This helps reduce flickering,
                // particularly in dark mode.
                //
                var cp = base.CreateParams;

                if (!this.DesignMode)
                {
                    //
                    // WS_EX_COMPOSITED can break scrolling if
                    // DPI virtualization is active.
                    //
                    cp.ExStyle |= (int)WindowStyles.WS_EX_COMPOSITED;
                }

                return cp;
            }
        }
    }
}
