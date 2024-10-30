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

using Google.Solutions.Mvvm.Shell;
using NUnit.Framework;
using System.IO;
using System.Text;
using System.Threading;

namespace Google.Solutions.Mvvm.Test.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestFileDataObject
    {
        [Test]
        public void GetData()
        {
            var content = Encoding.ASCII.GetBytes("Test");
            using (var contentStream = new MemoryStream())
            {
                contentStream.Write(content, 0, content.Length);

                var dataObject = new FileDataObject(new[] {
                    new FileDataObject.Descriptor(
                        "file-1.txt",
                        (ulong)content.Length,
                        FileAttributes.Normal,
                        contentStream),
                    new FileDataObject.Descriptor(
                        "file-2.txt",
                        (ulong)content.Length,
                        FileAttributes.Normal,
                        contentStream),
                });

                Assert.IsInstanceOf<Stream>(
                    dataObject.GetData(FileDataObject.CFSTR_FILEDESCRIPTORW, false));

                Assert.IsInstanceOf<Stream>(
                    dataObject.GetData(FileDataObject.CFSTR_FILECONTENTS, false));
            }
        }
    }
}
