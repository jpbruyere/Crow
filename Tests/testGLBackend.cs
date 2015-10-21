#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

using go;
using System.Threading;
using GGL;
//using Cairo;
using go.GLBackend;

namespace testGLBackend
{
	class GOLIBTestglb : OpenTKGameWindow
	{
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

		public float coef1 = 1.0f;
		public float coef2 = 0.5f;
		public float coef3 = 0.0f;
		public float coefA = 1.0f;

		Color foreground = Color.Black;
		Color background = Color.White;

		public GOLIBTestglb ()
			: base(1024, 800)
		{
			VSync = VSyncMode.Off;
		}

		#region  scene matrix and vectors
		public static Matrix4 modelview;
		public static Matrix4 projection;

		public static Vector3 vEye = new Vector3(5.0f, 5.0f, 5.0f);    // Camera Position
		public static Vector3 vEyeTarget = Vector3.Zero;
		public static Vector3 vLook = new Vector3(0f, 1f, -0.7f);  // Camera vLook Vector

		float _zFar = 6400.0f;

		public float zFar {
			get { return _zFar; }
			set {
				_zFar = value;
			}
		}

		public float zNear = 0.1f;
		public float fovY = (float)Math.PI / 4;

		float MoveSpeed = 0.5f;
		float RotationSpeed = 0.02f;
		#endregion

		Context ctx;
		Surface surf;

		QuadVAO testQuad;
		QuadVAO magnifierQuad;
		int magTex;	//mignified texture
		go.GLBackend.Shader shade;
		Matrix4 projectionMatrix, 
		modelviewMatrix;
		Texture testTex;


		void drawScene()
		{
			shade.Enable ();
			//shader.LineWidth = lineWidth;
			shade.ProjectionMatrix = projectionMatrix;
			shade.ModelViewMatrix = modelviewMatrix;
			shade.Color = new Vector4(1f,1f,1f,1f);

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, surf.texId);
			//GL.BindTexture(TextureTarget.Texture2D, FontFace.testTexture);

			testQuad.Render (PrimitiveType.TriangleStrip);

			GL.BindTexture(TextureTarget.Texture2D, magTex);
			magnifierQuad.Render (PrimitiveType.TriangleStrip);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			//shader.Disable ();
		}
		glFont testFont;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);


			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);

			GL.Viewport (0, 0, ClientRectangle.Width, ClientRectangle.Height);
			GL.ClearColor(0.0f, 0.0f, 0.2f, 1.0f);

			//GL.Enable(EnableCap.DepthTest);
			//GL.DepthFunc(DepthFunction.Less);
			//GL.Enable(EnableCap.CullFace);
			//GL.Enable(EnableCap.Texture2D);



			//GL.FrontFace(FrontFaceDirection.Ccw);

			//GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			//GL.ShadeModel(ShadingModel.Smooth);
			//GL.Hint (HintTarget.LineSmoothHint, HintMode.Nicest);
			ErrorCode err = GL.GetError ();
			Debug.Assert (err == ErrorCode.NoError, "OpenGL Error");

			surf = new Surface (Format.Argb32, 512, 512);

			testQuad = new QuadVAO (0, 0, 512, 512);
			magnifierQuad = new QuadVAO (150, 300, 400, 400,1.1f,0.73f,0.05f,0.05f);

			shade = new TexturedShader ();

			projectionMatrix = Matrix4.CreateOrthographicOffCenter 
				(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);
			modelviewMatrix = Matrix4.Identity;

			testTex = new Texture (rootDir + @"Images/texture/structures/639-diffuse.jpg");
			//string[] extensions = GL.GetString(StringName.Version).Split(' ');
			FontFace.BuildFontsList(@"/usr/share/fonts/truetype/");
			//glFont.dumpFontsDirectories (@"/usr/share/fonts/");
			//glFont.dumpFontsDirectories (@"/usr/share/fonts/opentype");
			LoadInterface("Interfaces/test0.goml");

			//testFont = new glFont (@"/usr/share/fonts/truetype/droid/DroidSans-Bold.ttf");
		}

		public static void DrawRoundedRectangle(Context gr, double x, double y, double width, double height, double radius)
		{

			if ((radius > height / 2) || (radius > width / 2))
				radius = Math.Min(height / 2, width / 2);

			gr.MoveTo(x, y + radius);
			gr.Arc(x + radius, y + radius, radius, Math.PI, -Math.PI / 2);
			gr.LineTo(x + width - radius, y);
			gr.Arc(x + width - radius, y + radius, radius, -Math.PI / 2, 0);
			gr.LineTo(x + width, y + height - radius);
			gr.Arc(x + width - radius, y + height - radius, radius, 0, Math.PI / 2);
			gr.LineTo(x + radius, y + height);
			gr.Arc(x + radius, y + height - radius, radius, Math.PI / 2, Math.PI);
			gr.ClosePath();
		}
		double angle;
		public override void GLClear ()
		{
			GL.ClearColor(1.0f, 0.0f, 0.2f, 1.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public override void OnRender (FrameEventArgs e)
		{
			drawScene ();
		}
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);

			fps = (int)RenderFrequency;

			using (Context ctx = new Context (surf)) {
				ctx.coef1 = coef1;
				ctx.coef2 = coef2;
				ctx.coef3 = coef3;
				ctx.coefA = coefA;

				ctx.Rectangle(new Rectangle(0,0,512,512));
				ctx.Color = background;//new Color(1.0,0.1,0.2,1);
				ctx.Fill ();
				ctx.LineWidth = 1.0;
				ctx.Color = foreground;
				ctx.SelectFontFace ("courier new", FontSlant.Normal, FontWeight.Normal);
				ctx.SetFontSize (6);
				ctx.FillText ("Test d'une string a petite échelle", new Point (50, 50));
				ctx.SetFontSize (8);
				ctx.FillText ("Test d'une string a petite échelle", new Point (50, 60));
				ctx.SetFontSize (10);
				ctx.FillText ("Test d'une string a petite échelle", new Point (50, 70));
				ctx.SetFontSize (12);
				ctx.FillText ("Test d'une string a petite échelle", new Point (50, 85));
				ctx.SetFontSize (14);
				ctx.FillText ("Test d'une string a petite échelle", new Point (50, 100));

				ctx.FillText ("c1: " + string.Format("{0:##.000}",coef1), new Point (300, 100));
				ctx.FillText ("c2: " + string.Format("{0:##.000}",coef2), new Point (300, 120));
				ctx.FillText ("c3: " + string.Format("{0:##.000}",coef3), new Point (300, 140));
				ctx.FillText ("cA: " + string.Format("{0:##.000}",coefA), new Point (300, 160));

				ctx.Rectangle(new Rectangle(10,10,40,100));
				ctx.Stroke ();
				ctx.SetFontSize (40);
				ctx.FillText ("ceci est un test de string", new Point (2, 200));
				ctx.Fill ();
//				ctx.LineWidth = 1.0;
//				ctx.MoveTo (300, 10);
//				ctx.LineTo (300, 350);
//				ctx.Color = new Color (1.0, 0.5, 0.0, 1.0);
//				ctx.Stroke ();
//				ctx.MoveTo (301, 10);
//				ctx.LineTo (301, 350);
//				ctx.Color = new Color (0.0, 0.5, 1.0, 1.0);
//				ctx.Stroke ();
//				ctx.Rectangle(new Rectangle(100,100,200,200));
				//ctx.Clip ();
//				for (int i = 0; i < 25; i++) {
//					
//					
//					angle += Math.PI / 10000;
//					if (angle > Math.PI * 2)
//						angle = 0;
//
//					
//					ctx.Color = new Color (1.0 / i, 1, 0, 0.1);
//					ctx.Translate (-200, -200);
//					ctx.Rotate (angle);
//					ctx.Translate (200, 200);
//					
//					
//					ctx.Rectangle (new Rectangle (0, 0, 400, 400));
//					DrawRoundedRectangle (ctx, 100, 100, 70, 70, 10);
//					ctx.FillPreserve ();
//					ctx.Color = go.Color.White;
//					ctx.Stroke ();
//				}
				surf.Save (@"/home/jp/surf.png");

				if (GL.IsTexture(magTex))
					GL.DeleteTexture(magTex);

				magTex = ctx.copyCurentSurfaceToTexture ();

//				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
//				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
//
			}

			float inc = 0.01f;
			if (Keyboard [Key.ShiftLeft])
				inc = 0.1f;
			if (Keyboard[Key.Keypad1])
				coef1 -= inc;
			if (Keyboard[Key.Keypad2])
				coef2 -= inc;
			if (Keyboard[Key.Keypad3])
				coef3 -= inc;
			if (Keyboard[Key.Keypad4])
				coef1 = 0f;
			if (Keyboard[Key.Keypad5])
				coef2 = 0f;
			if (Keyboard[Key.Keypad6])
				coef3 = 0f;
			if (Keyboard[Key.Keypad7])
				coef1 += inc;
			if (Keyboard[Key.Keypad8])
				coef2 += inc;
			if (Keyboard[Key.Keypad9])
				coef3 += inc;
			if (Keyboard[Key.KeypadAdd])
				coefA += inc;
			if (Keyboard[Key.KeypadSubtract])
				coefA -= inc;
			//Debug.WriteLine (fps);

		}
		protected override void OnKeyPress (KeyPressEventArgs e)
		{
			base.OnKeyPress (e);
			if (e.KeyChar == 'c') {
				if (background == Color.Black) {
					background = Color.White;
					foreground = Color.Black;
				} else {
					background = Color.Black;
					foreground = Color.White;
				}
			}
		}
		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);
			UpdateViewMatrix();
		}

		#region Mouse Handling
		void Object_Mouse_Move(object sender, MouseMoveEventArgs e){
			if (activeWidget == null)
				return;

			activeWidget.registerClipRect ();
			if (activeWidget!=null) {
				activeWidget.Left += e.XDelta;
				activeWidget.Top += e.YDelta;
			}
		}
		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{
			if (e.XDelta != 0 || e.YDelta != 0)
			{
				if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed)
				{
					Matrix4 m = Matrix4.CreateRotationZ(-e.XDelta * RotationSpeed);
					m *= Matrix4.CreateFromAxisAngle(-vLookPerpendicularOnXYPlane, -e.YDelta * RotationSpeed);
					vEyeTarget = Vector3.Zero;
					vEye = Vector3.Transform(vEye, Matrix4.CreateTranslation(-vEyeTarget) * m * Matrix4.CreateTranslation(vEyeTarget));
					UpdateViewMatrix();
				}
				if (e.Mouse.RightButton == ButtonState.Pressed)
				{

					Matrix4 m = Matrix4.CreateRotationZ(-e.XDelta * RotationSpeed);
					Matrix4 m2 = Matrix4.Rotate(vLookPerpendicularOnXYPlane, e.YDelta * RotationSpeed);

					vEyeTarget = Vector3.Transform(vEyeTarget, Matrix4.CreateTranslation(-vEye) * m * m2 * Matrix4.CreateTranslation(vEye));

					//vLook = Vector3.Transform(vLook, m2);
					UpdateViewMatrix();

				}
			}
		}			
		void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
		{
			float speed = MoveSpeed;
			if (Keyboard[Key.ShiftLeft])
				speed *= 0.1f;
			else if (Keyboard[Key.ControlLeft])
				speed *= 20.0f;

			vLook = Vector3.NormalizeFast(vEye - vEyeTarget);
			vEye -= vLook * e.Delta * speed;
			UpdateViewMatrix();
		}
		#endregion

		#region vLookCalculations
		Vector3 vLookDirOnXYPlane
		{
			get
			{
				Vector3 v = Vector3.NormalizeFast(vEye - vEyeTarget);
				v.Z = 0;
				return v;
			}
		}
		public Vector3 vLookPerpendicularOnXYPlane
		{
			get
			{
				Vector3 vLook = Vector3.NormalizeFast(vEye - vEyeTarget);
				vLook.Z = 0;

				Vector3 vHorizDir = Vector3.Cross(vLook, Vector3.UnitZ);
				return vHorizDir;
			}
		}

		void moveCamera(Vector3 v)
		{
			vEye += v;
			vEyeTarget += v;
		}
		#endregion

		public void UpdateViewMatrix()
		{
//			Rectangle r = this.ClientRectangle;
//			GL.Viewport( r.X, r.Y, r.Width, r.Height);
//			projection = Matrix4.CreatePerspectiveFieldOfView(fovY, r.Width / (float)r.Height, zNear, zFar);
//			GL.MatrixMode(MatrixMode.Projection);
//			GL.LoadIdentity();
//
//			GL.LoadMatrix(ref projection);
//
//			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);
//			GL.MatrixMode(MatrixMode.Modelview);
//			GL.LoadIdentity();
//			GL.LoadMatrix(ref modelview);
		}
		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTestglb win = new GOLIBTestglb( )) {
				win.Run (30.0);
			}
		}
	}
}
//using (Context ctx = new Context (surf)) {
//	//for (int i = 0; i < 150; i++) {
//
//
//	//					angle += Math.PI / 100000;
//	//					if (angle > Math.PI * 2)
//	//						angle = 0;
//	//GL.
//	//drawScene();
//
//	//					ctx.Color = new Color (1.0/i, 1, 0, 1);
//	//				ctx.Translate (-512, -400);
//	//				ctx.Rotate (angle);
//	//				ctx.Translate (512, 400);
//	ctx.LineWidth = 1.0;
//	ctx.Color = go.Color.LightBlue.AdjustAlpha(0.2);			
//	//				ctx.MoveTo (100, 100);
//	//				ctx.LineTo (150, 150);
//	//				ctx.LineTo (300, 100);
//	//				ctx.LineTo (150, 200);
//	//				ctx.LineTo (500, 200);
//	//				ctx.LineTo (400, 100);
//	//				ctx.LineTo (600, 100);
//	//				ctx.LineTo (600, 600);
//	//				ctx.LineTo (200, 600);
//	//				ctx.LineTo (300, 300);
//	//				ctx.LineTo (100, 300);
//	//				//ctx.ClosePath ();
//	//ctx.Rectangle(new go.Rectangle(100,600,60,80));
//
//	DrawRoundedRectangle (ctx, 200, 200, 70, 70,10);
//	//					DrawRoundedRectangle (ctx, 300, 300, 70, 70,5);
//	//					DrawRoundedRectangle (ctx, 400, 400, 70, 70,25);
//	//					DrawRoundedRectangle (ctx, 500, 500, 70, 70,25);
//	//					DrawRoundedRectangle (ctx, 600, 600, 100, 100,25);
//
//
//	//**** concave shape
//
//	//				ctx.LineTo (110, 200);
//	//				ctx.LineTo (400, 200);
//	//				ctx.LineTo (400, 100);
//	//				ctx.LineTo (200, 100);
//	//				ctx.LineTo (200, 50);
//	//				ctx.LineTo (500, 50);
//	//				ctx.LineTo (500, 200);
//	//				ctx.LineTo (400, 300);
//	//				ctx.LineTo (100, 300);
//	//				ctx.LineTo (50, 200);
//	ctx.FillPreserve ();
//	//*****************************
//
//	ctx.Color = go.Color.White;
//	//ctx.Stroke ();
//	//					ctx.LineTo (500, 50);
//	//					ctx.LineTo (750, 500);
//	//					ctx.LineTo (750, 50);
//	//					ctx.MoveTo (100, 100);
//	//					ctx.LineTo (200, 200);
//	//					ctx.LineTo (100, 400);
//	//					ctx.LineTo (300, 400);
//	//					//ctx.LineTo (250, 300);
//	//					ctx.Arc (300, 600, 100, 0, Math.PI);
//	ctx.Stroke ();
//}
////}
