// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Crow {
	/// <summary>
	/// Base abstract class for commands with an execute method.
	/// </summary>
	public abstract class Command : CommandBase
	{

		#region CTOR
		public Command () {}
		public Command (string caption, string icon = null, bool _canExecute = true)
			:base (caption, icon)
		{}
		#endregion

		bool canExecute = true;

		/// <summary>
		/// if true, command may be executed,
		/// </summary>
		[DefaultValue(true)]
		public virtual bool CanExecute {
			get => canExecute;
			set {
				if (canExecute == value)
					return;
				canExecute = value;
				NotifyValueChanged ("CanExecute", canExecute);
			}
		}

		/// <summary>
		/// trigger the execution of the command
		/// </summary>
		public abstract void Execute (object sender = null);

		internal override void raiseAllValuesChanged()
		{
			base.raiseAllValuesChanged();
			NotifyValueChanged ("CanExecute", CanExecute);
		}
	}
}
