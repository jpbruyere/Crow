#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;


using Crow;
using System.Threading;
using System.Collections.Generic;
using System.IO;


namespace test
{

	class GOLIBTest_4 : OpenTKGameWindow, IValueChange
	{
		#region IValueChange implementation

		public event EventHandler<ValueChangeEventArgs> ValueChanged;

		#endregion

		public GOLIBTest_4 ()
			: base(1024, 800,"test4")
		{}


		#region FPS
		int _fps = 0;

		public int fps {
			get { return _fps; }
			set {
				if (_fps == value)
					return;

				_fps = value;

				if (_fps > fpsMax) {
					fpsMax = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMax", fpsMax));
				} else if (_fps < fpsMin) {
					fpsMin = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMin", fpsMin));
				}

				ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
				ValueChanged.Raise (this, new ValueChangeEventArgs ("update",
					this.updateTime.ElapsedMilliseconds.ToString () + " ms"));
			}
		}

		public int fpsMin = int.MaxValue;
		public int fpsMax = 0;

		void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		public string update = "";
		#endregion


		GraphicObject c;
		ProgressBar pb, pb2;
		Label labMousePos, labPb, labF, labA, labH, labFps, labFpsMin, labFpsMax, labV,
				labUpdate;
		Slider slTest;
		Group colors;

		public List<ClsItem> TestList = new List<ClsItem>(new ClsItem[] 
			{
			new ClsItem("string 1"),
			new ClsItem("string 2")
			});
		int i = 0;
		Color[] colorsArray;
		volatile List<GraphicObject> loadedCols = new List<GraphicObject>();
		volatile bool allColsLoaded = false;
		private static readonly object mutex = new object();

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			c = LoadInterface("Interfaces/test4.goml");
			//LoadInterface("golibtests/test4.xml", out c2);
			//c2.HorizontalAlignment = HorizontalAlignment.Left;
			//c2.VerticalAlignment = VerticalAlignment.Top;
			c.Background.AdjustAlpha (0.5);
//			labMousePos = c.FindByName ("labMouse") as Label;
//			//pb = c.FindByName("pbBar") as ProgressBar;
//			pb2 = c.FindByName("pbBar2") as ProgressBar;
//			labPb = c.FindByName ("labPb") as Label;
			labF = c.FindByName ("labFocus") as Label;
			labA = c.FindByName ("labActive") as Label;
			labH = c.FindByName ("labHover") as Label;
//			labFps = c.FindByName ("labFps") as Label;
//			labFpsMin = c.FindByName ("labFpsMin") as Label;
//			labFpsMax = c.FindByName ("labFpsMax") as Label;
//			labV = c.FindByName ("labValue") as Label;
//			labUpdate = c.FindByName ("labUpdate") as Label;
//			slTest = c.FindByName ("slider") as Slider;
			colors = c.FindByName ("colors") as Group;


			c.MouseMove += pFps_mousemove;
//			slTest.ValueChanged += (object sender, ValueChangeEventArgs vce) => {
//				labV.Text = vce.NewValue.ToString ("00.00");
//			};


			colorsArray = Color.ColorDic.ToArray ();

			Thread t = new Thread (loadingThread);
			t.Start ();

//			ValueChanged.Raise(this, new ValueChangeEventArgs ("TestList", TestList));
		}

		void loadingThread()
		{			
			foreach (Color col in colorsArray) {
				HorizontalStack s = new HorizontalStack () { Fit = true};
				s.HorizontalAlignment = HorizontalAlignment.Left;
				Border b = new Border () {
					Bounds = new Size (32, 20),
					CornerRadius = 5,
					Background = col,
					BorderWidth = 2,
					BorderColor = Color.Transparent,
					Focusable = true
				};
				b.MouseEnter += delegate(object sender, MouseMoveEventArgs ee) {
					(sender as Border).BorderColor = Color.White;
				};
				b.MouseLeave += delegate(object sender, MouseMoveEventArgs ee) {
					(sender as Border).BorderColor = Color.Transparent;
				};
				s.addChild (b);

				s.addChild (
					new Label (col.ToString ()){
						Bounds=new Rectangle(0,0,-1,-1),
					}
				);
				i++;

				while(true){
					lock (mutex) {
						loadedCols.Add (s);
						break;
					}
				}

				Thread.Sleep (10);
				
			}	
			allColsLoaded = true;
		}
		void pFps_mousemove(object sender, MouseMoveEventArgs e)
		{
			if (!e.Mouse.IsButtonDown (MouseButton.Left)||sender!=c)
				return;
			redrawClip.AddRectangle (c.ScreenCoordinates(c.Slot));
			c.Left += e.XDelta;
			c.Top += e.YDelta;
			c.registerForGraphicUpdate ();
		}
		void onButClick(object send, MouseButtonEventArgs e)
		{
			Color col = Color.ColorDic.ToArray () [i];
			HorizontalStack s = colors.addChild (new HorizontalStack () { Fit = true});
			s.HorizontalAlignment = HorizontalAlignment.Left;
			Border b = new Border () {
				Bounds = new Size (32, 20),
				CornerRadius = 5,
				Background = col,
				BorderWidth = 2,
				BorderColor = Color.Transparent,
				Focusable = true
			};
			b.MouseEnter += delegate(object sender, MouseMoveEventArgs ee) {
				(sender as Border).BorderColor = Color.White;
			};
			b.MouseLeave += delegate(object sender, MouseMoveEventArgs ee) {
				(sender as Border).BorderColor = Color.Transparent;
			};
			s.addChild (b);

			s.addChild (
				new Label (col.ToString ()){
					Bounds=new Rectangle(0,0,-1,-1),
				}
			);
			i++;
		}
		private int frameCpt = 0;
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);

			fps = (int)RenderFrequency;

			lock (mutex) {
				if (loadedCols.Count > 50 || allColsLoaded) {
					while (loadedCols.Count > 0) {
						colors.addChild (loadedCols[0]);
						loadedCols.RemoveAt (0);
					}
				}
			}

			if (frameCpt > 200) {
				resetFps ();
				frameCpt = 0;
			}
			frameCpt++;


//			if (FocusedWidget==null)
//				labF.Text = "- none -";
//			else
//				labF.Text = FocusedWidget.Name;
//
//			if (activeWidget==null)
//				labA.Text = "- none -";
//			else
//				labA.Text = activeWidget.Name;
//
//			if (hoverWidget==null)
//				labH.Text = "- none -";
//			else
//				labH.Text = hoverWidget.Name;
		}
		//public Point MousePosition;

		protected override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);
			//MousePosition = e.Position;
			ValueChanged.Raise(this, new ValueChangeEventArgs ("MousePosition", e.Position.ToString()));
		}
		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_4 win = new GOLIBTest_4( )) {
				win.Run (60.0);
			}
		}
	}
}