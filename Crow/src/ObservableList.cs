using System.Reflection;
// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Crow
{
	public class ObservableList<T> : IList<T>, IObservableList, IValueChange, ICollection {
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

		List<T> items;
		public ObservableList() {
			items = new List<T>();
		}
		public ObservableList (IEnumerable<T> collection) {
			items = new List<T> (collection);
		}

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

		public int Count => items.Count;

		public bool IsReadOnly => false;

		public bool IsSynchronized => throw new NotImplementedException();

		public object SyncRoot => throw new NotImplementedException();

		public T this[int index] {
			get => items[index];
			set {

				if (items[index] == null) {
					if (value == null)
						return;
				}else if (items[index].Equals (value))
					return;
				Replace (items[index], value);
			}
		}

		public void Add (T elem) {
			items.Add (elem);
			ListAdd.Raise (this, new ListChangedEventArg (this.Count - 1, elem));
			SelectedIndex = this.Count - 1;
		}
		public void Insert (int index, T elem) {
			items.Insert (index, elem);
			ListAdd.Raise (this, new ListChangedEventArg (index, elem));
			SelectedIndex = index;
		}
		public bool Remove (T elem) {
			int idx = IndexOf (elem);
			if (idx < 0)
				return false;
			else
				items.RemoveAt (idx);
			ListRemove.Raise (this, new ListChangedEventArg (idx, elem));
			return true;
		}
		public void Clear ()
		{
			ListClearEventArg eventArg = new ListClearEventArg (this.Cast<object>());
			items.Clear ();
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
			items.Insert (selectedIndex+1, default(T));
			SelectedIndex++;
			ListAdd.Raise (this, new ListChangedEventArg (selectedIndex, SelectedItem));
		}
		public void Replace (T oldValue, T newValue) {
			int idx = IndexOf (oldValue);
			items[idx] = newValue;
			ListEdit.Raise (this, new ListChangedEventArg (idx, newValue));
		}
		public void RaiseEdit () {
			if (selectedIndex < 0)
				return;
			ListEdit.Raise (this, new ListChangedEventArg (selectedIndex, SelectedItem));
		}

		public void RemoveAt (int index)
		{
			items.RemoveAt (index);
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

		public int IndexOf(T item) => items.IndexOf (item);

		public bool Contains(T item) => items.Contains (item);

		public void CopyTo(T[] array, int arrayIndex) => items.CopyTo (array, arrayIndex);


		public IEnumerator<T> GetEnumerator() => items.GetEnumerator ();

		IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator ();

		public void CopyTo(Array array, int index) => items.ToArray().CopyTo (array, index);
	}
}

