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
	public class ScrollBar : NumericControl
	{
		Orientation _orientation;

		#region CTOR
		public ScrollBar() : base()	{}
		#endregion

		[XmlAttributeAttribute()][DefaultValue(0.0)]
		public override double Maximum {
			get { return base.Maximum; }
			set { base.Maximum = value; }
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
			Value -= SmallIncrement;
		}
		public void onScrollForth (object sender, MouseButtonEventArgs e)
		{
			Value += SmallIncrement;
		}

		public void onSliderValueChange(object sender, ValueChangeEventArgs e){
			if (e.MemberName == "Value")
				Value = Convert.ToDouble(e.NewValue);
		}
	}
}
