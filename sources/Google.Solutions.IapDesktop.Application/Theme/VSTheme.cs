using System.IO;
using System.Linq;
using WeifenLuo.WinFormsUI.ThemeVS2015;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// Visual Studio theme as defined by a .vstheme file.
    /// </summary>
    internal class VSTheme : VS2015ThemeBase
    {
        // TODO: Strip DLL to not ship 2015 themes

        private VSTheme(byte[] vsthemeXml) : base(vsthemeXml)
        {
        }

        /// <summary>
        /// Read gzip-compressed VSTheme XML file from embedded resource.
        /// </summary>
        public static VSTheme FromResource(string resourceName)
        {
            var assembly = typeof(VSTheme).Assembly;
            var qualifiedResourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(resourceName));

            if (qualifiedResourceName == null)
            {
                throw new IOException(
                    $"The theme {resourceName} does not exist");

            }
            using (var stream = assembly.GetManifestResourceStream(qualifiedResourceName))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return new VSTheme(Decompress(buffer.ToArray()));
            }
        }

        /// <summary>
        /// Read VSTheme XML file from a file.
        /// </summary>
        public static VSTheme FromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new IOException(
                    $"The theme file {filePath} does not exist");
            }

            return new VSTheme(File.ReadAllBytes(filePath));
        }
    }
}
