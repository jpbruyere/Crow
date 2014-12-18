using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
{
    public enum Orientation
    {
        Horizontal,
        Vertical
    }
    public enum Alignment
    {
        None,
        TopCenter,
        TopStretch,
        LeftCenter,
        LeftStretch,
        RightCenter,
        RightStretch,
        BottomCenter,
        BottomStretch,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center,
        Fit,
        HorizontalStretch,
        VerticalStretch
    }

    public enum PanelBorderPosition
    {
        Top,
        Left,
        Right,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Moving,
        Closing,
        ClientArea
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
