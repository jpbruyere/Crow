using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;

namespace go
{
    public class Slider : GraphicObject
    {
        double _actualValue, _minValue, _maxValue, _smallStep, _bigStep, unity;
        Rectangle cursor;
        bool holdCursor = false;

        public double value
        {
            get { return _actualValue; }
            set
            {
                if (value < _minValue)
                    _actualValue = _minValue;
                else if (value > _maxValue)
                    _actualValue = _maxValue;
                else                    
                    _actualValue = value;

                registerForGraphicUpdate();
            }
        }
        public Slider(double minimum, double maximum, double step)
            : base()
        {
            _minValue = minimum;
            _maxValue = maximum;
            _smallStep = step;
            _bigStep = step * 5;
            horizontalAlignment = HorizontalAlignment.None;
            verticalAlignment = VerticalAlignment.None;
            focusable = true;
        }

        void computeCursorPosition()
        {
            cursor = new Rectangle(new Size(4, 10));
            Rectangle r = clientBounds;
            PointD p1 = r.TopLeft + new Point(0, r.Height / 2);

            unity = (double)r.Width / (_maxValue - _minValue);

            cursor.TopLeft = new Point((int)(_actualValue * unity - cursor.Width / 2),
                                        (int)(p1.Y - cursor.Height / 2));
             
        }
        public override void ProcessMouseDown(Point mousePos)
        {
            Interface.activeWidget = this;

            base.ProcessMouseDown(mousePos);
            

            Rectangle cursInScreenCoord = rectInScreenCoord(cursor);
            if (cursInScreenCoord.Contains(mousePos))
            {
                holdCursor = true;
            }
            else if (mousePos.X < cursInScreenCoord.Left)
            {
                value -= _bigStep;
            }
            else
            {
                value += _bigStep;
            }

        }
        public override void ProcessMouseUp(Point mousePos)
        {
            holdCursor = false;
            base.ProcessMouseUp(mousePos);
        }
        public override bool ProcessMousePosition(Point mousePos)
        {
            return base.ProcessMousePosition(mousePos);
        }
        internal override void updateGraphic()
        {
            int stride = 4 * renderBounds.Width;


            //init  bmp with widget background and border
            base.updateGraphic();
            computeCursorPosition();

            using (ImageSurface bitmap =
                new ImageSurface(bmp, Format.Argb32, renderBounds.Width, renderBounds.Height, stride))
            {
                using (Context gr = new Context(bitmap))
                {
                    gr.FontOptions.Antialias = Antialias.Subpixel;

                    gr.Color = foreground;

                    Rectangle r = clientBounds.Clone;
                    PointD p1 = r.TopLeft + new Point(0, r.Height / 2);
                    PointD p2 = r.TopRight + new Point(0, r.Height / 2);

                    gr.LineWidth = 1;
                    gr.MoveTo(p1);
                    gr.LineTo(p2);

                    gr.Stroke();
                    gr.LineWidth = 0.5;
                    
                    double sst = unity * _smallStep;
                    double bst = unity * _bigStep;


                    PointD vBar = new PointD(0, sst);
                    for (double x = _minValue; x <= _maxValue - _minValue; x += _smallStep)
                    {
                        double lineLength = 2.5;
                        if (x % _bigStep == 0)
                            lineLength *= 2;
                        PointD p = new PointD(p1.X + x * unity, p1.Y);
                        gr.MoveTo(p);
                        gr.LineTo(new PointD(p.X, p.Y + lineLength));
                    }

                    gr.Stroke();

                    
                    gr.Rectangle(cursor);
                    gr.Fill();


                }

                bitmap.Flush();
                //bitmap.WriteToPng(@"d:\test.png");
            }
        }
    }
}
