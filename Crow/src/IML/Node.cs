// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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
		#region CTOR
		public Node (Type crowType, int _index = 0, Type dsType = null)
		{
			CrowType = crowType;
			Index = _index;
			DataSourceType = dsType;
		}
		#endregion

		/// <summary> Current node type</summary>
		public readonly Type CrowType;
		/// <summary> Index in parent, -1 for template</summary>
		public readonly int Index;
		/// <summary>
		/// DataSourceType attribute if set
		/// </summary>
		public Type DataSourceType;


		/// <summary>
		/// retrieve the child addition method depending on the type of this node
		/// </summary>
		/// <returns>The child addition method</returns>
		/// <param name="childIdx">child index or, template root node has index == -1</param>
		public MethodInfo GetAddMethod(int childIdx){
			if (typeof (Group).IsAssignableFrom (CrowType))
				return CompilerServices.miAddChild;
			if (typeof (Container).IsAssignableFrom (CrowType))
				return CompilerServices.miSetChild;
			if (typeof (TemplatedContainer).IsAssignableFrom (CrowType))
				return childIdx < 0 ? CompilerServices.miLoadTmp : CompilerServices.piContent.GetSetMethod();
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
		public bool IsTemplatedGroup {
			get { return typeof (TemplatedGroup).IsAssignableFrom (CrowType);}
		}
		public bool HasDataSourceType {
			get { return DataSourceType != null; }
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
