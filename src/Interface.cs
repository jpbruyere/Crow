using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

using GLU = OpenTK.Graphics.Glu;
using OpenTK.Input;
using OpenTK;
using Cairo;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace go
{
	public delegate void WidgetEvent (GraphicObject sender);

	public delegate void ButtonWidgetClick (Button sender);
	//public delegate object ValueGetter(Widget sender);


	public static class Interface
	{

		[DllImport ("libgo.so")]
		private static extern void TTY_init ();

		[DllImport ("libgo.so")]
		private static extern void TTY_attend ();

		[DllImport ("libgo.so")]
		private static extern void FB_init (out Framebuffer fb);

		[DllImport ("libgo.so")]					
		public static extern void DEV_init (int largeur, int hauteur);

		[DllImport ("libgo.so")]
		public static extern void DEV_lectureEvenement (out Evenement e);

		[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
		public struct Framebuffer
		{
			public IntPtr ptr;
			public int hauteur;
			public int largeur;
			public int linelength;
		}

		[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
		public struct Evenement
		{
			public int 		type;
			public int		data1;
			public int		data2;
			public int		data3;
			public IntPtr	emetteur;
			public const int MOUSE_DOWN = 0x21;
			public const int MOUSE_UP = 0x22;
			public const int MOUSE_MVT = 0x23;
		}

		public static List<Panel> panels = new List<Panel> ();
		public static Panel activePanel;

        #region cairo contexte
		public static Context ctx;
		public static Surface surf;
		public static byte[] buffer;   //opengl byte buffer for rastering

		public static Surface mouseGraphic;

		public static void createFrameBufferSurface ()
		{


			Framebuffer fb;
			FB_init (out fb);
			surf = new Cairo.ImageSurface (fb.ptr, Format.ARGB32, fb.largeur, fb.hauteur, fb.linelength);
			ctx = new Context (surf);

			DEV_init (fb.largeur, fb.hauteur);
			TTY_init();

			initMouseGraphic ();

		}

		public static void createWin32Surface ()
		{
			IntPtr hdc = Win32.GetDC (IntPtr.Zero);
			surf = new Win32Surface (hdc);
			ctx = new Context (surf);
		}
        public static void createOpenGLSurface()
        {
            int stride = 4 * renderBounds.Width;

            int bmpSize = Math.Abs(stride) * renderBounds.Height;
            buffer = new byte[bmpSize];
            surf = new ImageSurface(buffer, Format.Argb32, renderBounds.Width, renderBounds.Height, stride);
            ctx = new Context(surf);        
        }
		public static void createOpenGLSurface (Rectangle bounds)
		{
			renderBounds = bounds;

            createOpenGLSurface();
		}

		public static void initMouseGraphic ()
		{
			mouseGraphic = new ImageSurface (@"Pointeurs/left_ptr.png");
			//mouseGraphic = new ImageSurface(@"test.png");
			Thread t = new Thread (deviceThread);
			t.Start ();
		}

		public static void deviceThread ()
		{
			while (true) 
			{
				TTY_attend();

				Interface.Evenement e;
				Interface.DEV_lectureEvenement (out e);

				switch (e.type) {
				case Evenement.MOUSE_MVT:
				//erase previouse mouse
//				ctx.Operator = Operator.Clear;
//				ctx.SetSourceSurface(mouseGraphic,mouseX,mouseY);
//				ctx.Paint();
//				ctx.Operator = Operator.Over;
					lock(redrawClip)
					{
						redrawClip.AddRectangle (new Rectangle (mouseX, mouseY, 48, 48));
					}
					mouseX = e.data1;
					mouseY = e.data2;

					break;
				default:
					break;
				}
			}
		}
        #endregion

		//for panel only
		public static Rectangles redrawClip = new Rectangles ();
		public static char decimalSeparator = '.';

        #region Mouse Handling"
		public static bool MouseIsInInterface = false;
		//used only for pannel mouse grabbing, should extend it to other go
		public static bool grabMouse = false;
		public static MouseDevice Mouse;

		public static bool ProcessMousePosition (Point mousePos, Vector2 Delta)
		{
			if (checkMouse (mousePos)) {
				if (Delta != Vector2.Zero) {
					if (Interface.activePanel != null) {
						Interface.activePanel.updateMouseCursor ();
					}


					if (Mouse [MouseButton.Left]) {
						if (Interface.activePanel != null) {
							if (Interface.grabMouse) {
								go.Panel p = Interface.activePanel;
								p.registerForRedraw (); //add previous clip rect
								switch (Interface.activePanel.mouseBorderPosition) {
								case PanelBorderPosition.Top:
									p.y -= (int)Delta.Y;
									p.height += (int)Delta.Y;
									break;
								case PanelBorderPosition.Left:
									p.x -= (int)Delta.X;
									p.width += (int)Delta.X;
									break;
								case PanelBorderPosition.Right:
									p.width -= (int)Delta.X;
									break;
								case PanelBorderPosition.Bottom:
									p.height -= (int)Delta.Y;
									break;
								case PanelBorderPosition.TopLeft:
									p.y -= (int)Delta.Y;
									p.height += (int)Delta.Y;
									p.x -= (int)Delta.X;
									p.width += (int)Delta.X;
									break;
								case PanelBorderPosition.TopRight:
									p.y -= (int)Delta.Y;
									p.height += (int)Delta.Y;
									p.width -= (int)Delta.X;
									break;
								case PanelBorderPosition.BottomLeft:
									p.height -= (int)Delta.Y;
									p.x -= (int)Delta.X;
									p.width += (int)Delta.X;
									break;
								case PanelBorderPosition.BottomRight:
									p.height -= (int)Delta.Y;
									p.width -= (int)Delta.X;
									break;
								case PanelBorderPosition.Moving:
									p.x -= (int)Delta.X;
									p.y -= (int)Delta.Y;
									p.updatePosition ();
									break;
								case PanelBorderPosition.ClientArea:
									break;
								default:
									break;
								}
								p.invalidateLayout ();
							}
						}
					}
				}
				return true;
			} else
				return false;
		}

		public static void ProcessMouseWheel (int delta)
		{
			int step = 15;
			if (activePanel != null)
				activePanel.ProcessMouseWeel (delta * step);
		}

		static bool checkMouse (Point mousePos)
		{
			if (grabMouse)
				return true;
			else if (activePanel != null) {
				if (activePanel.ProcessMousePosition (mousePos))
					return true;
				else
					activePanel = null;
			}

			foreach (Panel p in panels) {
				if (p.isVisible) {
					if (p.ProcessMousePosition (mousePos)) {
						activePanel = p;
						activePanel.putOnTop ();
						Interface.MouseIsInInterface = true;
						return true;
					}
				}
			}
			activePanel = null;

			Interface.MouseIsInInterface = false;
			return false;
		}
        
		public static void Mouse_ButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (!MouseIsInInterface)
				return;

			Point m = e.Position;

			if (activePanel != null) {
				activePanel.putOnTop ();

				Rectangle r = Interface.activePanel.ScreenCoordClientBounds;

				if (r.Contains (m)) {
					grabMouse = false;
				} else {
					grabMouse = true;
				}
				activePanel.ProcessMouseDown (e.Position);
				activePanel.updateMouseCursor ();
			}

			if (hoverWidget != null)
				hoverWidget.ProcessMouseDown (e.Position);
		}

		public static void Mouse_ButtonUp (object sender, MouseButtonEventArgs e)
		{

			grabMouse = false;

			if (activeWidget != null) {
				activeWidget.ProcessMouseUp (e.Position);
			}
		}

        #endregion

        #region keyboard handling
		public static KeyboardDevice Keyboard;
		private static bool _capitalOn = false;

		public static bool capitalOn {
			get {
				return
                    Keyboard [Key.ShiftLeft] || Keyboard [Key.ShiftRight] ?
                        !_capitalOn : _capitalOn;
			}
			set { _capitalOn = value; }
		}

		public static void ProcessKeyboard (Key k)
		{
			switch (k) {
			case Key.CapsLock:
				capitalOn = !capitalOn;
				break;
			}

			if (activeWidget != null)
				activeWidget.ProcessKeyboard (k);
		}
        #endregion




		//used to manage focusable widget like textboxes or buttons
		private static GraphicObject _activeWidget;

		public static GraphicObject activeWidget {
			get { return _activeWidget; }
			set { _activeWidget = value; }
		}

		static GraphicObject _hoverWidget;

		public static GraphicObject hoverWidget {
			get { return _hoverWidget; }
			set {
				if (value == _hoverWidget)
					return;

				_hoverWidget = value;
			}
		}

        static Rectangle _renderBounds = new Rectangle(0, 0, 800, 600);
        public static Rectangle renderBounds
        {
            get { return _renderBounds; }
            set
            {
                _renderBounds = value;
                ctx = null;
                if (surf != null)
                    surf.Dispose();
                createOpenGLSurface();
                foreach (Panel p in panels)
                {
                    p.invalidateLayout();
                }
            }
        }

		public static Panel addPanel (Rectangle _bounds)
		{
			Panel p = new Panel (_bounds);
			panels.Add (p);
			return p;
		}

		public static PanelWithTitle addPanel (Rectangle _bounds, string _title)
		{
			PanelWithTitle p = new PanelWithTitle (_bounds);
			p.title = _title;
			panels.Add (p);
			return p;
		}

		public static int mouseX = 0;
		public static int mouseY = 0;

		public static void update ()
		{



			Stopwatch layoutTime = new Stopwatch ();
			Stopwatch guTime = new Stopwatch ();
			Stopwatch drawingTime = new Stopwatch ();


			Panel[] inversedPanels = new Panel[panels.Count];
			panels.CopyTo (inversedPanels);
			inversedPanels = inversedPanels.Reverse ().ToArray ();

			foreach (Panel p in inversedPanels) {
				if (p.isVisible) {
					layoutTime.Start ();
					p.processkLayouting ();
					layoutTime.Stop ();

                    
				}
			}
            lock(redrawClip)
			{
				if (redrawClip.count > 0) {
					redrawClip.clearAndClip (ctx);
	            

					foreach (Panel p in inversedPanels) {
						if (p.isVisible) {
							drawingTime.Start ();
		                    
							ctx.Save ();
		                    
							if (redrawClip.count > 0) {
		                        

								Rectangle r = p.renderBounds;
								Rectangles clip = redrawClip.intersectingRects (r);                        

								if (clip.count > 0)
									p.cairoDraw (ref ctx, clip);
								//p.processDrawing(ctx);
							}
							drawingTime.Stop ();

							ctx.Restore ();
						}
					}
                    //ctx.SetSourceSurface (mouseGraphic, mouseX, mouseY);
                    //ctx.Paint ();
					ctx.ResetClip ();
					redrawClip.Reset ();
				}

			}

			//Debug.WriteLine("INTERFACE: layouting: {0} ticks \t graphical update {1} ticks \t drawing {2} ticks",
			//    layoutTime.ElapsedTicks,
			//    guTime.ElapsedTicks,
			//    drawingTime.ElapsedTicks);
			Debug.WriteLine ("INTERFACE: layouting: {0} ms \t graphical update {1} ms \t drawing {2} ms",
                layoutTime.ElapsedMilliseconds,
                guTime.ElapsedMilliseconds,
                drawingTime.ElapsedMilliseconds);

		}

		public static void setTeint (float colorScale)
		{
			GL.PixelTransfer (PixelTransferParameter.RedScale, colorScale);
			GL.PixelTransfer (PixelTransferParameter.BlueScale, colorScale);
			GL.PixelTransfer (PixelTransferParameter.GreenScale, colorScale);
		}

		public static byte[] flitY (byte[] source, int stride, int height)
		{
			byte[] bmp = new byte[source.Length];
			source.CopyTo (bmp, 0);

			for (int y = 0; y < height / 2; y++) {
				for (int x = 0; x < stride; x++) {
					byte tmp = bmp [y * stride + x];
					bmp [y * stride + x] = bmp [(height - 1 - y) * stride + x];
					bmp [(height - y - 1) * stride + x] = tmp;
				}
			}
			return bmp;
		}

		public static double min (params double[] arr)
		{
			int minp = 0;
			for (int i = 1; i < arr.Length; i++)
				if (arr [i] < arr [minp])
					minp = i;

			return arr [minp];
		}

		public static void DrawRoundedRectangle (Cairo.Context gr, Rectangle r, double radius)
		{
			DrawRoundedRectangle (gr, r.X, r.Y, r.Width, r.Height, radius);
		}

		public static void DrawCurvedRectangle (Cairo.Context gr, Rectangle r)
		{
			DrawCurvedRectangle (gr, r.X, r.Y, r.Width, r.Height);
		}

		public static void DrawRoundedRectangle (Cairo.Context gr, double x, double y, double width, double height, double radius)
		{
			gr.Save ();

			if ((radius > height / 2) || (radius > width / 2))
				radius = min (height / 2, width / 2);

			gr.MoveTo (x, y + radius);
			gr.Arc (x + radius, y + radius, radius, Math.PI, -Math.PI / 2);
			gr.LineTo (x + width - radius, y);
			gr.Arc (x + width - radius, y + radius, radius, -Math.PI / 2, 0);
			gr.LineTo (x + width, y + height - radius);
			gr.Arc (x + width - radius, y + height - radius, radius, 0, Math.PI / 2);
			gr.LineTo (x + radius, y + height);
			gr.Arc (x + radius, y + height - radius, radius, Math.PI / 2, Math.PI);
			gr.ClosePath ();
			gr.Restore ();
		}

		public static void DrawCurvedRectangle (Cairo.Context gr, double x, double y, double width, double height)
		{
			gr.Save ();
			gr.MoveTo (x, y + height / 2);
			gr.CurveTo (x, y, x, y, x + width / 2, y);
			gr.CurveTo (x + width, y, x + width, y, x + width, y + height / 2);
			gr.CurveTo (x + width, y + height, x + width, y + height, x + width / 2, y + height);
			gr.CurveTo (x, y + height, x, y + height, x, y + height / 2);
			gr.Restore ();
		}

		public static void StrokeRaisedRectangle (Cairo.Context gr, Rectangle r, double width = 1)
		{
			gr.Save ();
			r.Inflate ((int)-width / 2, (int)-width / 2);
			gr.LineWidth = width;
			gr.Color = Color.White;
			gr.MoveTo (r.BottomLeft);
			gr.LineTo (r.TopLeft);
			gr.LineTo (r.TopRight);
			gr.Stroke ();

			gr.Color = Color.DarkGray;
			gr.MoveTo (r.TopRight);
			gr.LineTo (r.BottomRight);
			gr.LineTo (r.BottomLeft);
			gr.Stroke ();

			gr.Restore ();
		}

		public static void StrokeLoweredRectangle (Cairo.Context gr, Rectangle r, double width = 1)
		{
			gr.Save ();
			r.Inflate ((int)-width / 2, (int)-width / 2);
			gr.LineWidth = width;
			gr.Color = Color.DarkGray;
			gr.MoveTo (r.BottomLeft);
			gr.LineTo (r.TopLeft);
			gr.LineTo (r.TopRight);
			gr.Stroke ();
			gr.Color = Color.White;
			gr.MoveTo (r.TopRight);
			gr.LineTo (r.BottomRight);
			gr.LineTo (r.BottomLeft);
			gr.Stroke ();

			gr.Restore ();
		}
	}


}
