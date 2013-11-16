using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;

namespace go
{
    public class HorizontalWrappingWidget : WrappedWidgetGroup
    {
        public HorizontalWrappingWidget(int _borderWidth = 0) :
            base()
        {
            Orientation = Orientation.Horizontal;
            borderWidth = _borderWidth;
            sizeToContent = true;
            background = Color.Transparent;
        }
        public HorizontalWrappingWidget(Color _borderColor, int _borderWidth = 1) :
            base()
        {
            Orientation = Orientation.Horizontal;
            borderWidth = _borderWidth;
            borderColor = _borderColor;
            background = Color.Transparent;
            sizeToContent = true;
        }
        public HorizontalWrappingWidget(Color _borderColor, Color _background, int _borderWidth = 1) :
            base()
        {
            Orientation = Orientation.Horizontal;
            borderWidth = _borderWidth;
            borderColor = _borderColor;
            background = _background;
            sizeToContent = true;
        }
    }
}
