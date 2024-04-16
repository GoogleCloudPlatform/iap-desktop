//
// Copyright 2023 Google LLC
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

using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.Platform.Interop;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// A Win32 job.
    /// </summary>
    public interface IWin32Job : IWin32ProcessSet, IDisposable
    {
        /// <summary>
        /// Job handle.
        /// </summary>
        SafeHandle Handle { get; }

        /// <summary>
        /// Add a process to the job.
        /// </summary>
        void Add(IWin32Process process);

        /// <summary>
        /// Return the IDs of processes in this job.
        /// </summary>
        IEnumerable<uint> ProcessIds { get; }

        /// <summary>
        /// Wait for all processes to terminate.
        /// </summary>
        Task WaitForProcessesAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken);
    }

    public class Win32Job : DisposableBase, IWin32Job
    {
        private readonly SafeJobHandle handle;

        public Win32Job(bool killOnJobClose)
        {
            var securityAttributes = new NativeMethods.SECURITY_ATTRIBUTES()
            {
                nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>(),

                //
                // Don't inherit hande to child process, otherwise they might
                // be able to modify the job.
                //
                bInheritHandle = false
            };

            var job = NativeMethods.CreateJobObject(ref securityAttributes, null);
            if (job.IsInvalid)
            {
                throw DispatchException.FromLastWin32Error("Creating job object failed");
            }

            if (killOnJobClose)
            {
                //
                // Configure the job so that it kills all member processes
                // when it's closed.
                //
                var jobLimits = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
                {
                    BasicLimitInformation = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION()
                    {
                        LimitFlags = NativeMethods.JOB_OBJECT_LIMIT.KILL_ON_JOB_CLOSE
                    }
                };

                if (!NativeMethods.SetInformationJobObject(
                    job,
                    NativeMethods.JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                    ref jobLimits,
                    (uint)Marshal.SizeOf<NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()))
                {
                    job.Close();

                    throw DispatchException.FromLastWin32Error("Configuring job limits failed");
                }
            }

            this.handle = job;
        }

        //---------------------------------------------------------------------
        // IWin32Job.
        //---------------------------------------------------------------------

        public SafeHandle Handle => this.handle;

        public void Add(IWin32Process process)
        {
            process.ExpectNotNull(nameof(process));

            if (!NativeMethods.AssignProcessToJobObject(
                this.handle,
                process.Handle))
            {
                throw DispatchException.FromLastWin32Error(
                    $"Assigining the process {process} to the job failed");
            }
        }

        public bool Contains(IWin32Process process)
        {
            process.ExpectNotNull(nameof(process));

            if (!NativeMethods.IsProcessInJob(
                process.Handle,
                this.handle,
                out var inJob))
            {
                throw DispatchException.FromLastWin32Error(
                    $"Checking if the process {process} is in the job failed");
            }

            return inJob;
        }

        public bool Contains(uint processId)
        {
            using (var process = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                false,
                processId))
            {
                if (process.IsInvalid)
                {
                    throw DispatchException.FromLastWin32Error(
                        $"Accessing the process with PID {processId} failed");
                }

                if (!NativeMethods.IsProcessInJob(
                    process,
                    this.handle,
                    out var inJob))
                {
                    throw DispatchException.FromLastWin32Error(
                        $"Checking if the process with PID {processId} " +
                        "is in the job failed");
                }

                return inJob;
            }
        }

        public IEnumerable<uint> ProcessIds
        {
            get
            {
                var size = (uint)Marshal.SizeOf<NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST>();
                while (true)
                {
                    using (var listHandle = GlobalAllocSafeHandle.GlobalAlloc(size))
                    {
                        var listPtr = listHandle.DangerousGetHandle();
                        if (NativeMethods.QueryInformationJobObject(
                            this.handle,
                            NativeMethods.JOBOBJECTINFOCLASS.JobObjectBasicProcessIdList,
                            listPtr,
                            size,
                            out var requiredLength))
                        {
                            var list = Marshal.PtrToStructure<NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST>(listPtr);

                            var arrayOffset =
                                Marshal.SizeOf<NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST>() - UIntPtr.Size;
                            var pids = new uint[list.NumberOfProcessIdsInList];
                            for (var i = 0; i < pids.Length; i++)
                            {
                                pids[i] = (uint)Marshal.ReadIntPtr(listPtr, arrayOffset + i * UIntPtr.Size).ToInt32();
                            }

                            return pids;
                        }
                        else if (requiredLength > 0)
                        {
                            //
                            // Try again with proper size.
                            //
                            size = requiredLength;
                        }
                        else if (size < ushort.MaxValue)
                        {
                            //
                            // QueryInformationJobObject sometimes fails without
                            // setting a required length.
                            //
                            size *= 2;
                        }
                        else
                        {
                            throw new Win32Exception(
                                Marshal.GetLastWin32Error(),
                                $"Querying process list failed");
                        }
                    }
                }
            }
        }

        public Task WaitForProcessesAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            if (!this.ProcessIds.Any())
            {
                return Task.CompletedTask;
            }

            //
            // NB. Job objects aren't signaled when all processes
            // have exited. We have to listen for a completion port
            // notification instead.
            //

            //
            // Create and associate a completion port while we're still
            // on the calling thread. That way, we ensure that by the time
            // the method returns, we're ready to receive notifications.
            //
            var completionPort = NativeMethods.CreateIoCompletionPort(
                NativeMethods.INVALID_HANDLE_VALUE,
                IntPtr.Zero,
                IntPtr.Zero,
                1);

            var associationInfo = new NativeMethods.JOBOBJECT_ASSOCIATE_COMPLETION_PORT()
            {
                CompletionKey = this.handle,
                CompletionPort = completionPort
            };

            if (!NativeMethods.SetInformationJobObject(
                this.handle,
                NativeMethods.JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation,
                ref associationInfo,
                (uint)Marshal.SizeOf<NativeMethods.JOBOBJECT_ASSOCIATE_COMPLETION_PORT>()))
            {
                completionPort.Dispose();
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    $"Quering job completion status failed");
            }

            //
            // There's no great way to await a completion port notification,
            // so we have to sacrifice a thread for it.
            //
            return Task.Factory.StartNew(() =>
            {
                using (completionPort)
                {
                    while (NativeMethods.GetQueuedCompletionStatus(
                        completionPort,
                        out var completionCode,
                        out var completionKey,
                        out var overlapped,
                        (uint)timeout.TotalSeconds))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                        else if (completionCode == NativeMethods.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO)
                        {
                            //
                            // Active process count has dropped to 0.
                            //
                            return;
                        }
                        else
                        {
                            //
                            // Keep going.
                            //
                        }
                    }

                    var lastError = Marshal.GetLastWin32Error();
                    if (lastError == NativeMethods.WAIT_TIMEOUT)
                    {
                        throw new TimeoutException();
                    }
                    else
                    {
                        throw new Win32Exception(
                            lastError,
                            $"Quering job completion status failed");
                    }
                }
            },
            TaskCreationOptions.LongRunning);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.Handle.Dispose();
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            internal const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
            internal const uint SYNCHRONIZE = 0x00100000;

            internal const int ERROR_NO_TOKEN = 1008;
            internal const int WAIT_TIMEOUT = 0x102;
            internal const uint JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO = 4;

            [StructLayout(LayoutKind.Sequential)]
            internal struct SECURITY_ATTRIBUTES
            {
                public int nLength;
                public IntPtr lpSecurityDescriptor;
                public bool bInheritHandle;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern SafeJobHandle CreateJobObject(
                [In] ref SECURITY_ATTRIBUTES lpJobAttributes,
                string? lpName);

            [DllImport("kernel32.dll")]
            internal static extern bool SetInformationJobObject(
                SafeJobHandle hJob,
                JOBOBJECTINFOCLASS infoClass,
                ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo,
                uint cbJobObjectInfoLength);

            [DllImport("kernel32.dll")]
            internal static extern bool SetInformationJobObject(
                SafeJobHandle hJob,
                JOBOBJECTINFOCLASS infoClass,
                ref JOBOBJECT_ASSOCIATE_COMPLETION_PORT lpJobObjectInfo,
                uint cbJobObjectInfoLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AssignProcessToJobObject(
                SafeJobHandle hJob,
                SafeProcessHandle hProcess);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsProcessInJob(
                SafeProcessHandle process,
                SafeJobHandle job, out bool result);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern SafeProcessHandle OpenProcess(
                uint processAccess,
                bool bInheritHandle,
                uint processId);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryInformationJobObject(
                SafeJobHandle hJob,
                JOBOBJECTINFOCLASS infoClass,
                IntPtr lpJobObjectInfo,
                uint cbJobObjectInfoLength,
                out uint lpReturnLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern SafeCompletionPortHandle CreateIoCompletionPort(
                IntPtr fileHandle,
                IntPtr existingCompletionPort,
                IntPtr completionKey,
                int numberOfConcurrentThreads);

            [DllImport("Kernel32.dll", SetLastError = true)]
            internal static extern bool GetQueuedCompletionStatus(
                SafeCompletionPortHandle completionPort,
                out int lpNumberOfBytesTransferred,
                out IntPtr lpCompletionKey,
                out IntPtr lpOverlapped,
                uint dwMilliseconds);

            internal enum JOB_OBJECT_LIMIT
            {
                KILL_ON_JOB_CLOSE = 0x00002000
            }

            internal enum JOBOBJECTINFOCLASS
            {
                JobObjectBasicProcessIdList = 3,
                JobObjectAssociateCompletionPortInformation = 7,
                JobObjectExtendedLimitInformation = 9
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                public long PerProcessUserTimeLimit;
                public long PerJobUserTimeLimit;
                public JOB_OBJECT_LIMIT LimitFlags;
                public UIntPtr MinimumWorkingSetSize;
                public UIntPtr MaximumWorkingSetSize;
                public uint ActiveProcessLimit;
                public UIntPtr Affinity;
                public uint PriorityClass;
                public uint SchedulingClass;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct IO_COUNTERS
            {
                public ulong ReadOperationCount;
                public ulong WriteOperationCount;
                public ulong OtherOperationCount;
                public ulong ReadTransferCount;
                public ulong WriteTransferCount;
                public ulong OtherTransferCount;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
                public IO_COUNTERS IoInfo;
                public UIntPtr ProcessMemoryLimit;
                public UIntPtr JobMemoryLimit;
                public UIntPtr PeakProcessMemoryUsed;
                public UIntPtr PeakJobMemoryUsed;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct JOBOBJECT_BASIC_PROCESS_ID_LIST
            {
                public uint NumberOfAssignedProcesses;
                public uint NumberOfProcessIdsInList;
                public UIntPtr ProcessIdListStart;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct JOBOBJECT_ASSOCIATE_COMPLETION_PORT
            {
                public SafeJobHandle CompletionKey;
                public SafeCompletionPortHandle CompletionPort;
            }
        }

        private class SafeJobHandle : SafeWin32Handle
        {
            public SafeJobHandle()
                : base(true)
            {
            }

            public SafeJobHandle(IntPtr handle, bool ownsHandle)
                : base(handle, ownsHandle)
            {
            }
        }

        private class SafeCompletionPortHandle : SafeWin32Handle
        {
            public SafeCompletionPortHandle()
                : base(true)
            {
            }

            public SafeCompletionPortHandle(IntPtr handle, bool ownsHandle)
                : base(handle, ownsHandle)
            {
            }
        }
    }
}
