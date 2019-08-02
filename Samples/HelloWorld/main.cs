using Crow;

namespace HelloWorld
{
	class Program : CrowVkWin {
		static void Main (string[] args) {
			using (Program vke = new Program ()) {
				vke.Run ();
			}
		}

		protected override void onLoad ()
		{
			base.onLoad ();

			crow.Load ("#HelloWorld.helloworld.crow");
		}

	}
}
