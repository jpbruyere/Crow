#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using Crow;
using System.Threading;
using GGL;
using System.Collections.Generic;
using System.IO;


namespace Opuz2015
{
	public enum GameState { Init, CutStart, CutFinished, Play, Finished};

	class MainWin : OpenTKGameWindow, IBindable
	{
		#region IBindable implementation
		List<Binding> bindings = new List<Binding> ();
		public List<Binding> Bindings {
			get { return bindings; }
		}
		#endregion

		#region  scene matrix and vectors
		public static Matrix4 modelview;
		public static Matrix4 projection;
		public static int[] viewport = new int[4];

		public float EyeDist { 
			get { return eyeDist; } 
			set { 
				eyeDist = value; 
				UpdateViewMatrix ();
			} 
		}
		public Vector3 vEyeTarget = new Vector3(0, 0, 0f);
		public Vector3 vEye;
		public Vector3 vLookInit = Vector3.Normalize(new Vector3(0.0f, -0.7f, 0.7f));
		public Vector3 vLook = Vector3.Normalize(new Vector3(0.0f, -0.1f, 0.9f));  // Camera vLook Vector
		public float zFar = 6000.0f;
		public float zNear = 1.0f;
		public float fovY = (float)Math.PI / 4;

		float eyeDist = 1000f;
		float eyeDistTarget = 1000f;
		float MoveSpeed = 100.0f;
		float RotationSpeed = 0.02f;
		float ZoomSpeed = 2f;
		float viewZangle, viewXangle;

		public Vector4 vLight = new Vector4 (0.5f, 0.5f, -1f, 0f);
		#endregion

		#region GL
		public static PuzzleShader mainShader;
		static RenderCache mainCache;
		public static bool RebuildCache = false;

		//public static GameLib.EffectShader selMeshShader;


		void initOpenGL()
		{			
			GL.ClearColor(0.0f, 0.0f, 0.2f, 1.0f);
			GL.DepthFunc(DepthFunction.Lequal);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace (CullFaceMode.Back);

			GL.PrimitiveRestartIndex (uint.MaxValue);
			GL.Enable (EnableCap.PrimitiveRestart);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			GL.Enable (EnableCap.SampleShading);
			GL.MinSampleShading (0.5f);

			mainShader = new PuzzleShader();

			//selMeshShader = new GameLib.EffectShader ("Opuz2015.shaders.Border");


			mainCache = new RenderCache (ClientSize);

			ErrorCode err = GL.GetError ();
			Debug.Assert (err == ErrorCode.NoError, "OpenGL Error");	

		}
		#endregion

		#region Interface
		GraphicObject mainMenu = null;
		GraphicObject finishedMessage = null;
		GraphicObject imgSelection = null;

		void initInterface(){
			//special event handlers fired only if mouse not in interface objects
			//for scene mouse handling
			MouseMove += Mouse_Move;
			MouseButtonDown += Mouse_ButtonDown;
			MouseButtonUp += Mouse_ButtonUp;
			MouseWheelChanged += Mouse_WheelChanged;
			//KeyboardKeyDown += MainWin_KeyboardKeyDown;

			CrowInterface.LoadInterface("#Opuz2015.ui.fps.crow").DataSource = this;
			mainMenu = CrowInterface.LoadInterface("#Opuz2015.ui.MainMenu.goml");
			mainMenu.DataSource = this;
			mainMenu.Visible = false;

			Crow.CompilerServices.ResolveBindings (this.Bindings);
			mainMenu.Visible = true;
		}
		void showFinishedMsg(){
			if (finishedMessage != null)
				return;
			finishedMessage = CrowInterface.LoadInterface("#Opuz2015.ui.Finished.goml");
			finishedMessage.DataSource = this;
		}
		void onImageClick (object sender, Crow.MouseButtonEventArgs e){			
			if (imgSelection == null) {
				imgSelection = CrowInterface.LoadInterface ("#Opuz2015.ui.ImageSelect.goml");
				imgSelection.DataSource = this;
			}
			imgSelection.Visible = true;
			mainMenu.Visible = false;
		}
		void onSelectedImageChanged(object sender, SelectionChangeEventArgs e){
			mainMenu.Visible = true;
			imgSelection.Visible = false;
			ImagePath = e.NewValue.ToString();
		}
		void onCutPuzzle (object sender, Crow.MouseButtonEventArgs e)
		{
			mainMenu.Visible = false;
			currentState = GameState.CutStart;
		}
		void onButQuitClick (object sender, Crow.MouseButtonEventArgs e){
			closeGame ();

		}
		void onBackToMainMenu (object sender, Crow.MouseButtonEventArgs e)
		{
			closeCurrentPuzzle ();
		}
		#endregion

		GameState currentState = GameState.Init;
		Puzzle puzzle;

		int nbPceX = 5;
		int nbPceY = 3;
		string imagePath = @"Images/0.jpg";
		const float zSelPce = 8.0f;

		bool puzzleIsReady { get { return puzzle == null ? false : puzzle.Ready; } }

		public string[] Images
		{
			get {
				return Directory.GetFiles(
					System.IO.Path.GetDirectoryName(
						System.Reflection.Assembly.GetExecutingAssembly().Location ) + "/Images");
			}
		}
		public int NbPceX {
			get {
				return nbPceX;
			}
			set {
				if (value == nbPceX)
					return;
				nbPceX = value;
				NotifyValueChanged ("NbPceX", nbPceX);
			}
		}
		public int NbPceY {
			get { return nbPceY; }
			set { 
				if (value == nbPceY)
					return;
				nbPceY = value;
				NotifyValueChanged ("NbPceY", nbPceY);
			}
		}
		public string ImagePath {
			get { return imagePath; }
			set {
				imagePath = value;
				NotifyValueChanged ("ImagePath", imagePath);
			}
		}			

		void updateCache(){
			if (puzzle == null)
				return;
			if (!puzzle.Ready)
				return;
			
			mainCache.Bind (true);
			mainShader.Enable ();
			mainShader.Color = new Vector4 (1, 1, 1, 1);
			mainShader.ColorMultiplier = 1f;
			mainShader.Model = Matrix4.Identity;

			puzzle.Render ();

			GL.BindFramebuffer (FramebufferTarget.Framebuffer, 0);
			RebuildCache = false;
		}

		void closeGame(){
			if (puzzle != null)
				puzzle.Dispose();
			this.Quit (null,null);
		}
		void closeCurrentPuzzle(){
			currentState = GameState.Init;

			if (finishedMessage != null) {
				CrowInterface.DeleteWidget (finishedMessage);
				finishedMessage = null;
			}
			mainMenu.Visible = true;
			if (puzzle != null)
				puzzle.Dispose();
			puzzle = null;
		}

		#region OTK window overrides
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			initInterface ();

			initOpenGL ();
		}			
		public override void GLClear ()
		{
			GL.ClearColor(0.2f, 0.2f, 0.4f, 0.0f);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public override void OnRender (FrameEventArgs e)
		{
			if (currentState < GameState.Play)
				return;
			RenderCache.EnableCacheShader ();

			mainCache.PaintCache ();

			mainShader.Enable ();
			puzzle.Render (puzzle.Selection.ToArray ());
		}
		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);
			mainCache.CacheSize = ClientSize;
			UpdateViewMatrix();
		}
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);

			switch (currentState) {
			case GameState.Init:
				return;
			case GameState.CutStart:
				if (puzzle != null)
					puzzle.Dispose ();
				puzzle = new Puzzle (NbPceX, NbPceY, ImagePath);
				mainShader.Enable ();
				mainShader.ImgSize = new Vector2 (puzzle.Image.Width, puzzle.Image.Height);
				puzzle.Shuffle ();
				eyeDistTarget = puzzle.Image.Width * 1.5f;
				EyeDist = eyeDistTarget;
				currentState = GameState.Play;
				RebuildCache = true;
				return;
			case GameState.CutFinished:
				return;
			}

			GGL.Animation.ProcessAnimations ();

			if (RebuildCache)
				updateCache ();

		}
		#endregion

		#region vLookCalculations
		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView (fovY, r.Width / (float)r.Height, zNear, zFar);
			vLook = Vector3.Transform (vLookInit,
				Matrix4.CreateRotationX (viewXangle)*
				Matrix4.CreateRotationZ (viewZangle));
			vLook.Normalize();
			vEye = vEyeTarget + vLook * eyeDist;
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);
			GL.GetInteger(GetPName.Viewport, viewport);

			mainShader.Enable ();
			mainShader.SetMVP(modelview * projection);
			RebuildCache = true;
		}
		#endregion

		#region Keyboard
		protected override void OnKeyDown (OpenTK.Input.KeyboardKeyEventArgs e)
		{
			base.OnKeyDown (e);
			switch (e.Key) {
			case OpenTK.Input.Key.Space:
				if (puzzle != null)
					puzzle.resolve ();
				break;
			case OpenTK.Input.Key.Escape:
				if (puzzleIsReady)
					closeCurrentPuzzle ();
				else
					closeGame ();
				break;
			}
		}
		#endregion

		#region Mouse
		void Mouse_ButtonDown (object sender, OpenTK.Input.MouseButtonEventArgs e)
		{
			if (!puzzleIsReady)
				return;

			if (e.Button == OpenTK.Input.MouseButton.Left) {
				Piece[] tmp = null;

				lock (puzzle.Mutex)
					tmp = puzzle.ZOrderedPieces.ToArray();
				
				for (int i = tmp.Length-1; i >= 0; i--) {					
					Piece p = tmp [i];
					if (p.MouseIsIn (vMousePos)) {
						//this.CursorVisible = false;
						puzzle.SelectedPiece = p;
						p.ResetVisitedStatus ();
						p.PutOnTop ();
						p.ResetVisitedStatus ();
						p.Move (0f, 0f, zSelPce);
						break;
					}
				}
			} else if (e.Button == OpenTK.Input.MouseButton.Right) {
				if (puzzle.SelectedPiece == null)
					return;
				puzzle.SelectedPiece.ResetVisitedStatus ();
				puzzle.SelectedPiece.Rotate (puzzle.SelectedPiece);
			}

		}
		void Mouse_ButtonUp (object sender, OpenTK.Input.MouseButtonEventArgs e)
		{				
			if (!puzzleIsReady)
				return;	
			if (puzzle.SelectedPiece == null || e.Button != OpenTK.Input.MouseButton.Left)
				return;
			
			puzzle.SelectedPiece.ResetVisitedStatus ();
			puzzle.SelectedPiece.Move (0f, 0f, -zSelPce);
			puzzle.SelectedPiece.ResetVisitedStatus ();
			puzzle.SelectedPiece.Test ();
			puzzle.SelectedPiece.ResetVisitedStatus ();
			if (puzzle.SelectedPiece.PuzzleIsFinished) {
				showFinishedMsg ();
			}
			//ensure newly linked pce are on top of others
			puzzle.SelectedPiece.ResetVisitedStatus ();
			puzzle.SelectedPiece.PutOnTop ();
			puzzle.SelectedPiece = null;
		}
		Vector3 vMousePos;
		void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs e)
		{			
			if (!puzzleIsReady)
				return;
			if (e.XDelta != 0 || e.YDelta != 0)
			{				
				Vector3 vMouse = glHelper.UnProject(ref projection, ref modelview, viewport, new Vector2 (e.X, e.Y)).Xyz;
				Vector3 vMouseRay = Vector3.Normalize(vMouse - vEye);
				float a = -vMouse.Z / vMouseRay.Z;
				Vector3 vNewMousePos = vMouse + vMouseRay * a;
				Vector3 vMouseDelta = vNewMousePos - vMousePos;
				vMousePos = vNewMousePos;

				if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
					//viewZangle -= (float)e.XDelta * RotationSpeed;
					viewXangle -= (float)e.YDelta * RotationSpeed;
					if (viewXangle < - 0.75f)
						viewXangle = -0.75f;
					else if (viewXangle > MathHelper.PiOver4)
						viewXangle = MathHelper.PiOver4;
					UpdateViewMatrix ();
					return;
				}
				if (e.Mouse.LeftButton == OpenTK.Input.ButtonState.Pressed) {
					if (puzzle.SelectedPiece != null) {						
						Piece p = puzzle.SelectedPiece;
						p.ResetVisitedStatus ();
						p.Move (vMouseDelta.X, vMouseDelta.Y);
						return;
					}
				}
				if (e.Mouse.RightButton == OpenTK.Input.ButtonState.Pressed) {
					Matrix4 m = Matrix4.CreateTranslation (-e.XDelta, e.YDelta, 0);
					vEyeTarget = Vector3.Transform (vEyeTarget, m);
					UpdateViewMatrix();
					return;
				}
			}

		}			
		void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs e)
		{
			if (!puzzleIsReady)
				return;
			float speed = MoveSpeed;
			if (Keyboard[OpenTK.Input.Key.ShiftLeft])
				speed *= 0.1f;
			else if (Keyboard[OpenTK.Input.Key.ControlLeft])
				speed *= 20.0f;

			eyeDistTarget -= e.Delta * speed;
			if (eyeDistTarget < zNear+10)
				eyeDistTarget = zNear+10;
			else if (eyeDistTarget > zFar-100)
				eyeDistTarget = zFar-100;
			Animation.StartAnimation(new Animation<float> (this, "EyeDist", eyeDistTarget, (eyeDistTarget - eyeDist) * 0.2f));
		}
		#endregion

		#region CTOR and Main
		public MainWin (int numSamples = 4)
			: base(1024, 800, 32, 24, 0, numSamples, "Opuz")
		{			
		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (MainWin win = new MainWin( )) {
				win.Run (30.0);
			}
		}
		#endregion
	}
}