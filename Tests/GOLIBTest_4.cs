#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;


using go;
using System.Threading;
using System.Collections.Generic;


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
		static int _fps = 0;

		public static int fps {
			get { return _fps; }
			set {
				_fps = value;
				if (_fps > fpsMax)
					fpsMax = _fps;
				else if (_fps < fpsMin)
					fpsMin = _fps;
			}

		}

		public static int fpsMin = int.MaxValue;
		public static int fpsMax = 0;

		static void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		#endregion

		Group c,c2;
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

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			LoadInterface("Interfaces/test4.goml", out c);
			//LoadInterface("golibtests/test4.xml", out c2);
			//c2.HorizontalAlignment = HorizontalAlignment.Left;
			//c2.VerticalAlignment = VerticalAlignment.Top;
			c.Background.AdjustAlpha (0.5);
			labMousePos = c.FindByName ("labMouse") as Label;
			pb = c.FindByName("pbBar") as ProgressBar;
			pb2 = c.FindByName("pbBar2") as ProgressBar;
			labPb = c.FindByName ("labPb") as Label;
			labF = c.FindByName ("labFocus") as Label;
			labA = c.FindByName ("labActive") as Label;
			labH = c.FindByName ("labHover") as Label;
			labFps = c.FindByName ("labFps") as Label;
			labFpsMin = c.FindByName ("labFpsMin") as Label;
			labFpsMax = c.FindByName ("labFpsMax") as Label;
			labV = c.FindByName ("labValue") as Label;
			labUpdate = c.FindByName ("labUpdate") as Label;
			slTest = c.FindByName ("slider") as Slider;
			colors = c.FindByName ("colors") as Group;


			c.MouseMove += pFps_mousemove;
//			slTest.ValueChanged += (object sender, ValueChangeEventArgs vce) => {
//				labV.Text = vce.NewValue.ToString ("00.00");
//			};


			int i = 0;
			foreach (Color col in Color.ColorDic) {
				HorizontalStack s = colors.addChild (new HorizontalStack ());
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
				if (i > 50)
					break;
			}
			ValueChanged.Raise(this, new ValueChangeEventArgs ("TestList", null, TestList));
		}
		void pFps_mousemove(object sender, MouseMoveEventArgs e)
		{
			if (!e.Mouse.IsButtonDown (MouseButton.Left))
				return;
			redrawClip.AddRectangle (c.ScreenCoordinates(c.Slot));
			c.Left += e.XDelta;
			c.Top += e.YDelta;
			c.registerForGraphicUpdate ();
		}
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			GL.Clear (ClearBufferMask.ColorBufferBit);
			base.OnRenderFrame (e);
			SwapBuffers ();
		}
		private int frameCpt = 0;
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);

			fps = (int)RenderFrequency;

			labFps.Text = fps.ToString();
			labUpdate.Text = this.updateTime.ElapsedMilliseconds.ToString() + " ms";
			if (frameCpt > 200) {
				labFpsMin.Text = fpsMin.ToString();
				labFpsMax.Text = fpsMax.ToString();
				resetFps ();
				frameCpt = 0;

			}
			frameCpt++;

			if (pb.Value == pb.Maximum)
				pb.Value = 0;
			pb.Value++;
			pb2.Value = pb.Value;
			labPb.Text = pb.Value.ToString ();
			if (FocusedWidget==null)
				labF.Text = "- none -";
			else
				labF.Text = FocusedWidget.Name;

			if (activeWidget==null)
				labA.Text = "- none -";
			else
				labA.Text = activeWidget.Name;

			if (hoverWidget==null)
				labH.Text = "- none -";
			else
				labH.Text = hoverWidget.Name;
		}
		protected override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);
			labMousePos.Text = e.Position.ToString ();
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