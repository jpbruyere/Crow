using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;

namespace go
{
    public class VerticalWrappingWidget : WrappedWidgetGroup
    {
        public VerticalWrappingWidget(int _borderWidth = 0) :
            base()
        {
            Orientation = Orientation.Vertical;
            borderWidth = _borderWidth;
            sizeToContent = true;
            background = Color.Transparent;
        }
        public VerticalWrappingWidget(Color _borderColor, int _borderWidth = 1) :
            base()
        {
            Orientation = Orientation.Vertical;
            borderWidth = _borderWidth;
            borderColor = _borderColor;
            background = Color.Transparent;
            sizeToContent = true;
        }
        public VerticalWrappingWidget(Color _borderColor, Color _background, int _borderWidth = 1) :
            base()
        {
            Orientation = Orientation.Vertical;
            borderWidth = _borderWidth;
            borderColor = _borderColor;
            background = _background;
            sizeToContent = true;
        }

    }
}
