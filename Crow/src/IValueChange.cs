// Copyright (c) 20132020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;

namespace Crow
{
	/// <summary>
	/// implement `IValueChange` interface in object you want to bind to the interface.
	/// For each property updated in code, raise a value change in the container class
	/// to inform Crow binding system that the value has changed.
	/// </summary>
	public interface IValueChange
	{
		event EventHandler<ValueChangeEventArgs> ValueChanged;
	}
	/// <summary>
	/// Container for net primitive value type implementing IValueChange
	/// </summary>
	public class ValueContainer<T> : IValueChange, IEquatable<T>//, IConvertible
	{
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		T val;
		public T Value {
			get => val;
			set {
				if (EqualityComparer<T>.Default.Equals (value, val))
					return;
				val = value;
				ValueChanged?.Invoke (this, new Crow.ValueChangeEventArgs ("Value", val));
			}
		}

		public static implicit operator ValueContainer<T>(T v) => new ValueContainer<T> (v);
		public static implicit operator T (ValueContainer<T> v) => v.Value;

		public ValueContainer (T _val) { val = _val; }

		public bool Equals (T other) => val.Equals (other);
		public override bool Equals (object obj) => obj is ValueContainer<T> v && Equals (v);
		public override int GetHashCode () => val.GetHashCode ();
		public override string ToString () => val.ToString ();
	}
}

