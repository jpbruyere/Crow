using System;
using System.Xml.Serialization;
using System.ComponentModel;
using OpenTK.Input;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.ScrollBar.goml")]
	public class ScrollBar : NumericControl
	{
		Orientation _orientation;

		#region CTOR
		public ScrollBar() : base()	{}
		#endregion

		[XmlAttributeAttribute()][DefaultValue(Orientation.Vertical)]
		public virtual Orientation Orientation
		{
			get { return _orientation; }
			set {				
				if (_orientation == value)
					return;
				_orientation = value;
				NotifyValueChanged ("Orientation", _orientation);
				RegisterForGraphicUpdate ();
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
