﻿//
// Popper.cs
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
    public class Popper : TemplatedContainer
    {
		#region CTOR
		protected Popper () : base(){}
		public Popper (Interface iface) : base(iface){}
		#endregion

		bool _isPopped, _canPop;
		Alignment popDirection;
		GraphicObject _content;
		Measure popWidth, popHeight;

		public event EventHandler Popped;
		public event EventHandler Unpoped;

		#region Public Properties
		[DefaultValue("Fit")]
		public virtual Measure PopWidth {
			get { return popWidth; }
			set {
				if (popWidth == value)
					return;
				popWidth = value;
				NotifyValueChanged ("PopWidth", popWidth);
			}
		}
		[DefaultValue("Fit")]
		public virtual Measure PopHeight {
			get { return popHeight; }
			set {
				if (popHeight == value)
					return;
				popHeight = value;
				NotifyValueChanged ("PopHeight", popHeight);
			}
		}
		[DefaultValue(false)]
		public bool IsPopped
		{
			get { return _isPopped; }
			set
			{
				if (!_canPop & value)
					return;					
				
				if (value == _isPopped)
					return;

				_isPopped = value;

				NotifyValueChanged ("IsPopped", _isPopped);

				if (_isPopped)
					onPop (this, null);
				else
					onUnpop (this, null);

			}
		}
		[DefaultValue(true)]
		public bool CanPop
		{
			get { return _canPop; }
			set
			{
				if (value == _canPop)
					return;

				_canPop = value;
				NotifyValueChanged ("CanPop", _canPop);
			}
		}
		[DefaultValue(Alignment.Bottom)]
		public virtual Alignment PopDirection {
			get { return popDirection; }
			set {
				if (popDirection == value)
					return;
				popDirection = value;
				NotifyValueChanged ("PopDirection", popDirection);
			}
		}
		#endregion

		public override GraphicObject Content {
			get { return _content; }
			set {
				if (_content != null) {
					_content.LogicalParent = null;
					_content.isPopup = false;
					_content.LayoutChanged -= _content_LayoutChanged;
				}

				_content = value;

				if (_content == null)
					return;

				_content.LogicalParent = this;
				_content.isPopup = true;
				_content.HorizontalAlignment = HorizontalAlignment.Left;
				_content.VerticalAlignment = VerticalAlignment.Top;
				_content.LayoutChanged += _content_LayoutChanged;
			}
		}
		void positionContent(LayoutingType lt){
			ILayoutable tc = Content.Parent;
			if (tc == null)
				return;
			Rectangle r = this.ScreenCoordinates (this.Slot);
			if (lt == LayoutingType.X) {
				if (popDirection.HasFlag (Alignment.Right)) {
					if (r.Right + Content.Slot.Width > tc.ClientRectangle.Right)
						Content.Left = r.Left - Content.Slot.Width;
					else
						Content.Left = r.Right;
				} else if (popDirection.HasFlag (Alignment.Left)) {
					if (r.Left - Content.Slot.Width < tc.ClientRectangle.Left)
						Content.Left = r.Right;
					else
						Content.Left = r.Left - Content.Slot.Width;
				} else {
					if (Content.Slot.Width < tc.ClientRectangle.Width) {
						if (r.Left + Content.Slot.Width > tc.ClientRectangle.Right)
							Content.Left = tc.ClientRectangle.Right - Content.Slot.Width;
						else
							Content.Left = r.Left;
					} else
						Content.Left = 0;
				}
			}else if (lt == LayoutingType.Y) {
				if (Content.Slot.Height < tc.ClientRectangle.Height) {
					if (PopDirection.HasFlag (Alignment.Bottom)) {
						if (r.Bottom + Content.Slot.Height > tc.ClientRectangle.Bottom)
							Content.Top = r.Top - Content.Slot.Height;
						else
							Content.Top = r.Bottom;
					} else if (PopDirection.HasFlag (Alignment.Top)) {
						if (r.Top - Content.Slot.Height < tc.ClientRectangle.Top)
							Content.Top = r.Bottom;
						else
							Content.Top = r.Top - Content.Slot.Height;
					} else
						Content.Top = r.Top;
				}else
					Content.Top = 0;
			}
		}
		protected void _content_LayoutChanged (object sender, LayoutingEventArgs e)
		{
			if (e.LayoutType.HasFlag (LayoutingType.Width))
				positionContent (LayoutingType.X);
			if (e.LayoutType.HasFlag(LayoutingType.Height))
				positionContent (LayoutingType.Y);
		}

		#region GraphicObject overrides
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (this, e);
			IsPopped = false;
		}
		public override bool MouseIsIn (Point m)
		{			
			if (Content?.Parent != null)
				if (Content.MouseIsIn (m))
					return true;
			return base.MouseIsIn (m);
		}
		public override void checkHoverWidget (MouseMoveEventArgs e)
		{
			if (IFace.HoverWidget != this) {
				IFace.HoverWidget = this;
				onMouseEnter (this, e);
			}
			if (Content != null){
				if (Content.Parent != null) {
					if (Content.MouseIsIn (e.Position)) {
						Content.checkHoverWidget (e);
						return;
					}
				}
			}
			base.checkHoverWidget (e);
		}
		#endregion

		public virtual void onPop(object sender, EventArgs e)
		{
			if (Content != null) {
				Content.Visible = true;
				if (Content.Parent == null)
					IFace.AddWidget (Content);
				if (Content.LogicalParent != this)
					Content.LogicalParent = this;
				IFace.PutOnTop (Content, true);
				_content_LayoutChanged (this, new LayoutingEventArgs (LayoutingType.Sizing));
			}
			Popped.Raise (this, e);
		}
		public virtual void onUnpop(object sender, EventArgs e)
		{
			if (Content != null) {
				Content.Visible = false;
			}
			Unpoped.Raise (this, e);
		}

		protected override void Dispose (bool disposing)
		{
			if (_content != null && disposing) {
				if (_content.Parent == null)
					_content.Dispose ();
			}
			base.Dispose (disposing);
		}
	}
}
