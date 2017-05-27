//
// Measure.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;

namespace Crow
{
	/// <summary>
	/// Measurement unit
	/// </summary>
	public enum Unit : byte { Pixel, Percent, Inherit }
	/// <summary>
	/// Measure class allow proportional sizes as well as stretched and fit on content.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Measure
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
		public static Measure Fit = new Measure(-1);
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
		public bool IsFixed { get { return Value > 0 && Units == Unit.Pixel; }}
		public bool IsFit { get { return Value == -1 && Units == Unit.Pixel; }}
		#region Operators
		public static implicit operator int(Measure m){
			return m.Value;
		}
		public static implicit operator Measure(int i){
			return new Measure(i);
		}
		public static implicit operator string(Measure m){
			return m.ToString();
		}
		public static implicit operator Measure(string s){
			return Measure.Parse(s);
		}

		public static bool operator ==(Measure m1, Measure m2){
			return m1.Value == m2.Value && m1.Units == m2.Units;
		}
		public static bool operator !=(Measure m1, Measure m2){
			return !(m1.Value == m2.Value && m1.Units == m2.Units);
		}
		#endregion

		#region Object overrides
		public override int GetHashCode ()
		{
			return Value.GetHashCode ();
		}
		public override bool Equals (object obj)
		{
			return (obj == null || obj.GetType() != typeof(Measure)) ?
				false :
				this == (Measure)obj;
		}
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
				return Measure.Stretched;

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
