using System;
using Crow;
using Samples;

namespace HelloWorld
{
	class Program : SampleBase {
		static void Main (string[] args) {
			using (Interface app = new Program ()) {
				app.Initialized += (sender, e) => (sender as Interface).Load ("#HelloWorld.helloworld.crow").DataSource = sender;
				app.Run ();
			}
		}
	}
}
