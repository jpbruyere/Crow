using System;
using Crow;

namespace HelloWorld
{
	class Program {
		static void Main (string[] args) {
			using (Interface app = new Interface ()) {
				app.Initialized += (sender, e) => (sender as Interface).Load ("#HelloWorld.helloworld.crow");
				app.Run ();
			}
		}
	}
}
