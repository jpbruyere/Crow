//
//  InterfaceControler.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Threading;
using System.Collections.Generic;

namespace Crow
{
	public class ProjectiveIFaceControler : InterfaceControler {
		Matrix4 modelview;
		int[] viewport = new int[4];
		Vector3 vEyePosition;

		public Matrix4 ifaceModelMat;
		Point localMousePos;

		public ProjectiveIFaceControler(Rectangle ifaceBounds, Matrix4 _ifaceModelMat)
			: base(ifaceBounds){
			ifaceModelMat = _ifaceModelMat;
		}

		public override Matrix4 InterfaceMVP {
			get { return ifaceModelMat * modelview * projection; }
		}

		public override void initGL(){
			quad = new Crow.vaoMesh (0, 0, 0, 1, 1, 1, -1);
			//ifaceModelMat = Matrix4.CreateRotationX(MathHelper.PiOver2) * Matrix4.CreateTranslation(Vector3.UnitY);
			CrowInterface.ProcessResize(iRect);
			createContext ();
			//CrowInterface.ProcessResize (iRect);
		}
		public override void ProcessResize (Rectangle newSize)
		{
		}
		public override void OpenGLDraw ()
		{
			GL.Enable (EnableCap.DepthTest);
			base.OpenGLDraw ();
			GL.Disable (EnableCap.DepthTest);
		}
		public void UpdateView (Matrix4 _projection, Matrix4 _modelview, int[] _viewport, Vector3 _vEyePosition)
		{
			projection = _projection;
			modelview = _modelview;
			viewport = _viewport;
			vEyePosition = _vEyePosition;
		}
		public override bool ProcessMouseMove (int x, int y)
		{
			Matrix4 mv = ifaceModelMat * modelview;
			Vector3 vMouse = UnProject(ref projection, ref mv, viewport, new Vector2 (x, y)).Xyz;
			Vector3 vE = vEyePosition.Transform (ifaceModelMat.Inverted());
			Vector3 vMouseRay = Vector3.Normalize(vMouse - vE);
			float a = vE.Z / vMouseRay.Z;
			vMouse = vE - vMouseRay * a;
			//vMouse = vMouse.Transform (interfaceModelView.Inverted());
			localMousePos = new Point ((int)Math.Truncate ((vMouse.X + 0.5f) * iRect.Width),
				iRect.Height - (int)Math.Truncate ((vMouse.Y + 0.5f) * iRect.Height));
			mouseIsInInterface = localMousePos.X.IsInBetween (0, iRect.Width) & localMousePos.Y.IsInBetween (0, iRect.Height);

			return mouseIsInInterface ? CrowInterface.ProcessMouseMove (localMousePos.X, localMousePos.Y) : false;
		}
		Vector4 UnProject(ref Matrix4 projection, ref Matrix4 view, int[] viewport, Vector2 mouse)
		{
			Vector4 vec;

			vec.X = 2.0f * mouse.X / (float)viewport[2] - 1;
			vec.Y = -(2.0f * mouse.Y / (float)viewport[3] - 1);
			vec.Z = 0f;
			vec.W = 1.0f;

			Matrix4 viewInv = Matrix4.Invert(view);
			Matrix4 projInv = Matrix4.Invert(projection);

			Vector4.Transform(ref vec, ref projInv, out vec);
			Vector4.Transform(ref vec, ref viewInv, out vec);

			if (vec.W > float.Epsilon || vec.W < float.Epsilon)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}

			return vec;
		}
	}
	public class InterfaceControler : IDisposable {
		public Interface CrowInterface;
		public int texID;
		public vaoMesh quad;
		public Rectangle iRect = new Rectangle(0,0,2048,2048);
		public bool mouseIsInInterface = false;

		protected Matrix4 projection;
		public virtual Matrix4 InterfaceMVP {
			get { return projection; }
		}

		#if MEASURE_TIME
		public List<PerformanceMeasure> PerfMeasures;
		public PerformanceMeasure glDrawMeasure = new PerformanceMeasure("OpenGL Draw", 10);
		#endif

		#region CTOR
		public InterfaceControler(Rectangle ifaceBounds){
			iRect = ifaceBounds;

			CrowInterface = new Interface ();

			#if MEASURE_TIME
			PerfMeasures = new List<PerformanceMeasure> (
				new PerformanceMeasure[] {
					this.CrowInterface.updateMeasure,
					this.CrowInterface.layoutingMeasure,
					this.CrowInterface.clippingMeasure,
					this.CrowInterface.drawingMeasure,
					this.glDrawMeasure
				}
			);
			#endif

			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();

			initGL ();
		}
		#endregion

		void interfaceThread()
		{
			while (CrowInterface.ClientRectangle.Size.Width == 0)
				Thread.Sleep (5);

			while (true) {
				CrowInterface.Update ();
				//Thread.Sleep (1);
			}
		}

		#region Mouse And Keyboard handling
		public virtual void ProcessResize(Rectangle newSize){
			iRect = newSize;
			CrowInterface.ProcessResize(newSize);
			createContext ();
			GL.Viewport (0, 0, newSize.Width, newSize.Height);//TODO:find a better place for this
		}
		public virtual bool ProcessMouseMove(int x, int y){
			return CrowInterface.ProcessMouseMove (x, y);
		}
		public virtual bool ProcessMouseButtonUp(int button)
		{
			return CrowInterface.ProcessMouseButtonUp (button);
		}
		public virtual bool ProcessMouseButtonDown(int button)
		{
			return CrowInterface.ProcessMouseButtonDown (button);
		}
		public virtual bool ProcessMouseWheelChanged(float delta)
		{
			return CrowInterface.ProcessMouseWheelChanged (delta);
		}
		public virtual bool ProcessKeyDown(int Key){
			return CrowInterface.ProcessKeyDown(Key);
		}
		public virtual bool ProcessKeyUp(int Key){
			return CrowInterface.ProcessKeyUp(Key);
		}
		public virtual bool ProcessKeyPress(char Key){
			return CrowInterface.ProcessKeyPress(Key);
		}
		#endregion

		#region graphic context
		public virtual void initGL(){
			projection = OpenTK.Matrix4.CreateOrthographicOffCenter (-0.5f, 0.5f, -0.5f, 0.5f, 1, -1);
			quad = new Crow.vaoMesh (0, 0, 0, 1, 1, 1, -1);
			createContext ();
		}
		/// <summary>Create the texture for the interface redering</summary>
		public virtual void createContext()
		{
			if (GL.IsTexture(texID))
				GL.DeleteTexture (texID);
			GL.GenTextures(1, out texID);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texID);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				iRect.Width, iRect.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, CrowInterface.bmp);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
		/// <summary>Rendering of the interface</summary>
		public virtual void OpenGLDraw()
		{
			#if MEASURE_TIME
			glDrawMeasure.StartCycle();
			#endif

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texID);
			if (Monitor.TryEnter(CrowInterface.RenderMutex)) {
				if (CrowInterface.IsDirty) {
					GL.TexSubImage2D (TextureTarget.Texture2D, 0,
						CrowInterface.DirtyRect.Left, CrowInterface.DirtyRect.Top,
						CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height,
						OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, CrowInterface.dirtyBmp);
					CrowInterface.IsDirty = false;
				}
				Monitor.Exit (CrowInterface.RenderMutex);
			}
			quad.Render (BeginMode.TriangleStrip);
			GL.BindTexture(TextureTarget.Texture2D, 0);

			#if MEASURE_TIME
			glDrawMeasure.StopCycle();
			#endif
		}
		#endregion

		#region IDisposable implementation

		public void Dispose ()
		{
			if (GL.IsTexture(texID))
				GL.DeleteTexture (texID);
			if (quad != null)
				quad.Dispose ();
		}

		#endregion
	}
}

