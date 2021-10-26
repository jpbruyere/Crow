// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// templated button control
	/// </summary>
	public class Button : TemplatedContainer
    {
		#region CTOR
		protected Button() {}
		public Button (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		string icon;
		bool isPressed;
		Command command;

		public event EventHandler Pressed;
		public event EventHandler Released;

		#region Widget Overrides
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			IsPressed = true;

			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			IsPressed = false;

			base.onMouseUp (sender, e);
		}
		#endregion

		[DefaultValue (null)]
		public virtual Command Command {
			get => command;
			set {
				if (command == value)
					return;

				if (command != null) {
					command.raiseAllValuesChanged ();
					command.ValueChanged -= Command_ValueChanged;
				}

				command = value;

				if (command != null) {
					command.ValueChanged += Command_ValueChanged;
					command.raiseAllValuesChanged ();
				}

				NotifyValueChangedAuto (command);
			}
		}

		[DefaultValue ("#Crow.Icons.crow.svg")]
		public string Icon {
			get => Command == null ? icon : Command.Icon;
			set {
				if (icon == value)
					return;
				icon = value;
				if (command == null)
					NotifyValueChangedAuto (icon);
			}
		}
		public override bool IsEnabled {
			get => Command == null ? base.IsEnabled : Command.CanExecute;
			set => base.IsEnabled = value;
		}

		public override string Caption {
			get => Command == null ? base.Caption : Command.Caption;
			set => base.Caption = value;
		}

		[DefaultValue (false)]
		public bool IsPressed
		{
			get => isPressed;
			set
			{
				if (isPressed == value)
					return;

				isPressed = value;

				NotifyValueChangedAuto (isPressed);

				if (isPressed)
					Pressed.Raise (this, null);
				else
					Released.Raise (this, null);
			}
		}

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			command?.Execute (this);
			e.Handled = true;
			base.onMouseClick (sender, e);
		}

		void Command_ValueChanged (object sender, ValueChangeEventArgs e)
		{
			string mName = e.MemberName;
			if (mName == "CanExecute") {
				mName = "IsEnabled";
				RegisterForRedraw ();
			}
			NotifyValueChanged (mName, e.NewValue);
		}
	}
}
