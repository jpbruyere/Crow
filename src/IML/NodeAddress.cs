//
//  NodeAddress.cs
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

			if (string.IsNullOrEmpty (splitedExp [0]) || splitedExp [0] == ".") {//search template root
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
				if (expression.StartsWith ("²")) {
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
