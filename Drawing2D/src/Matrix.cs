// Copyright (c) 2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Drawing2D {
	public struct Matrix {
		float xx; float yx;
		float xy; float yy;
		float x0; float y0;

		public float XX, YX;
		public float XY, YY;
		public float X0, Y0;

		public override string ToString () {
			return string.Format ($"({xx};{yx};{xy};{yy};{x0};{y0})");
		}
	}
}
