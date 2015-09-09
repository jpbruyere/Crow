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


namespace test2
{
	class GOLIBTest_DirViewer : OpenTKGameWindow
	{
		public GOLIBTest_DirViewer ()
			: base(1024, 600,"test")
		{}

		public DirContainer CurDir;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			CurDir = new DirContainer(new DirectoryInfo ("/home/jp/"));

			this.AddWidget(Interface.Load ("Interfaces/testDirViewer.goml", CurDir));
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
	public class DirContainer
	{
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
				return CurDir.GetFileSystemInfos ().Where(fi => !fi.Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
			}
		}
		void onDirUp(object sender, MouseButtonEventArgs e)
		{
			
		}
		public void onMouseDown(object sender, MouseButtonEventArgs e)
		{
			Debug.WriteLine (sender.ToString ());
		}
	}

}