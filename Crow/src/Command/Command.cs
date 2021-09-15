// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Crow {

	public interface ICommandHost : IValueChange
	{
		event EventHandler<KeyEventArgs> KeyDown;
	}
	/// <summary>
	/// Base abstract class for commands with an execute method.
	/// </summary>
	public abstract class Command : CommandBase, IDisposable
	{

		#region CTOR
		public Command () {}
		public Command (string caption, string icon = null, bool _canExecute = true )
			:base (caption, icon)
		{
			CanExecute = _canExecute;
		}
		public Command (ICommandHost _host, string caption, string icon, KeyBinding _keyBinding = null,
						bool _canExecute = true)
			: this (_host, caption, icon, _keyBinding, null)
		{
			CanExecute = _canExecute;
		}

		public Command (ICommandHost _host, string caption, string icon, KeyBinding _keyBinding = null,
						Binding<bool> _canExecuteBinding = null)
			: base (caption, icon)
		{
			host = _host;
			keyBinding = _keyBinding;
			canExecuteBinding = _canExecuteBinding;

			if (HasKeyBinding)
				host.KeyDown += key_handler;
			if (HasCanExecuteBinding) {
				host.ValueChanged += canExecuteBinding_handler;
				CanExecute = canExecuteBinding.Get (host);
			}
		}
		#endregion

		protected ICommandHost host;
		bool canExecute = true;
		KeyBinding keyBinding;
		Binding<bool> canExecuteBinding;

		public KeyBinding KeyBinding => keyBinding;
		public bool HasKeyBinding => keyBinding != null;
		public bool HasCanExecuteBinding => canExecuteBinding != null;

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
		void key_handler (object sender, KeyEventArgs e) {
			if (CanExecute && e.Key == keyBinding.Key && e.Modifiers == keyBinding.Modifiers) {
				Execute (sender);
				e.Handled = true;
			}
		}
		void canExecuteBinding_handler (object sender, ValueChangeEventArgs e) {
			if (e.MemberName == canExecuteBinding.SourceMember)
				CanExecute = (bool)e.NewValue;
		}
		#region IDispose implementation
		protected bool disposed;
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !disposed && host != null) {
				if (HasKeyBinding)
					host.KeyDown -= key_handler;
				if (HasCanExecuteBinding)
					host.ValueChanged -= canExecuteBinding_handler;
			}
			disposed = true;
		}
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
