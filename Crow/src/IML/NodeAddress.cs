//
// NodeAddress.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Crow.IML
{
	/// <summary>
	/// Node address is a list of nodes from root to leaf defining a unique node
	/// </summary>
	public class NodeAddress : List<Node>
	{
		#region CTOR
		public NodeAddress (Node[] nodes) : base(nodes) {}
		#endregion

		public Type NodeType { get { return Count == 0 ? null : this[this.Count -1].CrowType; }}

		/// <summary>
		/// Gets the node adress from binding expression starting from this node
		/// and return in expression remaining part
		/// </summary>
		public NodeAddress ResolveExpression (ref string expression){
			int ptr = this.Count - 1;
			string[] splitedExp = expression.Split ('/');

			if (splitedExp.Length < 2)//dataSource binding
				return null;

			if (!string.IsNullOrEmpty (splitedExp [0])) {//else bind on current node
				//return new NodeAddress (this.Take(ptr+1).ToArray());
				if (splitedExp [0] == ".") {//search template root
					ptr--;
					while (ptr >= 0) {
						if (typeof(TemplatedControl).IsAssignableFrom (this [ptr].CrowType))
							break;
						ptr--;
					}
				} else if (splitedExp [0] == "..") { //search starting at current node
					int levelUp = splitedExp.Length - 1;
					if (levelUp > ptr + 1)
						throw new Exception ("Binding error: try to bind outside IML source");
					ptr -= levelUp;
				}
			}
			expression = splitedExp [splitedExp.Length - 1];
			//TODO:change Template special address identified with Nodecount = 0 to something not using array count to 0,
			//here linq is working without limits checking in compile option
			//but defining a 0 capacity array with limits cheking enabled, cause 'out of memory' error
			return new NodeAddress (this.Take(ptr+1).ToArray());//[ptr+1];
			//Array.Copy (sourceAddr.ToArray (), targetNode, ptr + 1);
			//return new NodeAddress (targetNode);
		}
		/// <summary>
		/// get BindingDefinition from binding expression
		/// </summary>
		public BindingDefinition GetBindingDef(string sourceMember, string expression){
			BindingDefinition bindingDef = new BindingDefinition(this, sourceMember);
			if (string.IsNullOrEmpty (expression)) {
				return bindingDef;
			} else {
				if (expression.StartsWith ("²", StringComparison.Ordinal)) {
					bindingDef.TwoWay = true;
					expression = expression.Substring (1);
				}

				string exp = expression;
				bindingDef.TargetNA = this.ResolveExpression (ref exp);

				string [] bindTrg = exp.Split ('.');

				if (bindTrg.Length == 0)
					throw new Exception ("invalid binding expression: " + expression);
				if (bindTrg.Length == 1)
					bindingDef.TargetMember = bindTrg [0];
				else {
					if (!string.IsNullOrEmpty(bindTrg[0]))//searchByName
						bindingDef.TargetName = bindTrg[0];

					bindingDef.TargetMember = exp.Substring (bindTrg[0].Length + 1);
				}
			}

			return bindingDef;
		}

		#region Object overrides
		public override bool Equals (object obj)
		{
			if (obj == null) 
				return false;
			
			NodeAddress na = (NodeAddress)obj;
			return this.SequenceEqual (na);
		}
		public override int GetHashCode ()
		{
			unchecked {
				int hash = 19;
				foreach (Node n in this)
					hash = hash * 31 + (n == null ? 0 : n.GetHashCode ());	
				return hash;
			}
		}
		public override string ToString ()
		{
			string tmp = "";
			foreach (Node n in this)
				tmp += string.Format ("{0};", n.Index);			
			return tmp;
		}
		#endregion
	}

	public class NamedNodeAddress : NodeAddress {
		public string Name;
		public NamedNodeAddress(string name, Node[] nodes) : base(nodes){
			Name = name;
		}
	}
}
