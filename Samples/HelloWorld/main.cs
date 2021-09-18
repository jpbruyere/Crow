using System;
using Crow;
using Samples;

namespace HelloWorld
{
	class Program : SampleBase {
		public CommandGroup CMDTest = new CommandGroup (
			new ActionCommand("Action", ()=> Console.WriteLine ("Action executed"))
		);
		static void Main (string[] args) {
			DbgLogger.IncludedEvents.AddRange ( new DbgEvtType[] {
				DbgEvtType.MouseEnter,
				DbgEvtType.MouseLeave,
				DbgEvtType.WidgetMouseDown,
				DbgEvtType.WidgetMouseUp,
				DbgEvtType.WidgetMouseClick,
				DbgEvtType.HoverWidget
			});
			using (Interface app = new Program ()) {
				app.Initialized += (sender, e) => (sender as Interface).Load ("#HelloWorld.helloworld.crow").DataSource = sender;
				app.Run ();
			}
		}
	}
}
