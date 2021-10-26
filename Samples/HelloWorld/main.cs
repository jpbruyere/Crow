using System;
using Crow;
using Samples;

namespace HelloWorld
{
	class Program {
		static void Main (string[] args) {
			using (Interface app = new Interface ()) {
				app.Initialized += (sender, e) => app.LoadIMLFragment (@"<Label Text='Hello World'/>");
				app.Run ();
			}
		}
	}
}
