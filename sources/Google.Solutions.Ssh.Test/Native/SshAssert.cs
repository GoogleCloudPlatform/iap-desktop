using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    internal static class SshAssert
    {
        public static void ThrowsNativeExceptionWithError(
            LIBSSH2_ERROR expected, 
            Action action)
        {
            try
            {
                action();
                Assert.Fail("Expected SshNativeException with error " + expected);
            }
            catch (Exception e)
            {
                Assert.IsInstanceOf(typeof(SshNativeException), e.Unwrap());
                Assert.AreEqual(expected, ((SshNativeException)e.Unwrap()).ErrorCode);
            }
        }
    }
}
