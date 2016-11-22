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
	public class NodeAddress : List<Node>
	{
		public NodeAddress (Node[] nodes) : base(nodes) { 
		}

		public Type NodeType { get { return this[this.Count -1].CrowType; }}

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
	}

	public class NamedNodeAddress : NodeAddress {
		public string Name;
		public NamedNodeAddress(string name, Node[] nodes) : base(nodes){
			Name = name;
		}
	}
}
