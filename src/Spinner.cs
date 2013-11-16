using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;

namespace go
{
    public class Spinner : HorizontalStack
    {
        Label Label;
        Button up;
        Button down;

        float _actualValue, _minValue, _maxValue, _increment;
        WidgetEvent _onValueChange;


        public float Value
        {
            get { return _actualValue; }
            set
            {
                _actualValue = value;
                Label.text = _actualValue.ToString();                
            }
        }

        public void Init(float _initialValue, float _min, float _max, float _step, WidgetEvent onValueChange, string _label = "")            
        {
            sizeToContent = true;

            if (!string.IsNullOrEmpty(_label))
            {
                Label = addChild(new Label(_label));
                Label.verticalAlignment = go.VerticalAlignment.None;
                Label.horizontalAlignment = go.HorizontalAlignment.None;

                Label.width = 100;
                Label.textAlignment = Alignment.LeftCenter;
            }

            Label = addChild(new TextBoxWidget(_initialValue.ToString()));
            Label.borderColor = Color.LightGray;
            Label.background = Color.DimWhite;
            Label.fontColor = Color.Black;
            Label.borderWidth = 1;
            Label.width = 50;
            Label.verticalAlignment = go.VerticalAlignment.None;
            Label.horizontalAlignment = go.HorizontalAlignment.None;
            Label.textAlignment = Alignment.RightCenter;



            VerticalStack arrows = new VerticalStack();
            arrows.widgetSpacing = 0;
            arrows.horizontalAlignment = go.HorizontalAlignment.Center;
            arrows.verticalAlignment = go.VerticalAlignment.Stretch;            
            arrows.width = 15;
            arrows.borderWidth = 0;
            arrows.sizeToContent = true;

            up = arrows.addChild(new Button(directories.rootDir + @"Images/Icons/spin_up.png", spinUp, false, 15, 10));
            up.borderWidth = 0;
            up.margin = 0;
            
            //up.alignment = Alignment.HorizontalStretch;


            down = arrows.addChild(new Button(directories.rootDir + @"Images/Icons/spin_down.png", spinDown, false, 15, 10));
            down.borderWidth = 0;
            down.margin = 0;
            //down.alignment = Alignment.HorizontalStretch;

            addChild(arrows);

            _actualValue = _initialValue;
            _minValue = _min;
            _maxValue = _max;
            _increment = Math.Abs(_step);
            _onValueChange = onValueChange;
        }

        public Spinner(float _initialValue, float _min, float _max, float _step, WidgetEvent onValueChange)
            : base()
        {
            Init(_initialValue, _min, _max, _step, onValueChange);
        }
        public Spinner(string _label, float _initialValue, float _min, float _max, float _step, WidgetEvent onValueChange)
            : base()
        {
            Init(_initialValue, _min, _max, _step, onValueChange,_label);
        }
        public void spinUp(Button sender)
        {
            _actualValue += _increment;
            if (_actualValue > _maxValue)
                _actualValue = _maxValue;

            Label.text = _actualValue.ToString();

            _onValueChange(this);
        }
        public void spinDown(Button sender)
        {
            _actualValue -= _increment;
            if (_actualValue < _minValue)
                _actualValue = _minValue;

            Label.text = _actualValue.ToString();

            _onValueChange(this);
        }

        public override void updateLayout()
        {
            base.updateLayout();
            up.height = Label.renderBounds.Height / 2;
            down.height = Label.renderBounds.Height / 2;
            down.y = Label.renderBounds.Top + Label.renderBounds.Height / 2; 
        }

        public override bool ProcessMousePosition(Point mousePos)
        {
            //if (base.ProcessMousePosition(mousePos))
            //    Debugger.Break();

            return base.ProcessMousePosition(mousePos);
        }
        public override void cairoDraw(ref Cairo.Context ctx, Rectangles clip = null)
        {
            base.cairoDraw(ref ctx, clip);
        }
    }

}
