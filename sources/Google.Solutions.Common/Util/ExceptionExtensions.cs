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

using Google.Solutions.Common.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Util
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Create a stack trace that looks like a default stack trace,
        /// but additionally contains IL offsets.
        /// </summary>
        private static string CreateStackTraceWithOffsets(Exception e)
        {
            var buffer = new StringBuilder();
            buffer.AppendLine($"{e.GetType().FullName}: {e.Message}");

            foreach (var frame in new StackTrace(e, false)
                .GetFrames()
                .EnsureNotNull())
            {
                var method = frame.GetMethod();
                var parameters = string.Join(
                    ", ",
                    method
                        .GetParameters()
                        .EnsureNotNull()
                        .Select(p => $"{p.ParameterType.Name} {p.Name}"));

                buffer.Append($"   at {method.ReflectedType.FullName}.{method.Name}({parameters})");
                buffer.Append($" +IL_{frame.GetILOffset():x4}");

                var line = frame.GetFileName();
                if (line != null)
                {
                    buffer.Append($" in {line}:{frame.GetFileLineNumber()}");
                }

                buffer.AppendLine();
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Create a compact, single-line stack trace.
        /// </summary>
        private static string CreateCompactStackTrace(Exception e)
        {
            var buffer = new StringBuilder();
            buffer.Append($"{e.GetType().Name}: {e.Message}");

            if (new StackTrace(e, false)
                .GetFrames()
                .EnsureNotNull()
                .Where(f => !f.GetMethod().ReflectedType.Namespace.StartsWith("System."))
                .FirstOrDefault() is StackFrame frame)
            {
                var method = frame.GetMethod();
                buffer.Append($" at {method.ReflectedType.FullName}.{method.Name}");
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Remove all enclosing <c>AggregateException</c> and 
        /// <c>TargetInvocationException</c> exceptions.
        /// </summary>
        public static Exception Unwrap(this Exception e)
        {
            if (e is AggregateException aggregate &&
                aggregate.InnerException != null)
            {
                return aggregate.InnerException.Unwrap();
            }
            else if (e is TargetInvocationException target &&
                target.InnerException != null)
            {
                return target.InnerException.Unwrap();
            }
            else
            {
                return e;
            }
        }

        /// <summary>
        /// Test if the exception, when unwrapped, is of a certain
        /// type. Can be used in exception filters.
        /// </summary>
        public static bool Is<T>(this Exception e) where T : Exception
        {
            return e.Unwrap() is T;
        }

        public static bool IsCancellation(this Exception e)
        {
            return e.Is<TaskCanceledException>() || e.Is<OperationCanceledException>();
        }

        public static bool IsComException(this Exception e)
        {
            return e.Is<COMException>() || e.Is<InvalidComObjectException>();
        }

        /// <summary>
        /// Combine the exception message of all nested exceptions.
        /// </summary>
        public static string FullMessage(this Exception exception)
        {
            var fullMessage = new StringBuilder();

            for (var ex = exception; ex != null; ex = ex.InnerException)
            {
                if (fullMessage.Length > 0)
                {
                    fullMessage.Append(": ");
                }

                fullMessage.Append(ex.Message);
            }

            return fullMessage.ToString();
        }

        /// <summary>
        /// Format an exception.
        /// </summary>
        public static string ToString(
            this Exception exception, 
            ExceptionFormatOptions options)
        {
            return options switch
            {
                ExceptionFormatOptions.IncludeOffsets 
                    => CreateStackTraceWithOffsets(exception),

                ExceptionFormatOptions.Compact 
                    => CreateCompactStackTrace(exception),

                _ => exception.ToString(),
            };
        }
    }

    public enum ExceptionFormatOptions
    {
        /// <summary>
        /// Normal format.
        /// </summary>
        None,

        /// <summary>
        /// Include IL offsets.
        /// </summary>
        IncludeOffsets,

        /// <summary>
        /// Single-line format that only contains the most relevant
        /// information.
        /// </summary>
        Compact
    }
}
