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


using Google.Solutions.IapDesktop.Application;
using Google.Solutions.Mvvm.Binding.Commands;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    public class UrlCommands
    {
        public UrlCommands()
        {
            this.LaunchRdpUrl = new ContextCommand<IapRdpUrl>(
                string.Empty,
                _ => CommandState.Unavailable,
                _ => { });
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        /// <summary>
        /// Command to launch a URL. Can be replaced by extensions.
        /// </summary>
        public IContextCommand<IapRdpUrl> LaunchRdpUrl { get; set; }
    }
}
