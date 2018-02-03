//
// Command.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// helper class to bind in one step icon, caption, action, and validity tests to a controls 
	/// </summary>
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
		/// <summary>
		/// Initializes a new instance of Command with the action pass as argument.
		/// </summary>
		/// <param name="_executeAction">action to excecute when command is triggered</param>
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
		/// <summary>
		/// if true, action defined in this command may be executed,
		/// </summary>
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
		/// <summary>
		/// label to display in the bound control
		/// </summary>
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
		/// <summary>
		/// Icon to display in the bound control
		/// </summary>
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

		/// <summary>
		/// trigger the execution of the command
		/// </summary>
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
