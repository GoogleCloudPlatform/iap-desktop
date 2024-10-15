using Google.Solutions.Common.Text;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.SerialOutput
{
    /// <summary>
    /// Reader that removes Xterm control sequences from a stream
    /// that are commonly encountered in serial console streams. This
    /// is done on a best-effort basis.
    /// </summary>
    internal class XtermSanitizingReader : IAsyncReader<string> // TODO: Drop infix
    {
        /// <summary>
        /// Regex patterns for control sequences that are being sanitized.
        /// </summary>
        private static readonly string[] ControlSequencePatterns = new[] {
            "\u001b\\[2J",     // Clear the screen.
            "\u001b\\[01;01H", // Set cursor position to the top-left corner.
            "\u001b\\[=3h",    // Set the terminal to a application keypad mode.
        };

        private static readonly Regex allControlSequencePatterns = new Regex(
            string.Join("|", ControlSequencePatterns));

        private readonly IAsyncReader<string> reader;

        public XtermSanitizingReader(IAsyncReader<string> reader)
        {
            this.reader = reader;
        }

        //----------------------------------------------------------------------
        // IAsyncReader.
        //----------------------------------------------------------------------

        public async Task<string> ReadAsync(CancellationToken token)
        {
            var chunk = await this.reader
                .ReadAsync(token)
                .ConfigureAwait(false);

            return allControlSequencePatterns.Replace(chunk, string.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.reader.Dispose();
            }
        }
    }
}
