// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

namespace Crow
{
	public enum Orientation
    {
        Horizontal,
        Vertical
    }

	public enum Alignment 
    {
        Top = 0x01,
        Left = 0x02,
		TopLeft = 0x03,
		Right = 0x04,
		TopRight = 0x05,
		Bottom = 0x08,
        BottomLeft = 0x0a,
        BottomRight = 0x0c,
		Center = 0x10,
		Undefined = 0x40
    }
    public enum HorizontalAlignment
    {
        Left,
        Right,
        Center,
    }
    public enum VerticalAlignment
    {
        Top,
        Bottom,
        Center,
    }
	public enum MouseCursor
	{
		Arrow,
		IBeam,
		Crosshair,
		Circle,
		Hand,
		Move,
		Wait,
		H,
		V,
		Top,
		TopLeft,
		TopRight,
		Left,
		Right,
		BottomLeft,
		Bottom,
		BottomRight,
		NW,
		NE,
		SW,
		SE,
	}
	/// <summary>
	/// Cursor shape use in Sliders
	/// </summary>
	public enum CursorType
	{
		/// <summary>Only Background of cursor will be drawm, you may use a bmp, svg, or shape as background for custom shape.</summary>
		None,
		Rectangle,
		Circle,
		Pentagone
	}
	/// <summary>
	/// Color component used in color widgets
	/// </summary>
	public enum ColorComponent
	{
		Red,
		Green,
		Blue,
		Alpha,
		Hue,
		Saturation,
		Value
	}
}
