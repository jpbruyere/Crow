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

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.ScrollBar.goml")]
	public class ScrollBar : TemplatedControl
	{
		Orientation _orientation;
		Slider _slider;
		double _maximumScroll;
		double _scroll;

		public ScrollBar() : base()
		{
		}	

		protected override void loadTemplate(GraphicObject template = null)
		{			
			base.loadTemplate (template);
			_slider = this.child.FindByName ("Slider") as Slider;
		}

		[XmlAttributeAttribute()][DefaultValue(0.0)]
		public virtual double MaximumScroll
		{
			get { return _maximumScroll; }
			set {
				if (_maximumScroll == value)
					return;
				_maximumScroll = value;
				registerForGraphicUpdate ();
				NotifyValueChanged ("MaximumScroll", _maximumScroll);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0.0)]
		public virtual double Scroll
		{
			get { return _scroll; }
			set {
				if (_scroll == value)
					return;
				_scroll = value;
				if (_scroll < 0.0)
					_scroll = 0.0;
				else if (_scroll > _maximumScroll)
					_scroll = _maximumScroll;
				registerForGraphicUpdate ();
				NotifyValueChanged ("Scroll", _scroll);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(Orientation.Vertical)]
		public virtual Orientation Orientation
		{
			get { return _orientation; }
			set {				
				if (_orientation == value)
					return;
				_orientation = value;
				NotifyValueChanged ("Orientation", _orientation);
				registerForGraphicUpdate ();
			}
		}
		public void onScrollBack (object sender, MouseButtonEventArgs e)
		{
			Scroll -= _slider.LargeIncrement;
		}
		public void onScrollForth (object sender, MouseButtonEventArgs e)
		{
			Scroll += _slider.LargeIncrement;

		}

		public void onSliderValueChange(object sender, ValueChangeEventArgs e){
			Scroll = Convert.ToDouble(e.NewValue);
		}

		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			OpenTKGameWindow.currentWindow.CursorVisible = true;
			base.OnLayoutChanges (layoutType);
		}
	}
}
