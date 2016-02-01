using System;
using OpenTK.Input;

namespace Crow
{
	public class BubblingMouseButtonEventArg: MouseButtonEventArgs
	{
		public GraphicObject Focused;
		public BubblingMouseButtonEventArg(MouseButtonEventArgs mbe) : base(mbe){
			
		}
	}
}

