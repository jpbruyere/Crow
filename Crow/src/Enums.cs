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
	public enum TextAlignment
    {
		Left,
		Right,
		Center,
		Justify
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
		arrow,
		base_arrow_down,
		base_arrow_up,
		boat,
		bottom_left_corner,
		bottom_right_corner,
		bottom_side,
		bottom_tee,
		center_ptr,
		circle,
		cross,
		cross_reverse,
		crosshair,
		dot,
		dot_box_mask,
		double_arrow,
		draft_large,
		draft_small,
		draped_box,
		exchange,
		fleur,
		gumby,
		hand,
		hand1,
		hand2,
		help,
		ibeam,
		left_ptr,
		left_ptr_watch,
		left_side,
		left_tee,
		ll_angle,
		lr_angle,
		move,
		pencil,
		pirate,
		plus,
		question_arrow,
		right_ptr,
		right_side,
		right_tee,
		sailboat,
		sb_down_arrow,
		sb_h_double_arrow,
		sb_left_arrow,
		sb_right_arrow,
		sb_up_arrow,
		sb_v_double_arrow,
		shuttle,
		sizing,
		target,
		tcross,
		top_left_arrow,
		top_left_corner,
		top_right_corner,
		top_side,
		top_tee,
		trek,
		ul_angle,
		ur_angle,
		watch,
		X_cursor,
		xterm,
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
