//
//  Extensions.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using OpenTK;
using Crow;

namespace Crow
{
	public static partial class Extensions {
		public static Vector4 ToVector4(this Color c){
			float[] f = c.floatArray;
			return new Vector4 (f [0], f [1], f [2], f [3]);
		}
		public static Vector3 Transform(this Vector3 v, Matrix4 m){
			return Vector4.Transform(new Vector4(v, 1), m).Xyz;
		}
		public static bool IsInBetween(this int v, int min, int max){
			return v >= min & v <= max;
		}

	}
}

