using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Format
{
    internal static class MarkdownBlock
    {
        private static readonly char[] NonBreakingWhitespace = new char[] { ' ', '\t' };
        private static readonly char[] UnorderedListBullets = new char[] { '*', '-', '+' };

        internal abstract class Block
        {
            private Block next;
            private Block firstChild;
            private Block lastChild;

            public IEnumerable<Block> Children
            {
                get
                {
                    if (this.firstChild == null)
                    {
                        yield break;
                    }
                    else
                    {
                        for (var block = this.firstChild; block != null;  block = block.next)
                        {
                            yield return block;
                        }
                    }
                }
            }

            protected void AppendBlock(Block block)
            {
                if (this.firstChild == null)
                {
                    Debug.Assert(this.lastChild == null);
                    this.firstChild = block;
                    this.lastChild = this.firstChild;
                }
                else
                {
                    this.lastChild.next = block;
                    this.lastChild = block;
                }
            }

            internal virtual Block CreateBlock(string line)
            {
                if (UnorderedListItemBlock.IsUnorderedListItemBlock(line))
                {
                    return new UnorderedListItemBlock(line);
                }
                else
                {
                    return new TextBlock(line);
                }
            }

            internal virtual bool TryConsume(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    //
                    // An empty line always ends a block, but does
                    // not start a new one yet.
                    //
                    AppendBlock(new ParagraphBreak());
                    return false;
                }
                else if (this.lastChild != null && this.lastChild.TryConsume(line))
                {
                    //
                    // Continuation of last block.
                    //
                    return true;
                }
                else
                {
                    //
                    // Last block is closed, append a new block.
                    //
                    AppendBlock(CreateBlock(line));
                    return true;
                }
            }

            public abstract string Value { get; }

            public override string ToString()
            {
                var buffer = new StringBuilder();
                
                void Visit(Block block, int level)
                {
                    buffer.Append(new string(' ', level));
                    buffer.Append(block.Value);
                    buffer.Append('\n');

                    foreach (var child in block.Children)
                    {
                        Visit(child, level + 1);
                    }
                }

                Visit(this, 0);

                return buffer.ToString(); ;
            }
        }

        internal class DocumentBlock : Block
        {
            internal override Block CreateBlock(string line)
            {
                if (HeadingBlock.IsHeadingBlock(line))
                {
                    return new HeadingBlock(line);
                }
                else
                {
                    return base.CreateBlock(line);
                }
            }

            public override string Value => "[Document]";
        }

        internal class ParagraphBreak : Block
        {
            public override string Value => "[ParagraphBreak]";

            internal override bool TryConsume(string line)
            {
                return false;
            }
        }

        internal class HeadingBlock : Block
        {
            public int Level { get; }
            public string Text { get; }

            public static bool IsHeadingBlock(string line)
            {
                var index = line.IndexOfAny(NonBreakingWhitespace);
                return index > 0 && line.Substring(0, index).All(c => c == '#');
            }

            public HeadingBlock(string line)
            {
                Debug.Assert(IsHeadingBlock(line));

                var index = line.IndexOfAny(NonBreakingWhitespace);
                this.Level = line.Substring(0, index).Count();
                this.Text = line.Substring(index).Trim();
            }

            internal override bool TryConsume(string line)
            {
                //
                // Headings are always single-line.
                //
                return false;
            }

            public override string Value => $"[Heading level={this.Level}] {this.Text}";
        }

        internal class TextBlock : Block
        {
            public string Text { get; private set; }

            public TextBlock(string text)
            {
                this.Text = text;
            }

            internal override bool TryConsume(string line)
            {
                Debug.Assert(!string.IsNullOrWhiteSpace(line));
                this.Text += " " + line;
                return true;
            }

            public override string Value => "[Text] " + this.Text;
        }

        internal class UnorderedListItemBlock : Block
        {
            public char Bullet { get;}
            public string Indent { get; }

            public static bool IsUnorderedListItemBlock(string line)
            {
                return line.Length > 3 && 
                    UnorderedListBullets.Contains(line[0]) && 
                    NonBreakingWhitespace.Contains(line[1]);
            }


            public UnorderedListItemBlock(string line)
            {
                Debug.Assert(IsUnorderedListItemBlock(line));

                this.Bullet = line[0];

                var indent = 1;
                while (line[indent] == ' ')
                {
                    indent++;
                }

                this.Indent = new string(' ', indent);

                AppendBlock(new TextBlock(line.Substring(indent)));
            }

            internal override bool TryConsume(string line)
            {
                if (!line.StartsWith(this.Indent))
                {
                    //
                    // Line doesn't have the minimum amount of indentation,
                    // so it can't be a continuation.
                    //
                    // NB. We don't support lazy continations.
                    //
                    return false;
                }
                else
                {
                    return base.TryConsume(line.Substring(this.Indent.Length));
                }
            }

            public override string Value => $"[UnorderedListItem bullet={this.Bullet}]";
        }

        public static DocumentBlock Parse(TextReader reader)
        {
            var document = new DocumentBlock();
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }
                else
                {
                    document.TryConsume(line);
                }
            }

            return document;
        }

        public static DocumentBlock Parse(string markdown)
        {
            using (var reader = new StringReader(markdown))
            {
                return Parse(reader);
            }
        }
    }
}
