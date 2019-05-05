using System.IO;
using Crow;

namespace HelloWorld
{


	class Program : CrowVkWin
	{
		static void Main (string[] args) {
			using (Program vke = new Program ()) {
				vke.Run ();
			}
		}

		Container crowContainer;
		protected override void onLoad ()
		{
			Widget w = crow.Load ("#ShowCase.showcase.crow");
			w.DataSource = this;
			crowContainer = w.FindByName ("CrowContainer") as Container;
			crowContainer.DataSource = new object ();
			hideError ();
		}

		void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			FileSystemInfo fi = e.NewValue as FileSystemInfo;
			if (fi == null)
				return;
			if (fi is DirectoryInfo)
				return;
			hideError ();
			lock (crow.UpdateMutex) {
				try {
					Widget g = crow.Load (fi.FullName);
					crowContainer.SetChild (g);
					g.DataSource = this;
				} catch (Crow.IML.InstantiatorException ex) {
					showError (ex);
				}
			}

			string source = "";
			using (Stream s = new FileStream (fi.FullName, FileMode.Open)) {
				using (StreamReader sr = new StreamReader (s)) {
					source = sr.ReadToEnd ();
				}
			}
			NotifyValueChanged ("source", source);
		}

		void showError (Crow.IML.InstantiatorException ex)
		{
			NotifyValueChanged ("ErrorMessage", ex.Path + ": " + ex.InnerException.Message);
			NotifyValueChanged ("ShowError", true);
		}
		void hideError ()
		{
			NotifyValueChanged ("ShowError", false);
		}
	}
}