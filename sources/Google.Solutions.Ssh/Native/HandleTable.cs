using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// Track open safe handles. Only active in Debug builds, for diagnostics only.
    /// </summary>
    internal static class HandleTable
    {
        private static readonly object handlesLock;
        private static readonly IDictionary<SafeHandle, string> handles;

        static HandleTable()
        {
#if DEBUG
            handlesLock = new object();
            handles = new Dictionary<SafeHandle, string>();
#endif
        }

        [Conditional("DEBUG")]
        public static void Clear()
        {
            lock (handlesLock)
            {
                handles.Clear();
            }
        }

        [Conditional("DEBUG")]
        public static void OnHandleCreated(SafeHandle handle, string description)
        {
            if (handle.IsInvalid)
            {
                return;
            }

            lock (handlesLock)
            {
                Debug.Assert(!handles.ContainsKey(handle));
                handles.Add(handle, description);
            }
        }

        [Conditional("DEBUG")]
        public static void OnHandleClosed(SafeHandle handle)
        {
            lock (handlesLock)
            {
                handles.Remove(handle);
            }
        }

        [Conditional("DEBUG")]
        public static void DumpOpenHandles()
        {
            Debug.Assert(handles.Count == 0);
            foreach (var entry in handles)
            {
                Debug.WriteLine("Leaked handle {0}: {1}", entry.Key, entry.Value);
            }
        }

        public static int HandleCount => handles != null
            ? handles.Count
            : 0;
    }
}
