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

using Google.Solutions.Common.Util;
using System;
using System.Drawing;

namespace Google.Solutions.Mvvm.Drawing
{
    /// <summary>
    /// HSL (Hue, Saturation, Luminosity) color.
    /// Inspired by https://stackoverflow.com/a/29316972/4372
    /// </summary>
    public struct HslColor
    {
        public float H;
        public float S;
        public float L;

        private static int ToBase256(float v)
        {
            return (int)Math.Min(255, 256 * v);
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f)
            {
                t += 1f;
            }

            if (t > 1f)
            {
                t -= 1f;
            }

            if (t < 1f / 6f)
            {
                return p + (q - p) * 6f * t;
            }

            if (t < 1f / 2f)
            {
                return q;
            }

            if (t < 2f / 3f)
            {
                return p + (q - p) * (2f / 3f - t) * 6f;
            }

            return p;
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public HslColor(float h, float s, float l)
        {
            this.H = h.ExpectInRange(0.0f, 1.0f, nameof(h));
            this.S = s.ExpectInRange(0.0f, 1.0f, nameof(s));
            this.L = l.ExpectInRange(0.0f, 1.0f, nameof(l));
        }

        public static HslColor FromColor(Color color)
        {
            return FromRgb(color.R, color.G, color.B);
        }

        public static HslColor FromRgb(int pR, int pG, int pB)
        {
            var r = pR / 255f;
            var g = pG / 255f;
            var b = pB / 255f;

            var max = (r > g && r > b) ? r : (g > b) ? g : b;
            var min = (r < g && r < b) ? r : (g < b) ? g : b;

            float h, s, l;
            l = (max + min) / 2.0f;

            if (max == min)
            {
                h = s = 0.0f;
            }
            else
            {
                var d = max - min;
                s = (l > 0.5f) ? d / (2.0f - max - min) : d / (max + min);

                if (r > g && r > b)
                {
                    h = (g - b) / d + (g < b ? 6.0f : 0.0f);
                }
                else if (g > b)
                {
                    h = (b - r) / d + 2.0f;
                }
                else
                {
                    h = (r - g) / d + 4.0f;
                }

                h /= 6.0f;
            }

            return new HslColor(h, s, l);
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public readonly Color ToColor()
        {
            float r, g, b;

            if (this.S == 0f)
            {
                r = g = b = this.L; // achromatic
            }
            else
            {
                var q = this.L < 0.5f
                    ? this.L * (1 + this.S)
                    : this.L + this.S - this.L * this.S;

                var p = 2 * this.L - q;
                r = HueToRgb(p, q, this.H + 1f / 3f);
                g = HueToRgb(p, q, this.H);
                b = HueToRgb(p, q, this.H - 1f / 3f);
            }

            return Color.FromArgb(ToBase256(r), ToBase256(g), ToBase256(b));
        }


        //---------------------------------------------------------------------
        // Operators and equality.
        //---------------------------------------------------------------------

        public readonly override int GetHashCode()
        {
            return ToBase256(this.H) << 16 |
                   ToBase256(this.S) << 8 |
                   ToBase256(this.L);
        }

        public readonly override bool Equals(object obj)
        {
            return obj is HslColor hsl &&
                hsl.H == this.H &&
                hsl.S == this.S &&
                hsl.L == this.L;
        }

        public readonly override string ToString()
        {
            return $"H={ToBase256(this.H)}, S={ToBase256(this.S)}, L={ToBase256(this.L)}";
        }

        public static bool operator ==(HslColor left, HslColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HslColor left, HslColor right)
        {
            return !(left == right);
        }
    }
}
