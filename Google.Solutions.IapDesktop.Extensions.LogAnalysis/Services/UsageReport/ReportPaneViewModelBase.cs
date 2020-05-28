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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.UsageReport
{

    internal abstract class ReportPaneViewModelBase : ViewModelBase
    {
        protected readonly InstanceSetHistory instanceSet;

        private DateSelection dateSelection;
        private IEnumerable<DataPoint> histogram
            = Enumerable.Empty<DataPoint>();

        public ReportPaneViewModelBase(InstanceSetHistory instanceSetHistory)
        {
            this.instanceSet = instanceSetHistory;
            this.dateSelection = new DateSelection()
            {
                StartDate = instanceSetHistory.StartDate,
                EndDate = instanceSetHistory.EndDate
            };
        }

        protected abstract void Repopulate();

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------


        public IEnumerable<DataPoint> Histogram
        {
            get => this.histogram;
            protected set
            {
                this.histogram = value;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public DateSelection Selection
        {
            get => this.dateSelection;
            set
            {
                this.dateSelection = value;
                Repopulate();
                RaisePropertyChange();
            }
        }
    }
    internal struct DateSelection
    {
        public DateTime StartDate;
        public DateTime EndDate;
    }



}
