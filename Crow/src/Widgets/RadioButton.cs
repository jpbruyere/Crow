// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// Bistate templated control. Several occurences in one Group constrol will allow
	/// only one checked RadioButton, other are automatically disabled.
	/// </summary>
	public class RadioButton : TemplatedControl
	{
		bool isChecked;

		#region CTOR
		protected RadioButton () { }
		public RadioButton (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		public event EventHandler Checked;
		public event EventHandler Unchecked;

		#region Widget overrides
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			Group pg = Parent as Group;
			if (pg != null) {
				for (int i = 0; i < pg.Children.Count; i++) {
					RadioButton c = pg.Children [i] as RadioButton;
					if (c == null)
						continue;
					c.IsChecked = (c == this);
				}
			} else
				IsChecked = !IsChecked;

			base.onMouseDown (sender, e);
		}
		#endregion

		[DefaultValue (false)]
		public bool IsChecked {
			get { return isChecked; }
			set {
				if (isChecked == value)
					return;

				isChecked = value;

				NotifyValueChangedAuto (value);

				if (isChecked)
					Checked.Raise (this, null);
				else
					Unchecked.Raise (this, null);
			}
		}
	}
}
