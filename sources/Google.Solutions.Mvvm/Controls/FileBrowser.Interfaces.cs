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

using System;
using System.IO;
using System.ComponentModel;
using Google.Solutions.Mvvm.Shell;

namespace Google.Solutions.Mvvm.Controls
{
    public partial class FileBrowser
    {
        /// <summary>
        /// A file or directory.
        /// </summary>
        public interface IFileItem : INotifyPropertyChanged
        {
            /// <summary>
            /// Unqualified name of file.
            /// </summary>
            string Name { get; }

            /// <summary>
            /// File attributes.
            /// </summary>
            FileAttributes Attributes { get; }

            /// <summary>
            /// Time of last access, in UTC.
            /// </summary>
            DateTime LastModified { get; }

            /// <summary>
            /// Size of file.
            /// </summary>
            ulong Size { get; }

            /// <summary>
            /// Type information.
            /// </summary>
            FileType Type { get; }

            /// <summary>
            /// Gets or sets the expansion state.
            /// </summary>
            bool IsExpanded { get; set; }
        }
    }
}
