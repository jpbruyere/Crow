//
// crow_object_t.cs
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
using System.Runtime.InteropServices;
using System;

namespace Crow.Native {
	public enum CrowType : byte {
		Simple,
		Container,
		Group,
		Stack
	}

	[StructLayout(LayoutKind.Sequential)]
	unsafe public struct crow_object_t {
		public CrowType TypeObj;
		public IntPtr Context;
		public int ChildrenCount;
		public crow_object_t* Parent;
		public crow_object_t** Children;
		public int Left;
		public int Top;
		public Measure Width;
		public Measure Height;
		public int Margin;
		public Size MinimumSize;
		public Size MaximumSize;
		public byte Visible;
		/// <summary>if true, content has to be recreated</summary>
		public byte IsDirty;
		public byte InClippingPool;
		public IntPtr Clipping;
		public LayoutingType RegisteredLayoutings;
		/// <summary>
		/// Current size and position computed during layouting pass
		/// </summary>
		public Rectangle Slot;
		public Size ContentSize;
		/// <summary>
		/// keep last slot components for each layouting pass to track
		/// changes and trigger update of other component accordingly
		/// </summary>
		public Rectangle LastSlot;
		/// <summary>
		/// keep last slot painted on screen to clear traces if moved or resized
		/// TODO: we should ensure the whole parsed widget tree is the last painted
		/// version to clear effective oldslot if parents have been moved or resized.
		/// IDEA is to add a ScreenCoordinates function that use only lastPaintedSlots
		/// </summary>
		public Rectangle LastPaintedSlot;
		public VerticalAlignment VerticalAlignment;
		public HorizontalAlignment HorizontalAlignment;
		public IntPtr bmp;

		public IntPtr MeasureRawSize;
		public IntPtr UpdateLayout;
		public IntPtr OnLayoutChanged;
		public IntPtr OnChildLayoutChanged;
		public IntPtr ChildrenLayoutingConstraints;
		public IntPtr OnDraw;

	}		
}