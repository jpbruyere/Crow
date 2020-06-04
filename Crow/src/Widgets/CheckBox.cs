// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
	/// <summary>
	/// templated checkbox control
	/// </summary>
	public class CheckBox : TemplatedControl
	{
		#region CTOR
		protected CheckBox() {}
		public CheckBox (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		bool isChecked;

		public event EventHandler Checked;
		public event EventHandler Unchecked;

		[DefaultValue(false)]
		public bool IsChecked
		{
			get { return isChecked; }
			set
			{
				if (isChecked == value)
					return;

				isChecked = value;

				NotifyValueChangedAuto (isChecked);

				if (isChecked)
					Checked.Raise (this, null);
				else
					Unchecked.Raise (this, null);
			}
		}

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsChecked = !IsChecked;
			base.onMouseClick (sender, e);
		}
	}
}
