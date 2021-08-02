// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

using Glfw;
using Crow.Drawing;

namespace Crow
{
	/// <summary>
	/// Color component slider with background gradient ranging from 0 to 1 for this component value.
	/// </summary>
	[DesignIgnore]
	public class ColorSlider : Widget
	{
		#region CTOR
		protected ColorSlider() {}
		public ColorSlider (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		protected Point mousePos;//store local mouse pos sync with currentValue
		Orientation orientation;
		CursorType cursorType = CursorType.Pentagone;
		ColorComponent component;
		Color currentColor = Colors.Black;
		int currentValue = -1;//force notify for property less binding 'CurrentValue'

		[DefaultValue (Orientation.Horizontal)]
		public Orientation Orientation {
			get => orientation;
			set {
				if (orientation == value)
					return;
				orientation = value;
				NotifyValueChangedAuto (orientation);
				RegisterForGraphicUpdate ();
			}
		}
		[DefaultValue (CursorType.Pentagone)]
		public CursorType CursorType {
			get => cursorType;
			set {
				if (cursorType == value)
					return;
				cursorType = value;
				NotifyValueChangedAuto (cursorType);
				RegisterForRedraw ();
			}
		}
		[DefaultValue (ColorComponent.Value)]
		public ColorComponent Component {
			get => component;
			set {
				if (component == value)
					return;
				component = value;
				NotifyValueChangedAuto (component);
				RegisterForRedraw ();
			}
		}
		public Color CurrentColor {
			get => currentColor;
			set {
				if (currentColor == value)
					return;

				currentColor = value;

				NotifyValueChangedAuto (currentColor);
				RegisterForRedraw ();

				switch (component) {
				case ColorComponent.Red:
					if (currentValue == currentColor.R)
						return;
					currentValue = (int)currentColor.R;
					break;
				case ColorComponent.Green:
					if (currentValue == currentColor.G)
						return; 
					currentValue = (int)currentColor.G;
					break;
				case ColorComponent.Blue:
					if (currentValue == currentColor.B)
						return;
					currentValue = (int)currentColor.B;
					break;
				case ColorComponent.Alpha:
					if (currentValue == currentColor.A)
						return;
					currentValue = (int)currentColor.A;
					break;
				case ColorComponent.Hue:
					if (currentValue == currentColor.Hue)
						return;
					currentValue = (int)currentColor.Hue;
					break;
				case ColorComponent.Saturation:
					if (currentValue == currentColor.Saturation)
						return;
					currentValue = (int)currentColor.Saturation;
					break;
				case ColorComponent.Value:
					if (currentValue == currentColor.Value)
						return; 
					currentValue = (int)currentColor.Value;
					break;
				}

				NotifyValueChanged ("CurrentValue", $"{currentValue:000}");

				if (Orientation == Orientation.Horizontal)
					mousePos.X = (int)Math.Floor ((double)currentValue * ClientRectangle.Width / 255.0);
				else
					mousePos.Y = (int)Math.Floor ((double)currentValue * ClientRectangle.Height / 255.0);
			}
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			if (IFace.IsDown(MouseButton.Left)) {				
				updateMouseLocalPos (e.Position);
				e.Handled = true;
			}
			base.onMouseMove (sender, e);
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			
			if (e.Button == MouseButton.Left) {
				updateMouseLocalPos (e.Position);
				e.Handled = true;
			}
			base.onMouseDown (sender, e);
		}

		protected override void onDraw (Context gr) {
			DbgLogger.StartEvent (DbgEvtType.GODraw, this);

			base.onDraw (gr);

			RectangleD r = ClientRectangle;
			r.Height -= 4;
			r.Y += 2;

			Gradient.Type gt = Gradient.Type.Horizontal;
			if (Orientation == Orientation.Vertical)
				gt = Gradient.Type.Vertical;

			Gradient grad = new Gradient (gt);
			Color c = currentColor;

			switch (component) {
			case ColorComponent.Red:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (0, c.G, c.B, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (255, c.G, c.B, c.A)));
				break;
			case ColorComponent.Green:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (c.R, 0, c.B, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (c.R, 255, c.B, c.A)));
				break;
			case ColorComponent.Blue:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (c.R, c.G, 0, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (c.R, c.G, 255, c.A)));
				break;
			case ColorComponent.Alpha:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (c.R, c.G, c.B, 0)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (c.R, c.G, c.B, 255)));
				break;
			case ColorComponent.Hue:
				grad.Stops.Add (new Gradient.ColorStop (0, new Color (1.0, 0, 0, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.167, new Color (1.0, 1, 0, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.333, new Color (0.0, 1, 0, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.5, new Color (0.0, 1, 1, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.667, new Color (0.0, 0, 1, 1)));
				grad.Stops.Add (new Gradient.ColorStop (0.833, new Color (1.0, 0, 1, 1)));
				grad.Stops.Add (new Gradient.ColorStop (1, new Color (1.0, 0, 0, 1)));
				break;
			case ColorComponent.Saturation:
				grad.Stops.Add (new Gradient.ColorStop (0, Color.FromHSV (c.Hue, c.Value, 0, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, Color.FromHSV (c.Hue, c.Value, 0xff, c.A)));
				break;
			case ColorComponent.Value:
				grad.Stops.Add (new Gradient.ColorStop (0, Color.FromHSV (c.Hue, 0, c.Saturation, c.A)));
				grad.Stops.Add (new Gradient.ColorStop (1, Color.FromHSV (c.Hue, 0xff, c.Saturation, c.A)));
				break;
			}

			grad.SetAsSource (IFace, gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill ();

			r = ClientRectangle;

			switch (cursorType) {
			case CursorType.Rectangle:
				if (Orientation == Orientation.Horizontal) {
					r.Width = 5;
					r.X = mousePos.X - 2.5;
				} else {
					r.Height = 5;
					r.Y = mousePos.Y - 2.5;
				}
				CairoHelpers.CairoRectangle (gr, r, 1);
				break;
			case CursorType.Circle:
				if (Orientation == Orientation.Horizontal)
					gr.Arc (mousePos.X, r.Center.Y, 3.5, 0, Math.PI * 2.0);
				else
					gr.Arc (r.Center.X, mousePos.Y, 3.5, 0, Math.PI * 2.0);
				break;
			case CursorType.Pentagone:
				if (Orientation == Orientation.Horizontal) {
					r.Width = 5;
					r.X = mousePos.X - 2.5;
					double y = r.CenterD.Y - r.Height * 0.2;
					gr.MoveTo (mousePos.X, y);
					y += r.Height * 0.15;
					gr.LineTo (r.Right, y);
					gr.LineTo (r.Right, r.Bottom - 0.5);
					gr.LineTo (r.Left, r.Bottom - 0.5);
					gr.LineTo (r.Left, y);
					gr.ClosePath ();
				} else {
				}
				break;
			}

			gr.SetSource (Colors.Black);
			gr.LineWidth = 2.0;
			gr.StrokePreserve ();
			gr.SetSource (Colors.White);
			gr.LineWidth = 1.0;
			gr.Stroke ();

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}
		public override void OnLayoutChanges (LayoutingType layoutType) {
			base.OnLayoutChanges (layoutType);

			if (Orientation == Orientation.Horizontal) {
				if (layoutType == LayoutingType.Width)
					mousePos.X = (int)Math.Floor (currentValue / 255.0 * ClientRectangle.Width);
			} else if (layoutType == LayoutingType.Height)
				mousePos.Y = (int)Math.Floor (currentValue / 255.0 * ClientRectangle.Height);
		}

		protected virtual void updateMouseLocalPos(Point mPos){
			Rectangle r = ScreenCoordinates (Slot);
			Rectangle cb = ClientRectangle;
			mousePos = mPos - r.Position;

			mousePos.X = Math.Max(cb.X, mousePos.X);
			mousePos.X = Math.Min(cb.Right, mousePos.X);
			mousePos.Y = Math.Max(cb.Y, mousePos.Y);
			mousePos.Y = Math.Min(cb.Bottom, mousePos.Y);

			if (Orientation == Orientation.Horizontal)
				currentValue = (int)((double)mousePos.X / ClientRectangle.Width * 0xff);
			else
				currentValue = (int)((double)mousePos.Y / ClientRectangle.Height * 0xff);

			NotifyValueChanged ("CurrentValue", $"{currentValue:000}");

			Color c = currentColor;
			switch (component) {
			case ColorComponent.Red:
				CurrentColor = new Color((uint)currentValue, c.G, c.B, c.A);
				break;
			case ColorComponent.Green:
				CurrentColor = new Color (c.R, (uint)currentValue, c.B, c.A);
				break;
			case ColorComponent.Blue:
				CurrentColor = new Color (c.R, c.G, (uint)currentValue, c.A);
				break;
			case ColorComponent.Alpha:
				CurrentColor = new Color (c.R, c.G, c.B, (uint)currentValue);
				break;
			case ColorComponent.Hue:
				CurrentColor = Color.FromHSV ((uint)currentValue, c.Value, c.Saturation, c.A);
				break;
			case ColorComponent.Saturation:
				CurrentColor = Color.FromHSV (c.Hue, c.Value, (uint)currentValue, c.A);
				break;
			case ColorComponent.Value:
				CurrentColor = Color.FromHSV (c.Hue, (uint)currentValue, c.Saturation, c.A);
				break;
			}
		}
	}
}

