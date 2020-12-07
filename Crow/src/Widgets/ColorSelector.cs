// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;

namespace Crow
{
	/// <summary>
	/// simple squarred rgb color selector
	/// </summary>
	[DesignIgnore]
	public class ColorSelector : Widget
	{
		#region CTOR
		protected ColorSelector() {}
		public ColorSelector (Interface iface, string style = null) : base(iface, style){ }
		#endregion

		protected Point mousePos;

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			if (IFace.IsDown (MouseButton.Left))
				return;
			updateMouseLocalPos (e.Position);
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
			if (e.Button == MouseButton.Left)
				updateMouseLocalPos (e.Position);
		}

		protected virtual void updateMouseLocalPos(Point mPos){
			Rectangle r = ScreenCoordinates (Slot);
			Rectangle cb = ClientRectangle;
			mousePos = mPos - r.Position;

			mousePos.X = Math.Max(cb.X, mousePos.X);
			mousePos.X = Math.Min(cb.Right, mousePos.X);
			mousePos.Y = Math.Max(cb.Y, mousePos.Y);
			mousePos.Y = Math.Min(cb.Bottom, mousePos.Y);

			RegisterForRedraw ();
		}
	}
}

