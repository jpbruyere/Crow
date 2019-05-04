using Crow;

namespace HelloWorld
{
	class Program : CrowVkWin {
		static void Main (string[] args) {
			using (Program vke = new Program ()) {
				vke.crow.Load ("#HelloWorld.helloworld.crow").DataSource = vke;
				vke.crow.LoadIMLFragment ("<FileDialog />");

				vke.Run ();
			}
		}

	}
}