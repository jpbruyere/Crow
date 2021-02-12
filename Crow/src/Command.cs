// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Crow {
	public class CommandGroup : ObservableList<Command>, IValueChange
	{
		string caption;
		string icon;

		/// <summary>
		/// label to display in the bound control
		/// </summary>
		[DefaultValue ("Unamed Command Group")]
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
		public string Icon {
			get { return icon; }
			set {
				if (icon == value)
					return;
				icon = value;
				NotifyValueChanged ("Icon", icon);
			}
		}

		public CommandGroup () { }
		public CommandGroup (params Command[] commands) {
			AddRange (commands);
		}
	}


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
		public Command () {}
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
		string icon;
		bool canExecute = true;

		#region Public properties
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
		/// label to display in the bound control
		/// </summary>
		[DefaultValue("Unamed Command")]
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
		public string Icon {
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
			if (execute != null && CanExecute){
				Task task = new Task(execute);
				task.Start();
			}
		}
		internal void raiseAllValuesChanged(){
			NotifyValueChanged ("CanExecute", CanExecute);
			NotifyValueChanged ("Icon", icon);
			NotifyValueChanged ("Caption", caption);
		}

	}
}
