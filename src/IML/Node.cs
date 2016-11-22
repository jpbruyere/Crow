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
		public MethodInfo AddMethod {
			get {
				if (typeof (Group).IsAssignableFrom (CrowType))
					return CompilerServices.miAddChild;
				if (typeof (Container).IsAssignableFrom (CrowType))
					return CompilerServices.miSetChild;
				if (typeof (TemplatedContainer).IsAssignableFrom (CrowType))
					return Index < 0 ? CompilerServices.miLoadTmp : CompilerServices.miSetContent;
				if (typeof (TemplatedGroup).IsAssignableFrom (CrowType))
					return Index < 0 ? CompilerServices.miLoadTmp : CompilerServices.miAddItem;
				if (typeof (TemplatedControl).IsAssignableFrom (CrowType))
					return CompilerServices.miLoadTmp;
				return null;
			}
		}
		public void EmitGetInstance (ILGenerator il){
			if (typeof (Group).IsAssignableFrom (CrowType)) {
				il.Emit (OpCodes.Ldfld, typeof(Group).GetField ("children", BindingFlags.Instance | BindingFlags.NonPublic));
				il.Emit(OpCodes.Ldc_I4, Index);
				il.Emit (OpCodes.Callvirt, typeof(List<GraphicObject>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
				return;
			}
			if (typeof(Container).IsAssignableFrom (CrowType) || Index < 0) {
				il.Emit (OpCodes.Ldfld, typeof(PrivateContainer).GetField ("child", BindingFlags.Instance | BindingFlags.NonPublic));
				return;
			}
			if (typeof(TemplatedContainer).IsAssignableFrom (CrowType)) {
				il.Emit (OpCodes.Callvirt, typeof(TemplatedContainer).GetProperty ("Content").GetGetMethod ());
				return;
			}
			if (typeof(TemplatedGroup).IsAssignableFrom (CrowType)) {
				il.Emit (OpCodes.Callvirt, typeof(TemplatedGroup).GetProperty ("Items").GetGetMethod ());
				il.Emit(OpCodes.Ldc_I4, Index);
				il.Emit (OpCodes.Callvirt, typeof(List<GraphicObject>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
				return;
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
