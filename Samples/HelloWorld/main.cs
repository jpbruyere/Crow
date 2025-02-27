using System;
using System.Collections.Generic;
using Crow;
using Glfw;
using Samples;

namespace HelloWorld
{
	class Program : Interface {
		Program() : base (800, 600, true) {}
		static void Main (string[] args) {
			//Interface.PreferedBackendType = Drawing2D.BackendType.Egl;
			using (Program app = new Program ()) {
				//app.Initialized += (sender, e) => app.LoadIMLFragment (@"<Label Text='Hello World' Background='Red' Top='50' Margin='0'/>");
				//app.Initialized += (sender, e) => app.LoadIMLFragment (@"<Container Width='Stretched' ><Window Caption='hello world' Background='Jet'/></Container>");
				app.Initialized += (sender, e) => app.Load("#ui.helloworld.crow");
				app.Run ();

				DbgLogger.Save(app);
			}
		}
	}
}
