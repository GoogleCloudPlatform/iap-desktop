//
// Copyright 2024 Google LLC
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

using Google.Solutions.Platform.IO;
using Google.Solutions.Terminal.Controls;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    public class TestTerminalDeviceBinding
    {
        //---------------------------------------------------------------------
        // OnTerminalDisposed.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTerminalDisposed_ThenDeviceIsDisposed()
        {
            var device = new Mock<IPseudoConsole>();
            var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            };

            virtualTerminal.Dispose();

            device.Verify(d => d.Dispose(), Times.Once);
        }

        //---------------------------------------------------------------------
        // OnTerminalDataSent.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTerminalSentData_ThenDataIsWrittenToDevice()
        {
            var device = new Mock<IPseudoConsole>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                virtualTerminal.SimulateSend("data");
            }

            device.Verify(d => d.WriteAsync(
                    It.Is<string>(s => s == "data"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void WhenTerminalSentDataButDeviceIsClosed_ThenNoDataIsWrittenToDevice()
        {
            var device = new Mock<IPseudoConsole>();
            device.SetupGet(d => d.IsClosed).Returns(true);

            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                virtualTerminal.SimulateSend("data");
            }

            device.Verify(d => d.WriteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void WhenDeviceWriteFails_ThenTerminalEventIsRaised()
        {
            var device = new Mock<IPseudoConsole>();
            device
                .Setup(d => d.WriteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock"));

            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                Exception exception = null;
                virtualTerminal.DeviceError += (_, args) => exception = args.Exception;

                virtualTerminal.SimulateSend("data");

                Assert.IsNotNull(exception);
                Assert.IsInstanceOf<ArgumentException>(exception);
            }
        }

        //---------------------------------------------------------------------
        // OnPseudoConsoleSizeChanged.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTerminaDimensionsChanged_ThenDeviceIsResized()
        {
            var device = new Mock<IPseudoConsole>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                virtualTerminal.Dimensions = new PseudoConsoleSize(81, 25);
            }

            device.Verify(d => d.ResizeAsync(
                    It.Is<PseudoConsoleSize>(s => s.Width == 81),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void WhenTerminaDimensionsChangedButDeviceIsClosed_ThenDeviceIsNotResized()
        {
            var device = new Mock<IPseudoConsole>();
            device.SetupGet(d => d.IsClosed).Returns(true);

            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                virtualTerminal.Dimensions = new PseudoConsoleSize(81, 25);
            }

            device.Verify(d => d.ResizeAsync(
                    It.IsAny<PseudoConsoleSize>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void WhenDeviceResizeFails_ThenTerminalEventIsRaised()
        {
            var device = new Mock<IPseudoConsole>();
            device
                .Setup(d => d.ResizeAsync(
                    It.IsAny<PseudoConsoleSize>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock"));

            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                Exception exception = null;
                virtualTerminal.DeviceError += (_, args) => exception = args.Exception;

                virtualTerminal.Dimensions = new PseudoConsoleSize(81, 25);

                Assert.IsNotNull(exception);
                Assert.IsInstanceOf<ArgumentException>(exception);
            }
        }

        //---------------------------------------------------------------------
        // OnDeviceError.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDeviceReportsError_ThenTerminalEventIsRaised()
        {
            var device = new Mock<IPseudoConsole>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                Exception? exception = null;
                virtualTerminal.DeviceError += (_, args) => exception = args.Exception;

                device.Raise(d => d.FatalError += null, new PseudoConsoleErrorEventArgs(new ArgumentException()));

                Assert.IsNotNull(exception);
                Assert.IsInstanceOf<ArgumentException>(exception);
            }
        }

        //---------------------------------------------------------------------
        // OnDeviceDisconnected.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDeviceDisconnects_ThenTerminalIsClosed()
        {
            var device = new Mock<IPseudoConsole>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                bool deviceClosedEventRaised = false;
                virtualTerminal.DeviceClosed += (_, args) => deviceClosedEventRaised = true;

                device.Raise(d => d.Disconnected += null, EventArgs.Empty);

                Assert.IsTrue(deviceClosedEventRaised);
            }
        }

        //---------------------------------------------------------------------
        // OnDeviceOutput.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDeviceReportsOutput_ThenTerminalEventIsRaised()
        {
            var device = new Mock<IPseudoConsole>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                string data = string.Empty;
                virtualTerminal.Output += (_, args) => data += args.Data;

                device.Raise(d => d.OutputAvailable += null, new PseudoConsoleDataEventArgs("data"));

                Assert.AreEqual("data", data);
            }
        }

        [Test]
        public void WhenDeviceReportsEof_ThenTerminalEventIsRaised()
        {
            var device = new Mock<IPseudoConsole>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                var deviceClosed = false;
                virtualTerminal.DeviceClosed += (_, args) => deviceClosed = true;

                device.Raise(d => d.OutputAvailable += null, PseudoConsoleDataEventArgs.Eof);

                Assert.IsTrue(deviceClosed);
            }
        }
    }
}
