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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;

namespace Google.Solutions.IapDesktop.Application.Views
{
    /// <summary>
    /// Declare that a class can be surfaced as a context
    /// command in a toolbar or menu.
    /// 
    /// Classes that use this attribute must implement IMenuCommand<TMenu>
    /// where TMenu matches the value of the Menu attribute.
    /// 
    /// Example:
    /// 
    ///   [MenuCommand(typeof(SampleMenu), Rank = 0x100)]
    ///   internal class MyCommand : IMenuCommand<SampleMenu>
    ///   {
    ///      ...
    ///   }
    ///   
    /// </summary>
    public class MenuCommandAttribute : ServiceCategoryAttribute
    {
        /// <summary>
        /// Rank, used for ordering.
        /// 
        /// Whenever two consecutive ranks differ in more than the
        /// least-significant byte, a separator is injected between
        /// them.
        ///
        /// For ex:
        /// - 0x100
        /// - 0x110
        /// - 0x140
        ///     <- separator
        /// - 0x400
        /// - 0x410
        ///     <- separator
        /// - 0x510
        ///
        /// </summary>
        public ushort Rank { get; set; } = 0xFF00;

        /// <summary>
        /// Menu that this command is extending.
        /// </summary>
        public Type Menu { get; }

        /// <summary>
        /// Declare class as a command that extends a menu.
        /// </summary>
        /// <param name="menu">Marker type for the menu to extend</param>
        public MenuCommandAttribute(Type menu)
            : base(typeof(IMenuCommand<>).MakeGenericType(menu))
        {
            this.Menu = menu.ExpectNotNull(nameof(menu));
        }
    }
}
