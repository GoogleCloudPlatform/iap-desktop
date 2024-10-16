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
    public class TestVirtualTerminalBinding
    {
        //---------------------------------------------------------------------
        // OnTerminalDisposed.
        //---------------------------------------------------------------------

        [Test]
        public void OnTerminalDisposed_WhenTerminalDisposed_DisposesDevice()
        {
            var device = new Mock<IPseudoTerminal>();
            var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            };

            virtualTerminal.Dispose();

            device.Verify(d => d.Dispose(), Times.Once);
        }

        //---------------------------------------------------------------------
        // OnTerminalUserInput.
        //---------------------------------------------------------------------

        [Test]
        public void OnTerminalUserInput_WritesToDevice()
        {
            var device = new Mock<IPseudoTerminal>();
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
        public void OnTerminalUserInput_WhenDeviceIsClosed_ThenDoesNotWriteToDevice()
        {
            var device = new Mock<IPseudoTerminal>();
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
        public void OnTerminalUserInput_WhenDeviceFails_ThenRaisesEvent()
        {
            var device = new Mock<IPseudoTerminal>();
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
                Exception? exception = null;
                virtualTerminal.DeviceError += (_, args) => exception = args.Exception;

                virtualTerminal.SimulateSend("data");

                Assert.IsNotNull(exception);
                Assert.IsInstanceOf<ArgumentException>(exception);
            }
        }

        //---------------------------------------------------------------------
        // OnTerminalDimensionsChanged.
        //---------------------------------------------------------------------

        [Test]
        public void OnTerminalDimensionsChanged_ResizesDevice()
        {
            var device = new Mock<IPseudoTerminal>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                virtualTerminal.Dimensions = new PseudoTerminalSize(81, 25);
            }

            device.Verify(d => d.ResizeAsync(
                    It.Is<PseudoTerminalSize>(s => s.Width == 81),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void OnTerminalDimensionsChanged_WhenDeviceIsClosed_ThenDoesNotResizeDevice()
        {
            var device = new Mock<IPseudoTerminal>();
            device.SetupGet(d => d.IsClosed).Returns(true);

            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                virtualTerminal.Dimensions = new PseudoTerminalSize(81, 25);
            }

            device.Verify(d => d.ResizeAsync(
                    It.IsAny<PseudoTerminalSize>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void OnTerminalDimensionsChanged_WhenDeviceResizeFails_ThenRaisesEvent()
        {
            var device = new Mock<IPseudoTerminal>();
            device
                .Setup(d => d.ResizeAsync(
                    It.IsAny<PseudoTerminalSize>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock"));

            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                Exception? exception = null;
                virtualTerminal.DeviceError += (_, args) => exception = args.Exception;

                virtualTerminal.Dimensions = new PseudoTerminalSize(81, 25);

                Assert.IsNotNull(exception);
                Assert.IsInstanceOf<ArgumentException>(exception);
            }
        }

        //---------------------------------------------------------------------
        // OnDeviceError.
        //---------------------------------------------------------------------

        [Test]
        public void OnDeviceError_RaisesEvent()
        {
            var device = new Mock<IPseudoTerminal>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                Exception? exception = null;
                virtualTerminal.DeviceError += (_, args) => exception = args.Exception;

                device.Raise(
                    d => d.FatalError += null,
                    new PseudoTerminalErrorEventArgs(new ArgumentException()));

                Assert.IsNotNull(exception);
                Assert.IsInstanceOf<ArgumentException>(exception);
            }
        }

        //---------------------------------------------------------------------
        // OnDeviceDisconnected.
        //---------------------------------------------------------------------

        [Test]
        public void OnDeviceDisconnected_ClosesDevice()
        {
            var device = new Mock<IPseudoTerminal>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                var deviceClosedEventRaised = false;
                virtualTerminal.DeviceClosed += (_, args) => deviceClosedEventRaised = true;

                device.Raise(d => d.Disconnected += null, EventArgs.Empty);

                Assert.IsTrue(deviceClosedEventRaised);
            }
        }

        //---------------------------------------------------------------------
        // OnDeviceOutput.
        //---------------------------------------------------------------------

        [Test]
        public void OnDeviceOutput_RaisesEvent()
        {
            var device = new Mock<IPseudoTerminal>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                var data = string.Empty;
                virtualTerminal.Output += (_, args) => data += args.Data;

                device.Raise(d => d.OutputAvailable += null, new PseudoTerminalDataEventArgs("data"));

                Assert.AreEqual("data", data);
            }
        }

        [Test]
        public void OnDeviceOutput_WhenDeviceReportsEof_ThenRaisesEvent()
        {
            var device = new Mock<IPseudoTerminal>();
            using (var virtualTerminal = new VirtualTerminal()
            {
                Device = device.Object,
            })
            {
                var deviceClosed = false;
                virtualTerminal.DeviceClosed += (_, args) => deviceClosed = true;

                device.Raise(d => d.OutputAvailable += null, PseudoTerminalDataEventArgs.Eof);

                Assert.IsTrue(deviceClosed);
            }
        }
    }
}
