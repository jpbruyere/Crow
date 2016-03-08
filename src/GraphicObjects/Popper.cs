using System;
using System.Xml.Serialization;
using System.ComponentModel;
using OpenTK.Input;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.Popper.style")]
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

		protected void _content_LayoutChanged (object sender, LayoutingEventArgs e)
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
		public override void ClearBinding ()
		{
			//ensure popped window is cleared
			if (Content != null) {
				if (Content.Parent != null)
					Interface.CurrentInterface.DeleteWidget (Content);
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
			IsPopped = false;
			base.onMouseLeave (sender, e);
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
			if (Interface.CurrentInterface == null)
				return;
			if (Content != null) {
				Content.Visible = true;
				if (Content.Parent == null)
					Interface.CurrentInterface.AddWidget (Content);
				Interface.CurrentInterface.PutOnTop (Content);
				_content_LayoutChanged (this, new LayoutingEventArgs (LayoutingType.Sizing));
			}
			Pop.Raise (this, e);
		}
		public virtual void onUnpop(object sender, EventArgs e)
		{
			if (Interface.CurrentInterface == null)
				return;
			if (Content != null)
				Content.Visible = false;
			Unpop.Raise (this, e);
		}
	}
}
