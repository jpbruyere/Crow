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
        Top,
        Left,
        Right,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
		Center
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
