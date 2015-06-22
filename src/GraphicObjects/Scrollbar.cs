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

namespace go
{
	[DefaultTemplate("#go.Templates.Scrollbar.goml")]
	public class Scrollbar : TemplatedControl, IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		#endregion

		Orientation _orientation;
		Slider _slider;

		public Scrollbar() : base()
		{
		}	

		protected override void loadTemplate(GraphicObject template = null)
		{			
			base.loadTemplate (template);
			_slider = this.child.FindByName ("Slider") as Slider;
		}

		[XmlAttributeAttribute()][DefaultValue(Orientation.Vertical)]
		public virtual Orientation Orientation
		{
			get { return _orientation; }
			set {				
				if (_orientation == value)
					return;
				_orientation = value;
				ValueChanged.Raise(this, new ValueChangeEventArgs ("Orientation", _orientation));
			}
		}
		public void onScrollBack (object sender, MouseButtonEventArgs e)
		{
			_slider.Value -= _slider.LargeIncrement;
		}
		public void onScrollForth (object sender, MouseButtonEventArgs e)
		{
			_slider.Value += _slider.LargeIncrement;
		}
	}
}
