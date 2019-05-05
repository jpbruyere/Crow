//
// Matrix.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2019 jp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
namespace vkvg {
	public struct Matrix {
		float xx; float yx;
		float xy; float yy;
		float x0; float y0;

		public float XX { get { return xx; } set { xx = value; } }
		public float YX { get { return yx; } set { yx = value; } }
		public float XY { get { return xy; } set { xy = value; } }
		public float YY { get { return yy; } set { yy = value; } }
		public float X0 { get { return x0; } set { x0 = value; } }
		public float Y0 { get { return y0; } set { y0 = value; } }

		public static Matrix Create (float xx, float yx, float xy, float yy, float x0, float y0) {
			Matrix tmp;
			NativeMethods.vkvg_matrix_init (out tmp, xx, yx, xy, yy, x0, y0);
			return tmp;
		}
		public static Matrix CreateTranslation (float tx, float ty) {
			Matrix tmp;
			NativeMethods.vkvg_matrix_init_translate (out tmp, tx, ty);
			return tmp;
		}
		public static Matrix CreateRotation (float radian) {
			Matrix tmp;
			NativeMethods.vkvg_matrix_init_rotate (out tmp, radian);
			return tmp;
		}
		public static Matrix CreateScale (float sx, float sy) {
			Matrix tmp;
			NativeMethods.vkvg_matrix_init_scale (out tmp, sx, sy);
			return tmp;
		}
		public static Matrix Identity {
			get {
				Matrix tmp;
				NativeMethods.vkvg_matrix_init_identity (out tmp);
				return tmp;
			}
		}

		public void Translate (float tx, float ty) {
			Matrix tmp = this;
			NativeMethods.vkvg_matrix_translate (ref tmp, tx, ty);
			xx = tmp.xx; yx = tmp.yx;
			xy = tmp.xy; yy = tmp.yy;
			x0 = tmp.x0; y0 = tmp.y0;
		}
		public void Rotate (float radian) {
			Matrix tmp = this;
			NativeMethods.vkvg_matrix_rotate (ref tmp, radian);
			xx = tmp.xx; yx = tmp.yx;
			xy = tmp.xy; yy = tmp.yy;
			x0 = tmp.x0; y0 = tmp.y0;
		}
		public void Scale (float sx, float sy) {
			Matrix tmp = this;
			NativeMethods.vkvg_matrix_scale (ref tmp, sx, sy);
			xx = tmp.xx; yx = tmp.yx;
			xy = tmp.xy; yy = tmp.yy;
			x0 = tmp.x0; y0 = tmp.y0;
		}
		public void Invert () {
			Matrix tmp = this;
			NativeMethods.vkvg_matrix_invert (ref tmp);
			xx = tmp.xx; yx = tmp.yx;
			xy = tmp.xy; yy = tmp.yy;
			x0 = tmp.x0; y0 = tmp.y0;
		}
		public void TransformDistance (ref float dx, ref float dy) {
			NativeMethods.vkvg_matrix_transform_distance (ref this, ref dx, ref dy);
		}
		public void TransformPoint (ref float px, ref float py) {
			NativeMethods.vkvg_matrix_transform_distance (ref this, ref px, ref py);
		}

		public static Matrix operator *(Matrix a, Matrix b) {
			Matrix tmp;
			NativeMethods.vkvg_matrix_multiply (out tmp, ref a, ref b);
			return tmp;
		}

		public override string ToString () {
			return string.Format ($"({xx};{yx};{xy};{yy};{x0};{y0})");
		}
	}
}
