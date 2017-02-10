//
//  Command.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	public class Command : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		#region CTOR
		public Command (Action _executeAction)
		{
			execute = _executeAction;
		}
		#endregion

		Action execute;

		string caption;
		Picture icon;
		bool canExecute = true;

		#region Public properties
		[XmlAttributeAttribute][DefaultValue(true)]
		public virtual bool CanExecute {
			get { return canExecute; }
			set {
				if (canExecute == value)
					return;
				canExecute = value;
				NotifyValueChanged ("CanExecute", canExecute);
			}
		}
		[XmlAttributeAttribute][DefaultValue("Unamed Command")]
		public virtual string Caption {
			get { return caption; }
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChanged ("Caption", caption);

			}
		}
		[XmlAttributeAttribute]
		public Picture Icon {
			get { return icon; }
			set {
				if (icon == value)
					return;
				icon = value;
				NotifyValueChanged ("Icon", icon);
			}
		}
		#endregion

		public void Execute(){
			if (execute != null && CanExecute)
				execute ();
		}
		internal void raiseAllValuesChanged(){
			NotifyValueChanged ("CanExecute", canExecute);
			NotifyValueChanged ("Icon", icon);
			NotifyValueChanged ("Caption", caption);
		}

	}
}
