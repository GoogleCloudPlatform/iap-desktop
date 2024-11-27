//
// Copyright 2022 Google LLC
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

using Google.Solutions.Mvvm.Drawing;
using Google.Solutions.Mvvm.Properties;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Drawing
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public partial class TestIconInverter : Form
    {
        //---------------------------------------------------------------------
        // UI tests.
        //---------------------------------------------------------------------

        private string? fileName;

        public TestIconInverter()
        {
            InitializeComponent();

            this.colorFactorScale.ValueChanged += (_, __) => Apply();
            this.grayFactorScale.ValueChanged += (_, __) => Apply();
            this.invertGraysCheckBox.CheckStateChanged += (_, __) => Apply();
        }

        [RequiresInteraction]
        [Test]
        public void InvertIcon()
        {
            ShowDialog();
        }

        private void browseButton_Click(object sender, System.EventArgs e)
        {
            using (var dialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                DefaultExt = ".png",
                Multiselect = false,
                Title = "Select icon"
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    this.fileName = dialog.FileName;
                }
            }

            Apply();
        }

        private void Apply()
        {
            if (this.fileName == null)
            {
                return;
            }

            var lightImage = (Bitmap)Image.FromFile(this.fileName);
            var darkImage = (Bitmap)lightImage.Clone();
            var inverter = new IconInverter()
            {
                ColorFactor = ((float)this.colorFactorScale.Value) / 100,
                GrayFactor = ((float)this.grayFactorScale.Value) / 100,
                InvertGray = this.invertGraysCheckBox.Checked
            };

            this.grayFactorValue.Text = inverter.GrayFactor.ToString();
            this.colorFactorValue.Text = inverter.ColorFactor.ToString();

            inverter.Invert(darkImage);

            this.smallLightIcon.Image = lightImage;
            this.mediumLightIcon.Image = lightImage;
            this.largeLightIcon.Image = lightImage;

            this.smallDarkIcon.Image = darkImage;
            this.mediumDarkIcon.Image = darkImage;
            this.largeDarkIcon.Image = darkImage;
        }

        //---------------------------------------------------------------------
        // Unit tests.
        //---------------------------------------------------------------------

        [Test]
        public void Invert_WhenImageInvertedBefore_ThenInvertReturnsFalse()
        {
            var icon = Resources.Copy_16x;
            var inverter = new IconInverter()
            {
                GrayFactor = .8f,
                ColorFactor = .8f
            };

            Assert.IsTrue(inverter.Invert(icon));
            Assert.IsFalse(inverter.Invert(icon));
        }

        [Test]
        public void Invert_WhenImageListInvertedBefore_ThenInvertReturnsFalse()
        {
            var icon = Resources.Copy_16x;
            using (var imageList = new ImageList())
            {
                imageList.Images.Add(icon);
                var inverter = new IconInverter()
                {
                    GrayFactor = .8f,
                    ColorFactor = .8f
                };

                Assert.IsTrue(inverter.Invert(imageList));
                Assert.IsFalse(inverter.Invert(imageList));
            }
        }
    }
}
