using System;


using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OpenTK.Graphics.OpenGL;

using Cairo;

using winColors = System.Drawing.Color;
using System.Diagnostics;

namespace go
{
    public class Button : Group
    {
        public static class Theme
        {
            public static Color background = Color.Gray;
            public static Color borderColor = Color.Black;
            public static int margin = 1;
            public static int borderWidth = 1;
            public static VerticalAlignment verticalAlignment = VerticalAlignment.None;
            public static HorizontalAlignment horizontalAlignment = HorizontalAlignment.None;
            public static bool sizeToContent = false;
        }

        public Button(ButtonWidgetClick _clickEvent, string _text, bool _checkable = false, int width = 0, int height = 0)
            : base(width, height)
        {
            initButtonWidget(_clickEvent, _checkable);
            label = new Label(_text);
            sizeToContent = true;
            label.fontColor = Color.Black;
            label.fontSize = 14;
            label.horizontalAlignment = HorizontalAlignment.Stretch;
            label.verticalAlignment = VerticalAlignment.Stretch;
            label.textAlignment = Alignment.Center;

        }
        public Button(System.Drawing.Bitmap iconBitmap, ButtonWidgetClick _clickEvent, bool _checkable, int width, int height)
            : base(width, height)
        {
            icon = new Image(iconBitmap);
            initButtonWidget(_clickEvent, _checkable);
        }
        public Button(string iconFile, ButtonWidgetClick _clickEvent = null, bool _checkable = false, bool _affectMultiSelectState = true)
            : base()
        {
            affectMultiSelectState = _affectMultiSelectState;
            icon = new Image(iconFile);
            initButtonWidget(_clickEvent, _checkable);
            sizeToContent = true;
        }
        public Button(string iconFile, ButtonWidgetClick _clickEvent, bool _checkable, int width, int height, bool _affectMultiSelectState = true)
            : base(width, height)
        {
            affectMultiSelectState = _affectMultiSelectState;
            icon = new Image(iconFile);
            initButtonWidget(_clickEvent, _checkable);
        }
        public Button(string iconFile, string checkedIconFile, ButtonWidgetClick _clickEvent, bool _checkable = false, int width = 30, int height = 30, bool _affectMultiSelectState = true)
            : base(width, height)
        {
            affectMultiSelectState = _affectMultiSelectState;
            icon = new Image(iconFile);
            checkedIcon = new Image(checkedIconFile);
            checkedIcon.isVisible = false;

            initButtonWidget(_clickEvent, _checkable);
        }

        void initButtonWidget(ButtonWidgetClick _clickEvent = null, bool _checkable = false)
        {
            focusable = true;
            IsCheckable = _checkable;
            clickEvent = _clickEvent;

            background = Theme.background;
            borderColor = Theme.borderColor;
            borderWidth = Theme.borderWidth;
            margin = Theme.margin;
            horizontalAlignment = Theme.horizontalAlignment;
            verticalAlignment = Theme.verticalAlignment;
            sizeToContent = Theme.sizeToContent;
        }

        public enum ButtonStates
        {
            normal,
            mouseOver,
            mouseDown,
            Disable
        }

        public static int maxIconSize = 22;

        ButtonStates _CurrentState = ButtonStates.normal;
        public ButtonStates CurrentState
        {
            get { return _CurrentState; }
            set
            {
                if (value == _CurrentState)
                    return;
                _CurrentState = value;
                registerForRedraw();
                //registerForGraphicUpdate();
                //needGraphicalUpdate = true;
            }
        }

        public ButtonWidgetClick clickEvent;

        bool _isChecked = false;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (value == _isChecked)
                    return;

                _isChecked = value;

                if (clickEvent != null)
                    clickEvent(this);

                registerForGraphicUpdate();
            }
        }

        public bool IsCheckable = false;
        public bool affectMultiSelectState = true;
        public bool border3D = false;

        Image _icon;
        Image _checkedIcon;
        Label _label;

        public Image icon
        {
            get { return _icon; }
            set
            {
                if (_icon != null)
                    removeChild(_icon);

                if (value == null)
                    _icon = null;
                else
                {
                    _icon = addChild(value) as Image;
                    _icon.horizontalAlignment = go.HorizontalAlignment.Stretch;
                    _icon.verticalAlignment = go.VerticalAlignment.Stretch;
                    _icon.background = Color.Transparent;
                    putWidgetOnBottom(_icon);
                    //_icon.borderWidth = 5;
                }
            }
        }
        public Image checkedIcon
        {
            get { return _checkedIcon; }
            set
            {
                if (_checkedIcon != null)
                    removeChild(_checkedIcon);

                if (value == null)
                    _checkedIcon = null;
                else
                {
                    _checkedIcon = addChild(value) as Image;
                    _checkedIcon.horizontalAlignment = go.HorizontalAlignment.Stretch;
                    _checkedIcon.verticalAlignment = go.VerticalAlignment.Stretch;
                    _checkedIcon.background = Color.Transparent;

                    putWidgetOnBottom(_checkedIcon);
                }
            }
        }
        public Label label
        {
            get { return _label; }
            set
            {
                if (_label != null)
                    removeChild(_label);

                if (value == null)
                    _label = null;
                else
                {
                    _label = addChild(value) as Label;
                    _label.horizontalAlignment = go.HorizontalAlignment.Stretch;
                    _label.verticalAlignment = go.VerticalAlignment.Stretch;
                    _label.textAlignment = Alignment.VerticalStretch;
                    putWidgetOnTop(_label);
                }
            }
        }

        public string text
        {
            get { return label.text; }
            set
            {
                label = new Label(value);
            }
        }

        //public ButtonWidget(string iconFile, int _x, int _y, bool _checkable = false):base()
        //{
        //    IsCheckable = _checkable;
        //    x = _x;
        //    y = _y;

        //    icon = new ImageWidget(iconFile);
        //    icon.Parent = this;
        //}



        public override bool ProcessMousePosition(Point mousePos)
        {
            if (CurrentState == ButtonStates.Disable)
                return false;

            bool result = base.ProcessMousePosition(mousePos);

            if (result && CurrentState != ButtonStates.mouseDown)
                CurrentState = ButtonStates.mouseOver;
            else
                CurrentState = ButtonStates.normal;

            return result;

        }
        public override void ProcessMouseDown(Point mousePos)
        {
            //base.ProcessMouseDown(mousePos);


            if (CurrentState != Button.ButtonStates.Disable)
            {
                Interface.activeWidget = this;

                if (IsCheckable)
                {
                    if (IsChecked)
                        IsChecked = false;
                    else
                    {
                        Group wg = Parent as Group;
                        if (wg != null)
                        {
                            if (affectMultiSelectState && !multiSelect)
                            {
                                foreach (Button but in wg.Children.OfType<Button>())
                                {
                                    if (but.IsCheckable)
                                    {
                                        if (but.IsChecked && but.affectMultiSelectState)
                                        {
                                            but.IsChecked = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        IsChecked = true;
                    }
                }
                else if (clickEvent != null)
                    clickEvent(this);

                CurrentState = Button.ButtonStates.mouseDown;
            }


        }
        public override void ProcessMouseUp(Point mousePos)
        {
            if (CurrentState != Button.ButtonStates.Disable)
            {
                CurrentState = ButtonStates.normal;
                ProcessMousePosition(mousePos);
            }

            //base.ProcessMouseUp(mousePos);
        }

        internal override void updateGraphic()
        {
            if (IsChecked)
            {
                if (checkedIcon != null)
                {
                    icon.isVisible = false;
                    checkedIcon.isVisible = true;
                }
            }
            else
            {
                if (checkedIcon != null)
                {
                    icon.isVisible = true;
                    checkedIcon.isVisible = false;
                }

            }

            base.updateGraphic();
        }
        public override void registerForRedraw()
        {
            base.registerForRedraw();
        }
        public override void cairoDraw(ref Context ctx, Rectangles clip = null)
        {
            base.cairoDraw(ref ctx, clip);

            //int stride = 4 * renderBounds.Width;
            //int bmpSize = Math.Abs(stride) * renderBounds.Height;
            //bmp = new byte[bmpSize];

            //using (ImageSurface draw =
            //    new ImageSurface(bmp, Format.Argb32, renderBounds.Width, renderBounds.Height, stride))
            //{

            Rectangle r = renderBoundsInContextCoordonate.Clone;

            ctx.Save();
            ctx.ResetClip();
            ctx.Antialias = Antialias.Subpixel;
            if (IsCheckable)
            {
                if (IsChecked)
                {
                    if (border3D)
                        Interface.StrokeLoweredRectangle(ctx, r);

                }
                else
                {
                    if (checkedIcon == null)
                    {
                        ctx.Color = new Color(0.3, 0.3, 0.3, 0.4);
                        ctx.Rectangle(r);
                        ctx.Fill();
                    }
                    if (border3D)
                        Interface.StrokeRaisedRectangle(ctx, r);
                }
            }
            switch (CurrentState)
            {
                case ButtonStates.normal:
                    break;
                case ButtonStates.mouseOver:
                    ctx.Operator = Operator.Add;
                    ctx.Rectangle(r);
                    ctx.Color = new Color(0.2, 0.2, 0.2, 1.0);
                    ctx.Fill();
                    ctx.Operator = Operator.Over;
                    break;
                case ButtonStates.mouseDown:
                    //ctx.Scale(0.7, 0.7);
                    ctx.Operator = Operator.Add;
                    ctx.Rectangle(r);
                    ctx.Color = Color.Red1;
                    ctx.Fill();
                    ctx.Operator = Operator.Over;
                    break;
                case ButtonStates.Disable:
                    ctx.Color = new Color(0.2, 0.2, 0.2, 0.7);
                    ctx.Rectangle(r);
                    ctx.Fill();
                    break;

            }
            ctx.Restore();

        }
        //ctx.Target.WriteToPng(directories.rootDir + @"test.png");

        //public override void Render()
        //{
        //    if (!isVisible)
        //        return;

        //    switch (CurrentState)
        //    {
        //        case ButtonStates.normal:
        //            if (IsCheckable)
        //            {
        //                if (IsChecked)
        //                    Interface.setTeint(1.0f);
        //                else
        //                    Interface.setTeint(0.6f);
        //            }
        //            else
        //                Interface.setTeint(1.0f);
        //            break;
        //        case ButtonStates.mouseOver:
        //            //GL.PixelZoom(1.2f, 1.2f);
        //            Interface.setTeint(1.1f);
        //            GL.Color3(winColors.Red);
        //            break;
        //        case ButtonStates.mouseDown:
        //            GL.PixelZoom(0.9f, 0.9f);
        //            break;
        //        case ButtonStates.Disable:
        //            Interface.setTeint(0.3f);
        //            break;
        //    }

        //    ImageWidget img = icon;

        //    if (icon != null)
        //    {
        //        if (IsCheckable)
        //        {
        //            if (checkedIcon != null && IsChecked)
        //            {
        //                img = checkedIcon;
        //            }
        //        }
        //    }

        //    if (invalidateLayout)
        //        updateLayout();


        //    if (label != null)
        //        label.Render();

        //    if (img != null)
        //        img.Render();


        //    GL.PixelZoom(1.0f, 1.0f);
        //    Interface.setTeint(1.0f);

        //    //if (CurrentState == ButtonStates.mouseOver)
        //    //{
        //    //    GL.PixelTransfer(PixelTransferParameter.BlueScale, 5.0f);
        //    //    GL.PixelTransfer(PixelTransferParameter.RedScale, 2.5f);
        //    //    GL.PixelTransfer(PixelTransferParameter.GreenScale, 2.5f);
        //    //}

        //    base.renderBaseWidgetOnly();

        //    Interface.setTeint(1.0f);
        //}        
    }
}
