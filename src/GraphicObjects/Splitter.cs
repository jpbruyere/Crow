//
// Splitter.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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

		Unit u1, u2;
		int init1 = -1, init2 = -1, delta = 0, min1, min2, max1 , max2;
		GraphicObject go1 = null, go2 = null;

		void initSplit(Measure m1, int size1, Measure m2, int size2){
			if (m1 != Measure.Stretched) {
				init1 = size1;
				u1 = m1.Units;
			}
			if (m2 != Measure.Stretched) {
				init2 = size2;
				u2 = m2.Units;
			}
		}
		void convertSizeInPix(GraphicObject g1){

		}

		#region GraphicObject override
		public override GraphicObject Parent {
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
				CurrentInterface.MouseCursor = XCursor.H;
			else
				CurrentInterface.MouseCursor = XCursor.V;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			CurrentInterface.MouseCursor = XCursor.Default;
		}
		unsafe public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
			go1 = go2 = null;
			init1 = init2 = -1;
			delta = 0;

			GenericStack gs = Parent as GenericStack;
			int ptrSplit = gs.Children.IndexOf (this);
			if (ptrSplit == 0 || ptrSplit == gs.Children.Count - 1)
				return;

			go1 = gs.Children [ptrSplit - 1];
			go2 = gs.Children [ptrSplit + 1];

			if (gs.Orientation == Orientation.Horizontal) {
				initSplit (go1.Width, go1.nativeHnd->Slot.Width, go2.Width, go2.nativeHnd->Slot.Width);
				min1 = go1.MinimumSize.Width;
				min2 = go2.MinimumSize.Width;
				max1 = go1.MaximumSize.Width;
				max2 = go2.MaximumSize.Width;
				if (init1 >= 0)
					go1.Width = init1;
				if (init2 >= 0)
					go2.Width = init2;
			} else {
				initSplit (go1.Height, go1.nativeHnd->Slot.Height, go2.Height, go2.nativeHnd->Slot.Height);
				min1 = go1.MinimumSize.Height;
				min2 = go2.MinimumSize.Height;
				max1 = go1.MaximumSize.Height;
				max2 = go2.MaximumSize.Height;
				if (init1 >= 0)
					go1.Height = init1;
				if (init2 >= 0)
					go2.Height = init2;
			}
		}
		unsafe public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!IsActive)
				return;

			GenericStack gs = Parent as GenericStack;
			int newDelta = delta, size1 = init1 , size2 = init2;
			if (gs.Orientation == Orientation.Horizontal) {
				newDelta -= e.XDelta;
				if (size1 < 0)
					size1 = go1.nativeHnd->Slot.Width + delta;
				if (size2 < 0)
					size2 = go2.nativeHnd->Slot.Width - delta;
			} else {
				newDelta -= e.YDelta;
				if (size1 < 0)
					size1 = go1.nativeHnd->Slot.Height + delta;
				if (size2 < 0)
					size2 = go2.nativeHnd->Slot.Height - delta;
			}

			if (size1 - newDelta < min1 || (max1 > 0 && size1 - newDelta > max1) ||
				size2 + newDelta < min2 || (max2 > 0 && size2 + newDelta > max2))
				return;

			delta = newDelta;

			if (gs.Orientation == Orientation.Horizontal) {
				if (init1 >= 0)
					go1.Width = init1 - delta;
				if (init2 >= 0)
					go2.Width = init2 + delta;
			} else {
				if (init1 >= 0)
					go1.Height = init1 - delta;
				if (init2 >= 0)
					go2.Height = init2 + delta;
			}
		}
		unsafe public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			GenericStack gs = Parent as GenericStack;

			if (init1 >= 0 && u1 == Unit.Percent) {
				if (gs.Orientation == Orientation.Horizontal)
					go1.Width = new Measure ((int)Math.Ceiling (
						go1.Width.Value * 100.0 / (double)gs.nativeHnd->Slot.Width), Unit.Percent);
				else
					go1.Height = new Measure ((int)Math.Ceiling (
						go1.Height.Value * 100.0 / (double)gs.nativeHnd->Slot.Height), Unit.Percent);
			}
			if (init2 >= 0 && u2 == Unit.Percent) {
				if (gs.Orientation == Orientation.Horizontal)
					go2.Width = new Measure ((int)Math.Floor (
						go2.Width.Value * 100.0 / (double)gs.nativeHnd->Slot.Width), Unit.Percent);
				else
					go2.Height = new Measure ((int)Math.Floor (
						go2.Height.Value * 100.0 / (double)gs.nativeHnd->Slot.Height), Unit.Percent);
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

