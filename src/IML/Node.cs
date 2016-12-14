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
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Crow.IML
{
	/// <summary>
	/// IML Node are defined with a type and the index in the parent,
	/// </summary>
	public struct Node
	{
		/// <summary> Current node type</summary>
		public Type CrowType;
		/// <summary> Index in parent, -1 for template</summary>
		public int Index;

		public Node (Type crowType, int _index = 0)
		{
			CrowType = crowType;
			Index = _index;
		}

		public MethodInfo GetAddMethod(int childIdx){
			if (typeof (Group).IsAssignableFrom (CrowType))
				return CompilerServices.miAddChild;
			if (typeof (Container).IsAssignableFrom (CrowType))
				return CompilerServices.miSetChild;
			if (typeof (TemplatedContainer).IsAssignableFrom (CrowType))
				return childIdx < 0 ? CompilerServices.miLoadTmp : CompilerServices.miSetContent;
			if (typeof (TemplatedGroup).IsAssignableFrom (CrowType))
				return childIdx < 0 ? CompilerServices.miLoadTmp : CompilerServices.miAddItem;
			if (typeof (TemplatedControl).IsAssignableFrom (CrowType))
				return CompilerServices.miLoadTmp;
			return null;
		}

		#region Equality Compare
		public override bool Equals (object obj)
		{
			if (obj == null) 
				return false;			
			Node n = (Node)obj;
			return CrowType == n.CrowType && Index == n.Index;
		}
		public override int GetHashCode ()
		{
			return CrowType.GetHashCode () ^ Index.GetHashCode ();
		}
		#endregion

		public bool HasTemplate {
			get { return typeof (TemplatedControl).IsAssignableFrom (CrowType);}
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
