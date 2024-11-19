//
// Copyright 2024 Google LLC
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

namespace Google.Solutions.Mvvm.Shell
{
    /// <summary>
    /// Shell Clipboard Formats.
    /// </summary>
    internal static class ShellDataFormats
    {
        /// <summary>
        /// This format identifier is used with the CFSTR_FILECONTENTS 
        /// format to transfer data as a group of files. 
        /// </summary>
        internal const string CFSTR_FILEDESCRIPTORW = "FileGroupDescriptorW";

        /// <summary>
        /// This format identifier is used with the CFSTR_FILEDESCRIPTOR 
        /// format to transfer data as if it were a file, regardless of how it 
        /// is actually stored.
        /// </summary>
        internal const string CFSTR_FILECONTENTS = "FileContents";

        /// <summary>
        /// This format identifier is used by the source to specify whether its 
        /// preferred method of data transfer is move or copy.
        /// </summary>
        internal const string CFSTR_PREFERREDDROPEFFECT = "Preferred DropEffect";

        /// <summary>
        /// This format identifier is used by the target to inform the data object
        /// through its IDataObject::SetData method of the outcome of a data transfer.
        /// </summary>
        internal const string CFSTR_PERFORMEDDROPEFFECT = "Performed DropEffect";
    }
}
