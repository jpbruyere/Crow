using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace go
{
    public static class Mouse2d
    {
		public static Point position;

		public static int X
		{
			get { return position.X; }
			set { position.X = value; }
		}
		public static int Y
		{
			get { return position.Y; }
			set { position.Y = value; }
		}
        
    }
}
