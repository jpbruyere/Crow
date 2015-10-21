using System;
using System.Collections.Generic;
using OpenTK;

namespace go
{
	public class Path : List<Vector2>
	{
		public bool IsClosed;

		public Path () : base()
		{
		}
		public Path (IEnumerable<Vector2> collection) : base(collection){
		}			
	}
}

