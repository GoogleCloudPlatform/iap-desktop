//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    internal static class SshAssert
    {
        public static void ThrowsNativeExceptionWithError(
            Libssh2Session? session,
            LIBSSH2_ERROR expected,
            Action action)
        {
            try
            {
                action();
                Assert.Fail("Expected SshNativeException with error " + expected);
            }
            catch (Exception e) when (!(e is AssertionException))
            {
                Assert.IsInstanceOf(typeof(Libssh2Exception), e.Unwrap());
                Assert.That(((Libssh2Exception)e.Unwrap()).ErrorCode, Is.EqualTo(expected));

                if (session != null)
                {
                    Assert.IsTrue(session.LastError == LIBSSH2_ERROR.NONE ||
                                  session.LastError == expected);
                }
            }
        }

        public static void ThrowsSftpNativeExceptionWithError(
            LIBSSH2_FX_ERROR expected,
            Action action)
        {
            try
            {
                action();
                Assert.Fail("Expected SshNativeException with error " + expected);
            }
            catch (Exception e) when (!(e is AssertionException))
            {
                Assert.IsInstanceOf(typeof(Libssh2SftpException), e.Unwrap());
                Assert.That(((Libssh2SftpException)e.Unwrap()).ErrorCode, Is.EqualTo(expected));
            }
        }

        public static void ThrowsAggregateExceptionWithError(
            LIBSSH2_ERROR expected,
            TestDelegate code)
        {
            ThrowsNativeExceptionWithError(
                null,
                expected,
                () =>
            {
                try
                {
                    code();
                }
                catch (AggregateException e)
                {
                    throw e.Unwrap();
                }
            });
        }

        public static void ThrowsAggregateExceptionWithError(
            LIBSSH2_FX_ERROR expected,
            TestDelegate code)
        {
            ThrowsSftpNativeExceptionWithError(
                expected,
                () =>
                {
                    try
                    {
                        code();
                    }
                    catch (AggregateException e)
                    {
                        throw e.Unwrap();
                    }
                });
        }
    }
}
