using Google.Solutions.Mvvm.Format;
using System;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Control that can render a limited subset of Markdown.
    /// </summary>
    public partial class MarkdownViewer : UserControl
    {
        private readonly MarkdownRtfConverter converter = new MarkdownRtfConverter();
        private string markdown = string.Empty;

        public MarkdownViewer()
        {
            InitializeComponent();
        }

        // TODO: Font, etc

        public string Markdown
        {
            get => this.markdown;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(Markdown));
                }

                var document = MarkdownDocument.Parse(value);
                this.richTextBox.Rtf = this.converter.ConvertToString(document);
                this.markdown = value;
            }
        }
    }
}
