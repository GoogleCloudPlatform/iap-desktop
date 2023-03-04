using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Rich text box that uses RICHEDIT50W.
    /// </summary>
    public class RichTextBox50 : RichTextBox
    {
#if !NET471_OR_GREATER
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                NativeMethods.LoadLibraryW("MsftEdit.dll");
                cp.ClassName = "RichEdit50W";
                return cp;
            }
        }

        private static class NativeMethods
        {

            [DllImport(
                "kernel32.dll", 
                EntryPoint = "LoadLibraryW",
                CharSet = CharSet.Unicode,
                SetLastError = true)]
            internal static extern IntPtr LoadLibraryW(string file);
        }
    }
#endif
}
