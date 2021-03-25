// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	/// <summary>
	/// Measurement unit
	/// </summary>
	public enum Unit {Undefined, Pixel, Percent, Inherit }
	/// <summary>
	/// Measure class allow proportional sizes as well as stretched and fit on content.
	/// </summary>
	public struct Measure : IEquatable<Measure>
	{
		/// <summary>
		/// Integer value of the measure
		/// </summary>
		public int Value;
		/// <summary>
		/// Measurement unit
		/// </summary>
		public Unit Units;

		/// <summary>
		/// Fit on content, this special measure is defined as a fixed integer set to -1 pixel
		/// </summary>
		public static Measure Fit = new Measure(-1,Unit.Percent);
		/// <summary>
		/// Stretched into parent client area. This special measure is defined as a proportional cote
		/// set to 100 Percents
		/// </summary>
		public static Measure Stretched = new Measure(100, Unit.Percent);
		public static Measure Inherit = new Measure (0, Unit.Inherit);
		#region CTOR
		public Measure (int _value, Unit _units = Unit.Pixel)
		{
			Value = _value;
			Units = _units;
		}
		#endregion

		/// <summary>
		/// True is size is fixed in pixels, this means not proportional, stretched nor fit.
		/// </summary>
		public bool IsFixed { get { return Units == Unit.Pixel; }}
		public bool IsFit { get { return Value == -1 && Units == Unit.Percent; }}
		/// <summary>
		/// True if width is proportional to parent client rectangle
		/// </summary>
		public bool IsRelativeToParent { get { return Value >= 0 && Units == Unit.Percent; }}
		#region Operators
		public static implicit operator int(Measure m) => m.Value;
		public static implicit operator Measure(int i) => new Measure(i);		
		public static implicit operator string(Measure m) => m.ToString();		
		public static implicit operator Measure(string s) => Measure.Parse(s);

		public static bool operator ==(Measure m1, Measure m2) => m1.Equals (m2);
		public static bool operator !=(Measure m1, Measure m2) => !m1.Equals (m2);
		#endregion

		public bool Equals(Measure other) => Value == other.Value && Units == other.Units;

		#region Object overrides
		public override int GetHashCode () => HashCode.Combine (Value, Units);
		public override bool Equals (object obj) => obj is Measure m ? Equals (m) : false;
		public override string ToString ()
		{
			return Units == Unit.Inherit ? "Inherit" :
				Value == -1 ? "Fit" :
				Units == Unit.Percent ? Value == 100 ? "Stretched" :
				Value.ToString () + "%" : Value.ToString ();
		}
		#endregion

		public static Measure Parse(string s){
			if (string.IsNullOrEmpty (s))
				return default(Measure);

			string st = s.Trim ();
			int tmp = 0;

			if (string.Equals ("Inherit", st, StringComparison.Ordinal))
				return Measure.Inherit;
			else if (string.Equals ("Fit", st, StringComparison.Ordinal))
				return Measure.Fit;
			else if (string.Equals ("Stretched", s, StringComparison.Ordinal))
				return Measure.Stretched;			
			else {
				if (st.EndsWith ("%", StringComparison.Ordinal)) {
					if (int.TryParse (s.Substring(0, st.Length - 1), out tmp))
						return new Measure (tmp, Unit.Percent);
				}else if (int.TryParse (s, out tmp))
					return new Measure (tmp);
			}

			throw new Exception ("Error parsing Measure.");
		}

	}
}
