// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

namespace Crow
{
	/// <summary>
	/// templated control for selecting a numeric value by clicking on
	/// up and down buttons
	/// </summary>
	public class Spinner : NumericControl
	{
		#region CTOR
		protected Spinner() {}
		public Spinner (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			base.onMouseClick (sender, e);
		}
		void onUp (object sender, MouseButtonEventArgs e)
		{
			Value += this.SmallIncrement;
		}
		void onDown (object sender, MouseButtonEventArgs e)
		{
			Value -= this.SmallIncrement;
		}

	}
}

