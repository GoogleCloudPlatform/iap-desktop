using System;

namespace Google.Solutions.Ssh.Native
{
    internal sealed class Disposable : IDisposable
    {
        private readonly Action dispose;

        private Disposable(Action dispose)
        {
            this.dispose = dispose;
        }

        public static IDisposable For(Action dispose)
        {
            return new Disposable(dispose);
        }

        public void Dispose()
        {
            this.dispose();
        }
    }
}
