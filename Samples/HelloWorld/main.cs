using System;
using Crow;
using Glfw;
using Samples;

namespace HelloWorld
{
	class Program : Interface {
		Program() : base (800, 600, true, true) {}
		static void Main (string[] args) {
			using (Program app = new Program ()) {
				app.Initialized += (sender, e) => app.LoadIMLFragment (@"<Window Caption='Hello World'/>");
				app.Run ();
			}
		}
	}
}
