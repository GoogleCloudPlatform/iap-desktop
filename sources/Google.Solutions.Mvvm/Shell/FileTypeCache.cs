using Google.Apis.Util;
using Google.Solutions.Mvvm.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Solutions.Mvvm.Shell.FileType;

namespace Google.Solutions.Mvvm.Shell
{
    /// <summary>
    /// Cache for file type information.
    /// 
    /// The cache is not synchronized and intended to be used on
    /// the UI thread only.
    /// </summary>
    internal sealed class FileTypeCache : IDisposable
    {
        private readonly IDictionary<CacheKey, FileType> cache
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
                IconSize = size
            };

            if (!this.cache.TryGetValue(cacheKey, out var info))
            {
                info = FileType.Lookup(filePath, fileAttributes, size);
                this.cache.Add(cacheKey, info);
            }
                
            return info;
        }

        public void Dispose()
        {
            foreach (var fileType in this.cache.Values)
            {
                fileType.Dispose();
            }
        }

        private struct CacheKey
        {
            public FileAttributes Attributes;
            public string FileExtension;
            public IconFlags IconSize;

            public override bool Equals(object obj)
            {
                return obj is CacheKey key &&
                    key.Attributes == this.Attributes &&
                    key.FileExtension == this.FileExtension &&
                    key.IconSize == this.IconSize;
            }

            public override int GetHashCode()
            {
                return ((int)this.Attributes) ^
                    ((int)this.IconSize) ^
                    (this.FileExtension?.GetHashCode() ?? 0);
            }
        }
    }
}
