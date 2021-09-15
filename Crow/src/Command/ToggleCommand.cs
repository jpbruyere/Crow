// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection.Emit;

namespace Crow {
	/// <summary>
	/// helper class to bind in one step icon, caption, action, and validity tests to a controls
	/// </summary>
	public class ToggleCommand : Command, IToggle
	{
		#region CTOR
		public ToggleCommand (ICommandHost _host, string caption, Binding<bool> toggleBinding, string icon = null, KeyBinding _keyBinding = null,
						Binding<bool> _canExecuteBinding = null)
			: base (_host, caption, icon, _keyBinding, _canExecuteBinding)
		{
			binding = toggleBinding;
			delSet = binding.CreateSetter (host);
			delGet = binding.CreateGetter (host);

			host.ValueChanged +=  toggleCommand_host_valueChanged_handler;
		}
		public ToggleCommand (ICommandHost _host, string caption, Binding<bool> toggleBinding, string icon = null,
					KeyBinding _keyBinding = null, bool _canExecute = true)
			: this (_host, caption, toggleBinding, icon, _keyBinding, null)
		{
			CanExecute = _canExecute;
		}
		#endregion
		Binding<bool> binding;
		Action<bool> delSet;
		Func<bool> delGet;

		void toggleCommand_host_valueChanged_handler (object sender, ValueChangeEventArgs e) {
			if (e.MemberName != binding.SourceMember)
				return;
			bool tog = (bool)e.NewValue;
			NotifyValueChanged ("IsToggled", tog);
			if (tog)
				ToggleOn.Raise (this, null);
			else
				ToggleOff.Raise (this, null);

		}


		/// <summary>
		/// trigger the execution of the command
		/// </summary>
		public override void Execute (object sender = null){
			if (CanExecute)
				IsToggled = !IsToggled;
		}

		internal override void raiseAllValuesChanged()
		{
			base.raiseAllValuesChanged();
			NotifyValueChanged ("IsToggled", IsToggled);
		}

		#region IToggle implementation
		public event EventHandler ToggleOn;
		public event EventHandler ToggleOff;
		public BooleanTestOnInstance IsToggleable {get; set; }
		public bool IsToggled {
			get => delGet ();
			set {
				if (value == IsToggled)
					return;
				delSet (value);
				NotifyValueChanged ("IsToggled", IsToggled);
				//Console.WriteLine ($"ToggleCommand.IsToggled => {value}");

				if (IsToggled)
					ToggleOn.Raise (this, null);
				else
					ToggleOff.Raise (this, null);
			}
		}
		#endregion

		#region IDispose implementation
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed && host != null)
				host.ValueChanged -=  toggleCommand_host_valueChanged_handler;
			base.Dispose (true);
		}
		#endregion
	}
}
