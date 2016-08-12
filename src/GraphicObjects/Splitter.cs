//
//  Spliter.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	public class Splitter : GraphicObject
	{
		#region CTOR
		public Splitter (): base(){}
		#endregion

		int thickness;

		[XmlAttributeAttribute][DefaultValue(1)]
		public virtual int Thickness {
			get { return thickness; }
			set {
				if (thickness == value)
					return;
				thickness = value; 
				NotifyValueChanged ("Thickness", thickness);
				RegisterForLayouting (LayoutingType.Sizing);
				RegisterForGraphicUpdate ();
			}
		} 
		#region GraphicObject override
		public override ILayoutable Parent {
			get { return base.Parent; }
			set {
				if (value != null) {			
					GenericStack gs = value as GenericStack;
					if (gs == null)
						throw new Exception ("Splitter may only be chil of stack");
					
				}
				base.Parent = value;
			}
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if ((Parent as GenericStack).Orientation == Orientation.Horizontal)
				Interface.CurrentInterface.MouseCursor = XCursor.H;
			else
				Interface.CurrentInterface.MouseCursor = XCursor.V;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			Interface.CurrentInterface.MouseCursor = XCursor.Default;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!IsActive)
				return;

			GenericStack gs = Parent as GenericStack;
			int ptrSplit = gs.Children.IndexOf (this);

			if (ptrSplit == 0 || ptrSplit == gs.Children.Count - 1)
				return;

			if (gs.Orientation == Orientation.Horizontal) {
				if ((gs.Children [ptrSplit - 1].Width + e.XDelta <
					gs.Children [ptrSplit - 1].MinimumSize.Width) ||
					(gs.Children [ptrSplit + 1].Width - e.XDelta <
						gs.Children [ptrSplit + 1].MinimumSize.Width))
					return;
				
				if (!gs.Children [ptrSplit - 1].Width.IsFixed)
					gs.Children [ptrSplit - 1].Width = gs.Children [ptrSplit - 1].Slot.Width;					
				if (!gs.Children [ptrSplit + 1].Width.IsFixed)
					gs.Children [ptrSplit + 1].Width = gs.Children [ptrSplit + 1].Slot.Width;
				
				gs.Children [ptrSplit - 1].Width = gs.Children [ptrSplit - 1].Width + e.XDelta;
				gs.Children [ptrSplit + 1].Width = gs.Children [ptrSplit + 1].Width - e.XDelta;

			} else {
				if ((gs.Children [ptrSplit - 1].Height + e.YDelta <
					gs.Children [ptrSplit - 1].MinimumSize.Height) ||
					(gs.Children [ptrSplit + 1].Height - e.YDelta <
						gs.Children [ptrSplit + 1].MinimumSize.Height))
					return;
				
				if (!gs.Children [ptrSplit - 1].Height.IsFixed)
					gs.Children [ptrSplit - 1].Height = gs.Children [ptrSplit - 1].Slot.Height;
				if (!gs.Children [ptrSplit + 1].Height.IsFixed)
					gs.Children [ptrSplit + 1].Height = gs.Children [ptrSplit + 1].Slot.Height;
				
				gs.Children [ptrSplit - 1].Height = gs.Children [ptrSplit - 1].Height + e.YDelta;
				gs.Children [ptrSplit + 1].Height = gs.Children [ptrSplit + 1].Height - e.YDelta;
			}
		}
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			GenericStack gs = Parent as GenericStack;
			if (layoutType == LayoutingType.Width){
				if (gs.Orientation == Orientation.Horizontal)
					Width = thickness;
				else
					Width = Measure.Stretched;
			} else if (layoutType == LayoutingType.Height){
				if (gs.Orientation == Orientation.Vertical)
					Height = thickness;
				else
					Height = Measure.Stretched;
			}
			return base.UpdateLayout (layoutType);
		}
		#endregion
	}
}

