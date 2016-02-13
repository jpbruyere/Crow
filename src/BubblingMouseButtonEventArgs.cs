using System;

namespace Crow
{
	public class BubblingMouseButtonEventArg: MouseButtonEventArgs
	{
		public GraphicObject Focused;
		public BubblingMouseButtonEventArg(MouseButtonEventArgs mbe) : base(mbe){
			
		}
	}
}

