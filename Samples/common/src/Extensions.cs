

using System.Reflection;
using Crow;

namespace Samples {
	public static class Extensions {

		public static CommandGroup GetCommands (this System.IO.DirectoryInfo di) =>
			new CommandGroup(
				new ActionCommand ("Set as root", ()=> {SampleBaseForEditor.CurrentProgramInstance.CurrentDir = di.FullName;})
			);
		public static CommandGroup GetCommands (this System.IO.FileInfo fi) =>
			new CommandGroup(
				new ActionCommand ("Delete", (sender0) => {
					MessageBox.ShowModal (SampleBaseForEditor.CurrentProgramInstance, MessageBox.Type.YesNo, $"Delete {fi.Name}?").Yes += (sender, e) => {
						System.IO.File.Delete(fi.FullName);
						Widget listContainer = ((sender0 as Widget).LogicalParent as Widget).DataSource as Widget;
						(listContainer.Parent as Group).RemoveChild(listContainer);
					};
				})
			);
		public static Picture GetIcon (this MemberInfo mi)
			=> mi is EventInfo ? new BmpPicture("#Icons.event.png") : new BmpPicture("#Icons.property.png");

	}
}