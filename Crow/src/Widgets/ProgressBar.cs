// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.ComponentModel;
using Crow.Cairo;

namespace Crow
{
	/// <summary>
	/// Templated numeric control for displaying a progress indicator
	/// </summary>
	public class ProgressBar : NumericControl
    {
		#region CTOR
		protected ProgressBar () {}
		public ProgressBar(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		Orientation orientation;

		[DefaultValue (Orientation.Horizontal)]
		public virtual Orientation Orientation {
			get => orientation;
			set {
				if (orientation == value)
					return;
				orientation = value;
				NotifyValueChangedAuto (orientation);
				RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
			}
		}
	}
}
