using Crow;

namespace HelloWorld
{
	class Program : Interface {
		static void Main (string[] args) {
			using (Program vke = new Program ()) {
				vke.Load ("#HelloWorld.helloworld.crow");
				vke.Run ();
			}
		}
	}
}
