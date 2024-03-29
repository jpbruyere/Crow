﻿// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;

namespace Crow.Text
{
	[DebuggerDisplay ("{Line}, {Column}, {VisualCharXPosition}")]
	public struct CharLocation : IEquatable<CharLocation>
	{
		public readonly int Line;
		/// <summary>
		/// Character position in current line. If equals '-1', the visualX must contains the on screen position.
		///
		/// </summary>
		public int Column;
		public double VisualCharXPosition;
		public CharLocation (int line, int column, double visualX = -1) {
			Line = line;
			Column = column;
			VisualCharXPosition = visualX;
		}
		public bool HasVisualX => Column >= 0 && VisualCharXPosition >= 0;
		public void ResetVisualX () => VisualCharXPosition = -1;
		public static bool operator == (CharLocation a, CharLocation b)
			=> a.Equals (b);
		public static bool operator != (CharLocation a, CharLocation b)
			=> !a.Equals (b);
		public bool Equals (CharLocation other) {
			return Column < 0 ?
				Line == other.Line && VisualCharXPosition == other.VisualCharXPosition :
				Line == other.Line && Column == other.Column;
		}
		public override bool Equals (object obj) => obj is CharLocation loc ? Equals (loc) : false;
		public override int GetHashCode () {
			return Column < 0 ?
				HashCode.Combine (Line, VisualCharXPosition) :
				HashCode.Combine (Line, Column);
		}

		public override string ToString () => $"{Line}, {Column}";
	}
}
