//
//  Node.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Reflection;

namespace Crow.IML
{
	/// <summary>
	/// IML Node are the elements of the interface XML,
	/// </summary>
	public class Node
	{
		public Type CrowType;
		/// <summary>
		/// Indexer for group child, -1 for template
		/// </summary>
		public int Index;

		public Node (Type crowType, int _index = 0)
		{
			CrowType = crowType;
			Index = _index;
		}

		#region Equality Compare
		public override bool Equals (object obj)
		{
			return obj is Node && this == (Node)obj;
		}
		public override int GetHashCode ()
		{
			return CrowType.GetHashCode () ^ Index.GetHashCode ();
		}
		public static bool operator == (Node x, Node y)
		{
			return x.CrowType == y.CrowType && x.Index == y.Index;
		}
		public static bool operator != (Node x, Node y)
		{
			return !(x == y);
		}
		#endregion

		public bool IsTemplate {
			get { return typeof (TemplatedControl).IsAssignableFrom (CrowType) && Index == 0; }
		}
		public MethodInfo AddMethod {
			get {
				if (typeof (Group).IsAssignableFrom (CrowType))
					return CompilerServices.miAddChild;
				if (typeof (Container).IsAssignableFrom (CrowType))
					return CompilerServices.miSetChild;
				if (typeof (TemplatedContainer).IsAssignableFrom (CrowType))
					return Index == 0 ? CompilerServices.miLoadTmp : CompilerServices.miSetContent;
				if (typeof (TemplatedGroup).IsAssignableFrom (CrowType))
					return Index == 0 ? CompilerServices.miLoadTmp : CompilerServices.miAddItem;
				if (typeof (TemplatedControl).IsAssignableFrom (CrowType))
					return CompilerServices.miLoadTmp;
				return null;
			}
		}

		public static implicit operator string (Node sn)
		{
			return sn.ToString ();
		}
		public override string ToString ()
		{
			return string.Format ("{0}.{1}", CrowType.FullName, Index);
		}
	}
}
