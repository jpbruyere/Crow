using System.Runtime.InteropServices.ComTypes;
// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections;

namespace Crow {
	public abstract class CommandBase : IValueChange {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion
		
		#region CTOR
		protected CommandBase() {}
		protected CommandBase (string _caption, string _icon)
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
	public class CommandGroup : CommandBase, IEnumerable
	{
		public ObservableList<CommandBase> Commands = new ObservableList<CommandBase>();

		public CommandGroup () { }
		public CommandGroup (string caption, string icon, params CommandBase[] commands) :
			base (caption, icon) {
			Commands.AddRange (commands);
		}
		public CommandGroup (params CommandBase[] commands) {
			Commands.AddRange (commands);
		}

		public IEnumerator GetEnumerator() => Commands.GetEnumerator ();
	}


	/// <summary>
	/// helper class to bind in one step icon, caption, action, and validity tests to a controls 
	/// </summary>
	public class Command : CommandBase
	{

		#region CTOR
		public Command () {}
		/// <summary>
		/// Initializes a new instance of Command with the action pass as argument.
		/// </summary>
		/// <param name="_executeAction">action to excecute when command is triggered</param>
		public Command (Action _executeAction)
		{
			execute = _executeAction;
		}
		public Command (string caption, Action executeAction, string icon = null, bool _canExecute = true)
			:base (caption, icon)
		{					
			execute = executeAction;
			canExecute = _canExecute;
		}
		
		#endregion

		Action execute;		
		bool canExecute = true;
		
		/// <summary>
		/// if true, action defined in this command may be executed,
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
		public virtual void Execute(){
			if (execute != null && CanExecute){
				Task task = new Task(execute);
				task.Start();
			}
		}
		internal override void raiseAllValuesChanged()
		{
			base.raiseAllValuesChanged();
			NotifyValueChanged ("CanExecute", CanExecute);
		}
	}
}
