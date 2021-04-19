// Copyright (c) 2018-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace vkvg
{
    public enum Status
    {
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

    public enum Direction
    {
        Horizontal = 0,
        Vertical = 1
    }

    public enum Format
    {
        ARGB32,
        RGB24,
        A8,
        A1
    }

    public enum Extend
    {
        None,
        Repeat,
        Reflect,
        Pad
    }

    public enum Filter
    {
        Fast,
        Good,
        Best,
        Nearest,
        Bilinear,
        Gaussian,
    }

    public enum PatternType
    {
        Solid,
        Surface,
        Linear,
        Radial,
        Mesh,
        RasterSource,
    }

    public enum Operator
    {
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

    public enum FontSlant
    {
        Normal,
        Italic,
        Oblique
    }
    public enum FontWeight
    {
        Normal,
        Bold,
    }

    public enum SampleCount
    {
        Sample_1 = 0x00000001,
        Sample_2 = 0x00000002,
        Sample_4 = 0x00000004,
        Sample_8 = 0x00000008,
        Sample_16 = 0x00000010,
        Sample_32 = 0x00000020,
        Sample_64 = 0x00000040
    }

    public enum LineCap
    {
        Butt,
        Round,
        Square
    }

    public enum LineJoin
    {
        Miter,
        Round,
        Bevel
    }
    public enum FillRule
    {
        EvenOdd,
        NonZero,
    }
	public enum Antialias
	{
		Default,
		None,
		Grey,
		Subpixel,
	}
}