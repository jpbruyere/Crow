#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using go;
using System.Threading;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace test2
{
	class GOLIBTest_DirViewer : OpenTKGameWindow, IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public void NotifyValueChanged(string name, object value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (name, value));
		}
		#endregion

		public GOLIBTest_DirViewer ()
			: base(1024, 600,"test")
		{}

		public DirContainer CurDir;
		FileDialog dialog;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			CurDir = new DirContainer(new DirectoryInfo ("/home/jp/"));

//			GraphicObject dv = Interface.Load ("Interfaces/testDirViewer.goml");
//			this.AddWidget(dv);
//			dv.DataSource = CurDir;
			dialog = new FileDialog();
			dialog.SearchPattern = ".png|.jpg|.jpeg|.gif|.svg";
			dialog.Show ();

			//LoadInterface("Interfaces/testTypeViewer.goml", out g);
		}
			

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_DirViewer win = new GOLIBTest_DirViewer( )) {
				win.Run (30.0);
			}
		}
	}
	public class DirContainer: IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public void NotifyValueChanged(string name, object value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (name, value));
		}
		#endregion

		public DirectoryInfo CurDir;
		public DirContainer(DirectoryInfo _dir){
			CurDir = _dir;
		}
		public string Name {
			get { return CurDir.Name; }
		}
		public FileSystemInfo[] GetFileSystemInfos
		{
			get {
				List<FileSystemInfo> tmp = CurDir.GetFileSystemInfos ().Where(fi => !fi.Attributes.HasFlag(FileAttributes.Hidden)).ToList();
				if (CurDir.Parent != null)
					tmp.Insert (0, CurDir.Parent);
				return tmp.ToArray ();
			}
		}
		void onDirUp(object sender, MouseButtonEventArgs e)
		{
			
		}
		public void onMouseDown(object sender, MouseButtonEventArgs e)
		{
			Debug.WriteLine (sender.ToString ());
		}
		void OnSelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			CurDir = e.NewValue as DirectoryInfo;
			NotifyValueChanged ("GetFileSystemInfos", GetFileSystemInfos);
			NotifyValueChanged ("Name", Name);

		}
	}

}