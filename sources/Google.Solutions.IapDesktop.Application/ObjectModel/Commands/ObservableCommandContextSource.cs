//
// Copyright 2022 Google LLC
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
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ObjectModel.Commands
{
    /// <summary>
    /// Source (typically a view model) for the context that
    /// determines the availability of commands.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface ICommandContextSource<TContext>
    {
        TContext Context { get; }
    }

    /// <summary>
    /// Basic context source implementation.
    /// </summary>
    public class ObservableCommandContextSource<TContext> 
        : ViewModelBase, ICommandContextSource<TContext>
    {
        private TContext context;

        public TContext Context
        {
            get => this.context;
            set
            {
                this.context = value;
                RaisePropertyChange();
            }
        }
    }
}
