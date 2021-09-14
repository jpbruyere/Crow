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
	public class ActionCommand : Command
	{

		#region CTOR
		public ActionCommand () {}
		/// <summary>
		/// Initializes a new instance of Command with the action passed as argument.
		/// </summary>
		/// <param name="_executeAction">action to excecute when command is triggered</param>
		public ActionCommand (Action _executeAction) {
			execute = _executeAction;
		}
		/// <summary>
		/// Initializes a new instance of Command with the action<object> passed as argument.
		/// </summary>
		/// <param name="_executeAction">action to excecute when command is triggered</param>
		public ActionCommand (Action<object> _executeAction) {
			execute = _executeAction;
		}
		public ActionCommand (string caption, Action executeAction, string icon = null, bool _canExecute = true)
			: base (caption, icon, _canExecute)
		{
			execute = executeAction;
		}
		public ActionCommand (string caption, Action<object> executeAction, string icon = null, bool _canExecute = true)
			: base (caption, icon, _canExecute)
		{
			execute = executeAction;
		}
		#endregion

		Delegate execute;

		/// <summary>
		/// trigger the execution of the command
		/// </summary>
		public override void Execute (object sender = null) {
			if (execute != null && CanExecute){
				Task task =	(execute is Action a) ?
					task = new Task(a) :
				(execute is Action<object> o) ?
					task = new Task(o, sender) : throw new Exception("Invalid Delegate type in Crow.ActionCommand, expecting Action or Action<object>");
				task.Start();
			}
		}
	}
}
