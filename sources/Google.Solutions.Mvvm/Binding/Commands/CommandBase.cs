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

using System.Diagnostics;

namespace Google.Solutions.Mvvm.Binding.Commands
{
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class CommandBase : ICommandBase
    {
        private string? activityText;

        protected CommandBase(string text)
        {
            this.Text = text;
        }

        public virtual string Id
        {
            get => GetType().Name;
        }

        public string Text { get; protected set; }

        public string ActivityText
        {
            get => this.activityText ?? this.Text.Replace("&", string.Empty);
            set
            {
                Debug.Assert(
                    value.Contains("ing"),
                    "Action name should be formatted like 'Doing something'");

                this.activityText = value;
            }
        }
    }
}
