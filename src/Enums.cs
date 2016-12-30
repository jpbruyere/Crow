using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		Center = 0x10
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

    public enum RectanglesRelations
    {
        NoRelation, //nothing to do
        Intersect,  //clipped clear & repaint, no test
        Contains,   //clipped clear & repaint, test children
        Equal       //repaint, no test
    }
}
