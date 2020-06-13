// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;

namespace Crow.IML
{
	public class NodeStack : Stack<Node>
	{
		public void IncrementCurrentNodeIndex(){
			Node n = this.Pop();
			this.Push (new Node (n.CrowType, n.Index + 1));
		}
		public void DecrementCurrentNodeIndex(){
			Node n = this.Pop();
			this.Push (new Node (n.CrowType, n.Index - 1));
		}
		public void ResetCurrentNodeIndex(){			
			this.Push (new Node (this.Pop().CrowType));
		}
	}
}

