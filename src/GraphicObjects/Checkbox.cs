using System;


using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OpenTK.Graphics.OpenGL;

using Cairo;

using winColors = System.Drawing.Color;
using System.Diagnostics;
using System.Xml.Serialization;
using OpenTK.Input;

namespace go
{
	public delegate void GOEvent(GraphicObject sender);

    public enum ButtonStates
    {
        normal,
        mouseOver,
        mouseDown,
        Disable
    }

    public class Checkbox : Container, IXmlSerializable
    {
        public GOEvent Click;

        bool _isCheckable = false;
        bool _isChecked = false;
        bool _affectMultiSelectState = false;
        bool _border3D = false;
        Color _checkedColor;
        ButtonStates _CurrentState = ButtonStates.normal;

        public Checkbox() : base()
        {}

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValue(true)]
        public override bool Focusable
        {
            get { return base.Focusable; }
            set { base.Focusable = value; }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public Color CheckedColor
        {
            get { return _checkedColor; }
            set
            {
                _checkedColor = value;
                registerForGraphicUpdate();
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public ButtonStates CurrentState
        {
            get { return _CurrentState; }
            set
            {
                if (value == _CurrentState)
                    return;
                _CurrentState = value;
                registerForRedraw();
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValue(false)]
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (value == _isChecked)
                    return;

                _isChecked = value;

                if (Click != null)
                    Click(this);

                registerForGraphicUpdate();
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValue(false)]
        public bool IsCheckable
        {
            get { return _isCheckable; }
            set { _isCheckable = value; }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValue(false)]
        public bool AffectMultiSelectState
        {
            get { return _affectMultiSelectState; }
            set { _affectMultiSelectState = value; }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValue(false)]
        public bool Border3d
        {
            get { return _border3D; }
            set { _border3D = value; }
        }

        public override void Paint(ref Context ctx, Rectangles clip = null)
        {
            base.Paint(ref ctx, clip);

			Rectangle r = Parent.ContextCoordinates (Slot);

            ctx.Save();
			//ctx.ResetClip();
            
            if (IsCheckable){
                if (IsChecked){
                    ctx.Color = CheckedColor;
                    ctx.Rectangle(r);
                    ctx.Fill();

                    if (Border3d)
                        CairoHelpers.StrokeLoweredRectangle(ctx, r);
                }
                else{
                    if (Border3d)
                        CairoHelpers.StrokeRaisedRectangle(ctx, r);
                }
            }

            switch (CurrentState){
                case ButtonStates.normal:
                    break;
			case ButtonStates.mouseOver:
					CairoHelpers.CairoRectangle(ctx,r,CornerRadius);
                    ctx.Operator = Operator.Add;
                    ctx.Color = new Color(0.2, 0.2, 0.2, 1.0);
                    ctx.Fill();
                    ctx.Operator = Operator.Over;
                    break;
                case ButtonStates.mouseDown:                    
					CairoHelpers.CairoRectangle(ctx,r,CornerRadius);
                    ctx.Operator = Operator.Add;
                    ctx.Color = Color.Red;
                    ctx.Fill();
                    ctx.Operator = Operator.Over;
                    break;
                case ButtonStates.Disable:
					CairoHelpers.CairoRectangle(ctx,r,CornerRadius);
                    ctx.Color = new Color(0.2, 0.2, 0.2, 0.7);
                    ctx.Fill();
                    break;

            }
            ctx.Restore();
        }


		#region mouse handling
		public override void onMouseButtonDown (object sender,MouseButtonEventArgs e)
		{
            if (CurrentState != ButtonStates.Disable)
            {
                if (IsCheckable)
                {
                    if (IsChecked)
                        IsChecked = false;
                    else
                    {
                        Group wg = Parent as Group;
                        if (wg != null)
                        {
                            if (AffectMultiSelectState && !wg.MultiSelect)
                            {
                                foreach (Checkbox but in wg.Children.OfType<Checkbox>())
                                {
                                    if (but.IsCheckable)
                                    {
                                        if (but.IsChecked && but.AffectMultiSelectState)
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
				//else if (Click != null)
				//    Click(this);

                CurrentState = ButtonStates.mouseDown;
            }

			base.onMouseButtonDown (sender, e);
		}
		public override void onMouseButtonUp (object sender, OpenTK.Input.MouseButtonEventArgs e)
		{
			if (CurrentState != ButtonStates.Disable) {
				if (MouseIsIn (e.Position))
					CurrentState = ButtonStates.mouseOver;
				else
					CurrentState = ButtonStates.normal;

			}

			base.onMouseButtonUp (sender, e);
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			if (CurrentState == ButtonStates.normal)
				CurrentState = ButtonStates.mouseOver;

			base.onMouseEnter (sender, e);
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			if (CurrentState == ButtonStates.mouseOver)
				CurrentState = ButtonStates.normal;

			base.onMouseLeave (sender, e);
		}
		#endregion

		#region IXmlSerializable
        public override System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
        public override void ReadXml(System.Xml.XmlReader reader)
        {
            string handler = reader.GetAttribute("OnClick");
			Interface.EventsToResolve.Add(new EventSource 
            { 
                Source = this, 
                Handler = handler,
                EventName = "OnClick"
            });

            //Container c = this as Container;
            base.ReadXml(reader);
        }
        public override void WriteXml(System.Xml.XmlWriter writer)
        {
            if (Click != null)
            {
                writer.WriteAttributeString("OnClick", Click.Method.Name);
            }

            base.WriteXml(writer);
        }
		#endregion
	}
}
