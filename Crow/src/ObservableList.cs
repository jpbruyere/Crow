using System.Reflection;
// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Crow
{
	public class ObservableList<T> : List<T>, IObservableList, IValueChange {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
		{
			ValueChanged?.Invoke (this, new ValueChangeEventArgs (MemberName, _value));
		}
		#endregion

		#region IObservableList implementation
		public event EventHandler<ListChangedEventArg> ListAdd;
		public event EventHandler<ListChangedEventArg> ListRemove;
		public event EventHandler<ListChangedEventArg> ListEdit;
		public event EventHandler<ListClearEventArg> ListClear;
		#endregion

		public ObservableList() : base () {}
		public ObservableList (IEnumerable<T> collection) : base (collection) { }

		int selectedIndex = -1;

		public int SelectedIndex {
			get => selectedIndex;
			set {
				if (selectedIndex == value)
					return;

				if (value > Count - 1)
					selectedIndex = Count - 1;
				else
					selectedIndex = value;

				NotifyValueChanged ("SelectedIndex", selectedIndex);
				NotifyValueChanged ("SelectedItem", SelectedItem);
			}
		}
		public T SelectedItem {
			get => selectedIndex < 0 ? default(T) : this [selectedIndex];
			set {
				this [selectedIndex] = value;
			}
		}
		public new void Add (T elem) {
			base.Add (elem);
			ListAdd.Raise (this, new ListChangedEventArg (this.Count - 1, elem));
			SelectedIndex = this.Count - 1;
		}
		public new void Insert (int index, T elem) {
			base.Insert (index, elem);
			ListAdd.Raise (this, new ListChangedEventArg (index, elem));
			SelectedIndex = index;
		}
		public new void Remove (T elem) {
			int idx = IndexOf (elem);
			if (idx < 0)
				Console.WriteLine ($"ObsList.Remove idx < 0: {new System.Diagnostics.StackTrace()}");
			else
				base.RemoveAt (idx);
			ListRemove.Raise (this, new ListChangedEventArg (idx, elem));
		}
		public new void Clear ()
		{
			ListClearEventArg eventArg = new ListClearEventArg (this.Cast<object>());
			base.Clear ();
			ListClear.Raise (this, eventArg);
		}
		public void Remove () {
			if (selectedIndex < 0)
				return;
			RemoveAt (selectedIndex);
			SelectedIndex--;
		}
		public void Insert ()
		{
			base.Insert (selectedIndex+1, default(T));
			SelectedIndex++;
			ListAdd.Raise (this, new ListChangedEventArg (selectedIndex, SelectedItem));
		}
		public void Replace (T oldValue, T newValue) {
			int idx = IndexOf (oldValue);
			base[idx] = newValue;
			ListEdit.Raise (this, new ListChangedEventArg (idx, newValue));
		}
		public void RaiseEdit () {
			if (selectedIndex < 0)
				return;
			ListEdit.Raise (this, new ListChangedEventArg (selectedIndex, SelectedItem));
		}

		public new void RemoveAt (int index)
		{
			base.RemoveAt (index);
			ListRemove.Raise (this, new ListChangedEventArg (index, null));
		}
		public void RaiseEditAt (int index) {
			ListEdit.Raise (this, new ListChangedEventArg (index, this[index]));
		}


		public static ObservableList<T> Parse (string str) {
			ObservableList<T> tmp = new ObservableList<T>();
			Type t = typeof(T);
			MethodInfo miParse = t.GetMethod ("Parse", BindingFlags.Static | BindingFlags.Public,
							Type.DefaultBinder, new Type [] {typeof (string)}, null);
			if (miParse == null)
				throw new Exception ("no Parse method found for: " + t.FullName);
			if (!string.IsNullOrEmpty (str)) {
				foreach (string s in str.Split(';'))
					tmp.Add((T)miParse.Invoke (null, new object[] {s}));				
			}
			return tmp;
		}
	}
}

