//
//  Context.cs
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
using System.Reflection.Emit;

namespace Crow.IML
{
	/// <summary>
	/// Context while parsing IML
	/// </summary>
	public class Context
	{
		public ILGenerator il = null;
		//public SubNodeType curSubNodeType;
		public Stack<Node> nodesStack = new Stack<Node> ();

		public Dictionary<string, string> Names = new Dictionary<string, string> ();
		public Dictionary<string, Dictionary<string, MemberAddress>> PropertyBindings = new Dictionary<string, Dictionary<string, MemberAddress>> ();

		public Context ()
		{
		}
	}
}