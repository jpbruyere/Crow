//
// Enums.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
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
	public enum Status {
		Success = 0,
		NoMemory,
		InvalidRestore,
		InvalidPopGroup,
		NoCurrentPoint,
		InvalidMatrix,
		InvalidStatus,
		NullPointer,
		InvalidString,
		InvalidPathData,
		ReadError,
		WriteError,
		SurfaceFinished,
		SurfaceTypeMismatch,
		PatternTypeMismatch,
		InvalidContent,
		InvalidFormat,
		InvalidVisual,
		FileNotFound,
		InvalidDash
	}

	public enum Direction {
		Horizontal = 0,
		Vertical = 1
	}

	public enum Format {
		ARGB32,
		RGB24,
		A8,
		A1
	}

	public enum Extend {
		None,
		Repeat,
		Reflect,
		Pad
	}

	public enum Filter {
		Fast,
		Good,
		Best,
		Nearest,
		Bilinear,
		Gaussian,
	}

	public enum PatternType {
		Solid,
		Surface,
		Linear,
		Radial,
		Mesh,
		RasterSource,
	}

	public enum Operator {
		Clear,
		Source,
		Over,
		In,
		Out,
		Atop,

		Dest,
		DestOver,
		DestIn,
		DestOut,
		DestAtop,

		Xor,
		Add,
		Saturate,
	}

	public enum FontSlant {
		Normal,
		Italic,
		Oblique
	}
	public enum FontWeight {
		Normal,
		Bold,
	}

	public enum SampleCount {
		Sample_1 = 0x00000001,
		Sample_2 = 0x00000002,
		Sample_4 = 0x00000004,
		Sample_8 = 0x00000008,
		Sample_16 = 0x00000010,
		Sample_32 = 0x00000020,
		Sample_64 = 0x00000040
	}

	public enum LineCap {
		Butt,
		Round,
		Square
	}

	public enum LineJoin {
		Miter,
		Round,
		Bevel
	}
	public enum FillRule {
		EvenOdd,
		NonZero,
	}
}