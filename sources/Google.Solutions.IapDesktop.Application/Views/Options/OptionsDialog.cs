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
using Google.Solutions.IapDesktop.Application.Views.Properties;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    [SkipCodeCoverage("UI code")]
    public class OptionsDialog : PropertiesDialog
    {
        public OptionsDialog(IServiceCategoryProvider serviceProvider)
            : base()
        {
            this.Text = "Options";

            AddPane(new GeneralOptionsViewModel(serviceProvider));

            // Load all services implementing IOptionsDialogPane and
            // add them automatically. This gives extensions a chance
            // to plug in their own panes.
            foreach (var pane in serviceProvider
                .GetServicesByCategory<IOptionsDialogPane>()
                .OrderBy(p => p.Title))
            {
                AddPane(pane);
            }
        }
    }

    public interface IOptionsDialogPane : IPropertiesDialogPane
    { }
}
