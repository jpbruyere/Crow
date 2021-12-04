using System;
using Crow;
using Glfw;
using Samples;

namespace HelloWorld
{
	class Program : Interface {
		Program() : base (800, 600, true) {}
		static void Main (string[] args) {
			using (Program app = new Program ()) {
				//app.Initialized += (sender, e) => app.LoadIMLFragment (@"<Label Text='Hello World' Background='Red' Top='50' Margin='0'/>");
				//app.Initialized += (sender, e) => app.LoadIMLFragment (@"<Window Caption='hello world'/>");
				app.Run ();
			}
		}
		protected override void OnInitialized()
		{
			Load ("/mnt/devel/crow/Samples/HelloWorld/ui/helloworld.crow");
		}
	}
}
