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

using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh
{
    [Service]
    internal class DownloadFileViewModel : ViewModelBase
    {
        //
        // NB. We're not allowing:
        // - directories, because that would require a recursive download.
        // - links, because we don't have proper file sizes for those.
        //
        private static bool IsDownloadable(FileBrowser.IFileItem item)
            => item.Type.IsFile &&
               !item.Attributes.HasFlag(System.IO.FileAttributes.ReparsePoint);

        public DownloadFileViewModel(FileBrowser.IFileSystem fileSystem)
        {
            this.FileSystem = fileSystem;

            this.DialogText = ObservableProperty.Build(string.Empty);
            this.SelectedFiles = ObservableProperty.Build(Enumerable.Empty<FileBrowser.IFileItem>());
            this.TargetDirectory = ObservableProperty.Build(KnownFolders.Downloads);
            this.IsDownloadButtonEnabled = ObservableProperty.Build(
                this.SelectedFiles,
                files => files.Any() && files.All(IsDownloadable));
        }

        public FileBrowser.IFileSystem FileSystem { get; }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> DialogText { get; }
        public ObservableProperty<IEnumerable<FileBrowser.IFileItem>> SelectedFiles { get; }
        public ObservableProperty<string> TargetDirectory { get; }
        public ObservableFunc<bool> IsDownloadButtonEnabled { get; }
    }
}
