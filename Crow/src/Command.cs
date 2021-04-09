using System.Runtime.InteropServices.ComTypes;
// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

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
	public class CommandGroup : CommandBase, IEnumerable, IList<CommandBase>
	{
		public ObservableList<CommandBase> Commands = new ObservableList<CommandBase>();

		public CommandGroup () { }
		public CommandGroup (string caption, string icon, params CommandBase[] commands) :
			base (caption, icon) {
			Commands.AddRange (commands);
		}
		public CommandGroup (string caption, params CommandBase[] commands) :
			base (caption) {
			Commands.AddRange (commands);
		}
		public CommandGroup (params CommandBase[] commands) {
			Commands.AddRange (commands);
		}

		
		public int Count => Commands.Count;

		public bool IsReadOnly => false;

		public CommandBase this[int index] { get => Commands[index]; set => Commands[index] = value; }

		public IEnumerator GetEnumerator() => Commands.GetEnumerator ();

		public int IndexOf(CommandBase item) => Commands.IndexOf (item);

		public void Insert(int index, CommandBase item) => Commands.Insert(index, item);

		public void RemoveAt(int index) => Commands.RemoveAt(index);

		public void Add(CommandBase item) => Commands.Add (item);

		public void Clear() => Commands.Clear();

		public bool Contains(CommandBase item) => Commands.Contains (item);

		public void CopyTo(CommandBase[] array, int arrayIndex) => Commands.CopyTo (array, arrayIndex);		

		public bool Remove(CommandBase item) {			
			Commands.Remove (item);
			return true;
		}

		IEnumerator<CommandBase> IEnumerable<CommandBase>.GetEnumerator()
			=> Commands.GetEnumerator();
	}


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
