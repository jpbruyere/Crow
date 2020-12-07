// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow
{
	public class MenuItem : Menu
	{
		#region CTOR
		protected MenuItem () : base(){}
		public MenuItem (Interface iface) : base(iface) {}
		#endregion

		public event EventHandler Open;
		public event EventHandler Close;

		Command command;
		string icon;
		bool isOpened;
		Measure popWidth, popHeight;

		#region Public properties
		[DefaultValue(false)]
		public bool IsOpened {
			get { return isOpened; }
			set {
				if (isOpened == value)
					return;
				isOpened = value;
				NotifyValueChangedAuto (isOpened);

				if (isOpened) {
					onOpen (this, null);
					if (LogicalParent is Menu)
						(LogicalParent as Menu).AutomaticOpening = true;
				}else
					onClose (this, null);
			}
		}
		[DefaultValue(null)]
		public virtual Command Command {
			get { return command; }
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
		
		public override bool IsEnabled {
			get { return Command == null ? base.IsEnabled : Command.CanExecute; }
			set { base.IsEnabled = value; }
		}
		
		public override string Caption {
			get { return Command == null ? base.Caption : Command.Caption; }
			set { base.Caption = value; }
		}
		
		public string Icon {
			get { return Command == null ? icon : Command.Icon;; }
			set {
				if (icon == value)
					return;
				icon = value;
				if (command == null)
					NotifyValueChangedAuto (icon);
			}
		}
		[DefaultValue("Fit")]
		public virtual Measure PopWidth {
			get { return popWidth; }
			set {
				if (popWidth == value)
					return;
				popWidth = value;
				NotifyValueChangedAuto (popWidth);
			}
		}
		[DefaultValue("Fit")]
		public virtual Measure PopHeight {
			get { return popHeight; }
			set {
				if (popHeight == value)
					return;
				popHeight = value;
				NotifyValueChangedAuto (popHeight);
			}
		}
		#endregion

		public override void AddItem (Widget g)
		{
			base.AddItem (g);
			g.NotifyValueChanged ("PopDirection", Alignment.Right);
		}

		void Command_ValueChanged (object sender, ValueChangeEventArgs e)
		{
			string mName = e.MemberName;
			if (mName == "CanExecute")
				mName = "IsEnabled";
			NotifyValueChanged (mName, e.NewValue);
		}
		protected virtual void onOpen (object sender, EventArgs e){
			Open.Raise (this, null);
		}
		protected virtual void onClose (object sender, EventArgs e){
			System.Diagnostics.Debug.WriteLine ("close: " + this.ToString());
			Close.Raise (this, null);
		}
		public override bool MouseIsIn (Point m)
		{
			return IsEnabled && !IsDragged ? base.MouseIsIn (m) || child.MouseIsIn (m) : false;
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			Menu menu = LogicalParent as Menu;
			if (menu == null)
				return;
			if (menu.AutomaticOpening && items.Children.Count>0)
				IsOpened = true;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			if (IsOpened)
				IsOpened = false;
			base.onMouseLeave (this, e);
		}
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			if (command != null) {
				command.Execute ();
				closeMenu ();
			}
			if (hasClick)
				base.onMouseClick (sender, e);

			if (!IsOpened)
				(LogicalParent as Menu).AutomaticOpening = false;
		}
		void closeMenu () {
			MenuItem tmp = LogicalParent as MenuItem;
			while (tmp != null) {
				tmp.IsOpened = false;
				tmp.Background = Colors.Transparent;
				tmp.AutomaticOpening = false;
				tmp = tmp.LogicalParent as MenuItem;
			}
		}
	}
}

