// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow {
	/// <summary>
	/// Base class for Command and CommandGroup.
	/// </summary>
	public abstract class CommandBase : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion
		
		#region CTOR
		protected CommandBase() {}
		protected CommandBase (string _caption, string _icon = null)
		{
			caption = _caption;			
			icon = _icon;
		}
		#endregion
		
		string caption, icon;

		/// <summary>
		/// label to display in the bound control
		/// </summary>
		[DefaultValue("Unamed Command")]
		public virtual string Caption {
			get => caption;
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChanged ("Caption", caption);

			}
		}
		/// <summary>
		/// Icon to display in the bound control
		/// </summary>		
		public string Icon {
			get => icon;
			set {
				if (icon == value)
					return;
				icon = value;
				NotifyValueChanged ("Icon", icon);
			}
		}
		internal virtual void raiseAllValuesChanged() {		
			NotifyValueChanged ("Icon", icon);
			NotifyValueChanged ("Caption", caption);
		}
	}
}
