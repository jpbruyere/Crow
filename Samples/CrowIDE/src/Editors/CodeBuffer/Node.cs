using System;
using System.Collections.Generic;

namespace Crow.Coding
{
	public class Node
	{
		public Node Parent;
		public string Name;
		public string Type;
		public CodeLine StartLine;
		public CodeLine EndLine;
		public Dictionary<string,string> Attributes = new Dictionary<string, string> ();

		public List<Node> Children = new List<Node>();

		public Node ()
		{
		}

		public void AddChild (Node child) {
			child.Parent = this;
			Children.Add (child);
		}

		public int Level {
			get { return Parent == null ? 1 : Parent.Level + 1; } 
		}

		public override string ToString ()
		{
			return string.Format ("Name:{0}, Type:{1}\n\tparent:{2}", Name, Type, Parent);
		}
	}
}

