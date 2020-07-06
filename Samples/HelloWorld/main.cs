using System;
using Crow;

namespace HelloWorld
{
	class Program {
		static void Main (string[] args) {
#if NETCOREAPP3_1
			DllMapCore.Resolve.Enable (true);
#endif
			using (Interface app = new Interface ()) {
				app.Initialized += (sender, e) => (sender as Interface).Load ("#HelloWorld.helloworld.crow");
				app.Run ();
			}
		}
	}
}
