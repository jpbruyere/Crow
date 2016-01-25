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
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace test
{
	public class ClsItem
	{
//		#region IValueChange implementation
//
//		public event EventHandler<ValueChangeEventArgs> ValueChanged;
//
//		#endregion

		public string field;

		public string Field {
			get {
				return field;
			}
			set {
				field = value;
				//ValueChanged.Raise(this, new ValueChangeEventArgs ("Field", null, field));
			}
		}

		public ClsItem(){
		}
		public ClsItem(string str){
			Field = str;
		}
	}
	
	class GOLIBTest_Listbox : OpenTKGameWindow, IValueChange
	{
		#region IValueChange implementation

		public event EventHandler<ValueChangeEventArgs> ValueChanged;

		#endregion
	
		public GOLIBTest_Listbox ()
			: base(1024, 600,"test")
		{}

//		public List<ClsItem> TestList = new List<ClsItem>(new ClsItem[] 
//			{
//				new ClsItem("string 1"),
//				new ClsItem("string 2"),
//				new ClsItem("string 3")
//			});
		public List<string> TestList;/* = new List<string>( new string[] 
			{
				"string 1",
				"string 2",
				"string 3"
			});	*/

		public String Hover {
			get { return hoverWidget == null ? "None" : hoverWidget.ToString(); }
		}
		Point mPos;
		public string MousePos {
			get { return mPos.ToString(); }
		}
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			TestList = Directory.GetFileSystemEntries("/home/jp/tmp/mtgdata/a", "*.txt",SearchOption.AllDirectories).ToList();
			GraphicObject tlb = LoadInterface("Interfaces/test_Listbox.goml");
			tlb.DataSource = this;

//			TestList [1].Field = "test string";
//			ValueChanged.Raise(this, new ValueChangeEventArgs ("TestList", TestList));
		}
		protected override void OnMouseMove (MouseMoveEventArgs e)
		{			
			base.OnMouseMove (e);
			ValueChanged.Raise (this, new ValueChangeEventArgs ("Hover", Hover));
			ValueChanged.Raise (this, new ValueChangeEventArgs ("MousePos", e.Position.ToString()));
		}

		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);
		}
		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{
			TestList.Add ("newly added list item");
			ValueChanged.Raise(this, new ValueChangeEventArgs ("TestList", TestList));
		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_Listbox win = new GOLIBTest_Listbox( )) {
				win.Run (30.0);
			}
		}
	}
}