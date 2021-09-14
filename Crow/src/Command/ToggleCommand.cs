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
	public class ToggleCommand : Command, IToggle, IDisposable
	{
		#region CTOR
		public ToggleCommand () {}
		public ToggleCommand (object instance, string memberName, string icon = null, bool _canExecute = true)
			: this ("", instance, memberName, icon, _canExecute) { }
		public ToggleCommand (string caption, object instance, string memberName, string icon = null, bool _canExecute = true)
			: base (caption, icon, _canExecute)
		{
			this.instance = instance;
			this.memberName = memberName;
			MemberInfo mbi = instance.GetType().GetMember (memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();

			if (mbi is PropertyInfo pi) {
				delSet = (Action<bool>)Delegate.CreateDelegate (typeof (Action<bool>), instance, pi.GetSetMethod ());
				delGet = (Func<bool>)Delegate.CreateDelegate (typeof (Func<bool>), instance, pi.GetGetMethod ());
			} else if (mbi is FieldInfo fi) {
				DynamicMethod dm = new DynamicMethod($"{fi.ReflectedType.FullName}_fldset", null, new Type[] { fi.ReflectedType, typeof(bool) }, false);
				ILGenerator il = dm.GetILGenerator ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Stfld, fi);

				il.Emit(OpCodes.Ret);

				delSet = (Action<bool>)dm.CreateDelegate (typeof (Action<bool>), instance);

				dm = new DynamicMethod($"{fi.ReflectedType.FullName}_fldget", typeof(bool), new Type[] { fi.ReflectedType }, false);
				il = dm.GetILGenerator ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, fi);

				il.Emit(OpCodes.Ret);

				delGet = (Func<bool>)dm.CreateDelegate (typeof (Func<bool>), instance);
			} else
				throw new Exception ("unsupported member type for ToggleCommand");

			if (instance is IValueChange ivc)
				ivc.ValueChanged +=  instance_valueChanged;

			CanExecute = _canExecute;
		}
		void instance_valueChanged (object sender, ValueChangeEventArgs e) {
			if (e.MemberName != memberName)
				return;
			//Console.WriteLine ($"ToggleCommand valueChanged triggered => {e.NewValue}");

			bool tog = (bool)e.NewValue;
			NotifyValueChanged ("IsToggled", tog);
			if (tog)
				ToggleOn.Raise (this, null);
			else
				ToggleOff.Raise (this, null);

		}
		#endregion
		object instance;
		string memberName;
		Action<bool> delSet;
		Func<bool> delGet;
		bool disposedValue;


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

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (instance is IValueChange ivc)
						ivc.ValueChanged -=  instance_valueChanged;
				}
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
