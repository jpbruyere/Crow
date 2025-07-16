using System;
using System.Collections.Generic;
using Crow;
using Glfw;
using Samples;

namespace DockTests
{
	class Program : Interface {
		Program() : base (1400, 1000, true) {}
		static void Main (string[] args) {
			//Interface.PreferedBackendType = Drawing2D.BackendType.Egl;
			using (Program app = new Program ()) {
				//app.Initialized += (sender, e) => app.LoadIMLFragment (@"<Label Text='Hello World' Background='Red' Top='50' Margin='0'/>");
				//app.Initialized += (sender, e) => app.LoadIMLFragment (@"<Container Width='Stretched' ><Window Caption='hello world' Background='Jet'/></Container>");
				//app.Initialized += (sender, e) => app.Load("#ui.helloworld.crow");
				app.Run ();

				//DbgLogger.Save(app);
			}
		}
		public Command CMDNewDockWin;
		protected DockStack mainDock;

		static int winCpt = 5;
		Random rnd = new Random();

		int rndColorComp => (int)(25.5f * rnd.NextInt64(10));
		Drawing2D.Color randomColor => new Drawing2D.Color(rndColorComp, rndColorComp, rndColorComp);

        protected override void OnInitialized()
        {
            base.OnInitialized();

			CMDNewDockWin = new ActionCommand("New Dock Win", ()=>LoadIMLFragment(
				$"<DockWindow Name='win{winCpt++}' Style='SimpleDockWin' Background='{randomColor}' Width='200' Height='200' MinimumSize='20,20'/> ") );

			Widget w = Load ("#ui.main.crow");
			w.DataSource = this;

			mainDock = w.FindByName ("mainDock") as DockStack;


			LoadIMLFragment(@"<Window Background='Jet'><ListBox Style='ScrollingListBox' Data='{List2}' Height='95%' Width='95%' Background='Black' Foreground='Blue'/></Window>").DataSource = this;
			LoadIMLFragment(@"<Window Background='Onyx'/>");

			
		}
		public IEnumerable<String> List2 = new List<string>(new string[]
			{
				"string1",
				"string2",
				"string3",
				"string4",
				"string5",
				"string6",
				"string7",
				"string8",
				"string8",
				"string8",
				"string8",
				"string8",
				"string8",
				"string9"
			}
		);		
	}
}
