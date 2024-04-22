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
using System.Collections.Generic;
using System.IO;
using static Google.Solutions.Mvvm.Shell.FileType;

namespace Google.Solutions.Mvvm.Shell
{
    /// <summary>
    /// Thread-safe cache for file type information.
    /// </summary>
    public sealed class FileTypeCache : IDisposable
    {
        private readonly object cacheLock = new object();
        private readonly Dictionary<CacheKey, FileType> cache
            = new Dictionary<CacheKey, FileType>();

        internal int CacheSize => this.cache.Count;

        /// <summary>
        /// Look up file info using cache.
        /// </summary>
        public FileType Lookup(
            string filePath,
            FileAttributes fileAttributes,
            IconFlags size)
        {
            //
            // Determine the file extension and cache based on that.
            //
            var cacheKey = new CacheKey
            {
                Attributes = fileAttributes,
                FileExtension = fileAttributes.HasFlag(FileAttributes.Directory)
                    ? null
                    : new FileInfo(filePath).Extension,
                Flags = size
            };

            lock (this.cacheLock)
            {
                if (!this.cache.TryGetValue(cacheKey, out var info))
                {
                    info = FileType.Lookup(filePath, fileAttributes, size);
                    this.cache.Add(cacheKey, info);
                }

                return info;
            }
        }

        public void Dispose()
        {
            lock (this.cacheLock)
            {
                foreach (var fileType in this.cache.Values)
                {
                    fileType.Dispose();
                }
            }
        }

        private struct CacheKey
        {
            public FileAttributes Attributes;
            public string? FileExtension;
            public IconFlags Flags;

            public readonly override bool Equals(object obj)
            {
                return obj is CacheKey key &&
                    key.Attributes == this.Attributes &&
                    key.FileExtension == this.FileExtension &&
                    key.Flags == this.Flags;
            }

            public readonly override int GetHashCode()
            {
                return ((int)this.Attributes) ^
                    ((int)this.Flags) ^
                    (this.FileExtension?.GetHashCode() ?? 0);
            }
        }
    }
}
