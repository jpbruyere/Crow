// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Crow {	
	/// <summary>
	/// helper class to bind in one step icon, caption, action, and validity tests to a controls 
	/// </summary>
	public class Command : CommandBase
	{

		#region CTOR
		public Command () {}
		/// <summary>
		/// Initializes a new instance of Command with the action passed as argument.
		/// </summary>
		/// <param name="_executeAction">action to excecute when command is triggered</param>
		public Command (Action _executeAction) {
			execute = _executeAction;
		}
		/// <summary>
		/// Initializes a new instance of Command with the action<object> passed as argument.
		/// </summary>
		/// <param name="_executeAction">action to excecute when command is triggered</param>
		public Command (Action<object> _executeAction) {
			execute = _executeAction;
		}
		public Command (string caption, Action executeAction, string icon = null, bool _canExecute = true)
			:base (caption, icon)
		{					
			execute = executeAction;
			canExecute = _canExecute;
		}
		public Command (string caption, Action<object> executeAction, string icon = null, bool _canExecute = true)
			:base (caption, icon)
		{					
			execute = executeAction;
			canExecute = _canExecute;
		}
		
		#endregion

		Delegate execute;		

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
		public virtual void Execute (object sender = null){
			if (execute != null && CanExecute){
				Task task =	(execute is Action a) ?
					task = new Task(a) :				
				(execute is Action<object> o) ?
					task = new Task(o, sender) : throw new Exception("Invalid Delegate type in Crow.Command, expecting Action or Action<object>");
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
