

using Crow;

namespace DebugLogAnalyzer {
	public static class Extensions {

		public static CommandGroup GetCommands (this System.IO.DirectoryInfo di) =>
			new CommandGroup(
				new Command ("Set as root", ()=> {Program.CurrentProgramInstance.CurrentDir = di.FullName;})				
			);		
		public static CommandGroup GetCommands (this System.IO.FileInfo fi) =>
			new CommandGroup(
				new Command ("Delete", (sender0) => {
					MessageBox.ShowModal (Program.CurrentProgramInstance, MessageBox.Type.YesNo, $"Delete {fi.Name}?").Yes += (sender, e) => {
						System.IO.File.Delete(fi.FullName);
						Widget listContainer = ((sender0 as Widget).LogicalParent as Widget).DataSource as Widget;
						(listContainer.Parent as Group).RemoveChild(listContainer);
					};
				})
			);		

	}
}