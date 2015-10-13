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

namespace go
{
	[DefaultTemplate("#go.Templates.Popper.goml")]
    public class Popper : TemplatedContainer
    {		
		#region CTOR
		public Popper() : base()
		{
		}	
		#endregion
		bool _isPopped;
		string title;
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
					_content.MouseLeave -= _content_MouseLeave;
				}
				
				_content = value; 

				if (_content == null)
					return;

				_content.LogicalParent = this;
				_content.Focusable = true;
				_content.LayoutChanged += _content_LayoutChanged;
				_content.MouseLeave += _content_MouseLeave;
			}
		}

		void _content_MouseLeave (object sender, MouseMoveEventArgs e)
		{
			IsPopped = false;
		}

		void _content_LayoutChanged (object sender, LayoutChangeEventArgs e)
		{
			ILayoutable tc = Content.Parent as ILayoutable;
			if (tc == null)
				return;
			Rectangle r = this.ScreenCoordinates (this.Slot);
			if (e.LayoutType == LayoutingType.Width) {
				if (Content.Slot.Width < tc.ClientRectangle.Width) {
					if (r.Left + Content.Slot.Width > tc.ClientRectangle.Right)
						Content.Left = tc.ClientRectangle.Right - Content.Slot.Width;
					else
						Content.Left = r.Left;
				}else
					Content.Left = 0;
			}else if (e.LayoutType == LayoutingType.Height) {
				if (Content.Slot.Height < tc.ClientRectangle.Height) {
					if (r.Bottom + Content.Slot.Height > tc.ClientRectangle.Bottom)
						Content.Top = r.Top - Content.Slot.Height;
					else
						Content.Top = r.Bottom;
				}else
					Content.Top = 0;
			}
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

		[XmlAttributeAttribute()][DefaultValue(true)]//overiden to get default to true
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}

		[XmlAttributeAttribute()][DefaultValue("Popper")]
		public string Title {
			get { return title; } 
			set {
				if (title == value)
					return;
				title = value; 
				NotifyValueChanged ("Title", title);
			}
		}        
		[XmlAttributeAttribute()][DefaultValue("#go.Images.Icons.expandable.svg")]
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
			
		public virtual void onPop(object sender, EventArgs e)
		{
			IGOLibHost tc = TopContainer;
			if (tc == null)
				return;
			if (Content != null) {
				Content.Visible = true;
				if (Content.Parent == null)
					tc.AddWidget (Content);
			}
			Pop.Raise (this, e);
		}
		public virtual void onUnpop(object sender, EventArgs e)
		{
			IGOLibHost tc = TopContainer;
			if (tc == null)
				return;
			Content.Visible = false;
			Unpop.Raise (this, e);
		}
			
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsPopped = !IsPopped;
			base.onMouseClick (sender, e);
		}

	}
}
