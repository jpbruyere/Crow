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
using System.ComponentModel;
using System.Xml;
using System.IO;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.Popper.goml")]
    public class Popper : TemplatedContainer
    {		
		#region CTOR
		public Popper() : base()
		{
		}	
		#endregion
		bool _isPopped;
		string caption;
		string image;
		GraphicObject _content;

		public event EventHandler Pop;
		public event EventHandler Unpop;

		public override GraphicObject Content {
			get { return _content; }
			set { 
				if (_content != null) {
					_content.LogicalParent = null;
					_content.LayoutChanged -= _content_LayoutChanged;
				}
				
				_content = value; 

				if (_content == null)
					return;

				_content.LogicalParent = this;
				_content.LayoutChanged += _content_LayoutChanged;
			}
		}

		void _content_LayoutChanged (object sender, LayoutingEventArgs e)
		{
			ILayoutable tc = Content.Parent as ILayoutable;
			if (tc == null)
				return;
			Rectangle r = this.ScreenCoordinates (this.Slot);
			if (e.LayoutType.HasFlag(LayoutingType.Width)) {
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
			}
			if (e.LayoutType.HasFlag(LayoutingType.Height)) {
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

		#region GraphicObject overrides
		[XmlAttributeAttribute()][DefaultValue(true)]//overiden to get default to true
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		public override void ClearBinding ()
		{
			//ensure popped window is cleared
			if (Content != null) {
				if (Content.Parent != null) {
					IGOLibHost tc = Content.Parent as IGOLibHost;
					if (tc != null)
						tc.DeleteWidget (Content);
				}
			}
			base.ClearBinding ();
		}
		public override void ResolveBindings ()
		{
			base.ResolveBindings ();
			if (Content != null)
				Content.ResolveBindings ();
		}

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsPopped = !IsPopped;
			base.onMouseClick (sender, e);
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			IsPopped = false;
		}
		#endregion

		#region Public Properties
		[XmlAttributeAttribute()][DefaultValue("Popper")]
		public string Caption {
			get { return caption; } 
			set {
				if (caption == value)
					return;
				caption = value; 
				NotifyValueChanged ("Caption", caption);
			}
		}        
		[XmlAttributeAttribute()][DefaultValue("#Crow.Images.Icons.expandable.svg")]
		public string Image {
			get { return image; } 
			set {
				if (image == value)
					return;
				image = value; 
				NotifyValueChanged ("Image", image);
			}
		} 
		[XmlAttributeAttribute()][DefaultValue(false)]
        public bool IsPopped
        {
			get { return _isPopped; }
            set
            {
				_isPopped = value;

				if (_isPopped) {
					onPop (this, null);
					NotifyValueChanged ("SvgSub", "expanded");
					return;
				}

				onUnpop (this, null);
				NotifyValueChanged ("SvgSub", "collapsed");
            }
        }
		Alignment popDirection;
		[XmlAttributeAttribute()][DefaultValue(Alignment.Bottom)]
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
			
		public virtual void onPop(object sender, EventArgs e)
		{
			IGOLibHost tc = HostContainer;
			if (tc == null)
				return;
			if (Content != null) {
				Content.Visible = true;
				if (Content.Parent == null)
					tc.AddWidget (Content);
				tc.PutOnTop (Content);
				_content_LayoutChanged (this, new LayoutingEventArgs (LayoutingType.Sizing));
			}
			Pop.Raise (this, e);
		}
		public virtual void onUnpop(object sender, EventArgs e)
		{
			IGOLibHost tc = HostContainer;
			if (tc == null)
				return;
			Content.Visible = false;
			Unpop.Raise (this, e);
		}
	}
}
