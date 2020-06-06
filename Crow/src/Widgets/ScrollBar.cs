// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

namespace Crow
{
	/// <summary>
	/// templeted numeric control
	/// </summary>
	public class ScrollBar : Slider
	{
		#region CTOR
		protected ScrollBar () {}
		public ScrollBar(Interface iface, string style = null) : base (iface, style) { }
		#endregion

	}
}
