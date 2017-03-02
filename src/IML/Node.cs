//
// Node.cs
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
		public Node (Type crowType, int _index = 0)
		{
			CrowType = crowType;
			Index = _index;
		}
		#endregion

		/// <summary> Current node type</summary>
		public Type CrowType;
		/// <summary> Index in parent, -1 for template</summary>
		public int Index;

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
