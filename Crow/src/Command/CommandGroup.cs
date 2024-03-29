﻿// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Crow {
	public class CommandGroup : CommandBase, IEnumerable, IList<CommandBase>
	{
		public IList<CommandBase> Commands { get; private set; }


		public CommandGroup () {
			Commands = new ObservableList<CommandBase>();
		}
		public CommandGroup (string caption, string icon, params CommandBase[] commands) :
			base (caption, icon) {
			Commands = new ObservableList<CommandBase>(commands);
		}
		public CommandGroup (string caption, params CommandBase[] commands) :
			base (caption) {
			Commands = new ObservableList<CommandBase>(commands);
		}
		public CommandGroup (params CommandBase[] commands) {
			Commands = new ObservableList<CommandBase>(commands);
		}
		public CommandGroup (ICommandHost host) {

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

		public bool Remove(CommandBase item) => Commands.Remove (item);

		IEnumerator<CommandBase> IEnumerable<CommandBase>.GetEnumerator()
			=> Commands.GetEnumerator();
		public void Add (params CommandBase[] items) {
			foreach  (CommandBase c in items)
				Commands.Add (c);
		}
		public void Remove (params CommandBase[] items) {
			foreach  (CommandBase c in items)
				Commands.Remove (c);
		}
		/// <summary>
		/// Set boolean value for the CanExecute state of all commands
		/// </summary>
		/// <param name="canExecute"></param>
		public void ToggleAllCommand (bool canExecute) {
			foreach  (ActionCommand c in Commands.OfType<ActionCommand> ())
				c.CanExecute = canExecute;
		}
	}
}
