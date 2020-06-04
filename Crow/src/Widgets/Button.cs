// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// templated button control
	/// </summary>
	public class Button : TemplatedContainer
    {
		#region CTOR
		protected Button() {}
		public Button (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		string image;
		bool isPressed;

		public event EventHandler Pressed;
		public event EventHandler Released;

		#region GraphicObject Overrides
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			IsPressed = true;

			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			IsPressed = false;

			base.onMouseUp (sender, e);
		}
		#endregion

		[DefaultValue("#Crow.Images.button.svg")]
		public string Image {
			get { return image; }
			set {
				if (image == value)
					return;
				image = value;
				NotifyValueChangedAuto (image);
			}
		}
		[DefaultValue(false)]
		public bool IsPressed
		{
			get { return isPressed; }
			set
			{
				if (isPressed == value)
					return;

				isPressed = value;

				NotifyValueChangedAuto (isPressed);

				if (isPressed)
					Pressed.Raise (this, null);
				else
					Released.Raise (this, null);
			}
		}
	}
}
