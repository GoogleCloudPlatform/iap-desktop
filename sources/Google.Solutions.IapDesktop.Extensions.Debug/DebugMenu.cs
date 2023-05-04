﻿//
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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;

namespace Google.Solutions.IapDesktop.Extensions.Debug
{
    /// <summary>
    /// Debug menu in main menu. The menu is registered when
    /// the class is loaded (as service) during startup.
    /// </summary>
    [Service(ServiceLifetime.Singleton, DelayCreation = false)]
    public class DebugMenu : Menu<DebugMenu.Context>
    {
        /// <summary>
        /// Pseudo-context.
        /// </summary>
        public sealed class Context {
            public static Context None = new Context();
        }

        public DebugMenu(IServiceCategoryProvider serviceProvider)
            : base(
                  MenuCommandType.MenuCommand,
                  serviceProvider
                      .GetService<IMainWindow>()
                      .AddMenu<Context>(
                          "Debug",
                          3,
                          () => Context.None))
        {
            DiscoverCommands(serviceProvider);
        }
    }
}
