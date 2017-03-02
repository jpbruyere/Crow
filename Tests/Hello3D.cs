//
// Hello3D.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Crow;

namespace Tests
{
	class Hello3D : CrowWindow
	{
		[STAThread]
		static void Main ()
		{
			Hello3D win = new Hello3D ();
			win.Run (30);
		}

		public Hello3D ()
			: base(800, 600,"Crow Test with OpenTK")
		{
		}

		public Matrix4 modelview, projection;
		public int[] viewport = new int[4];
		public Vector3 vEyeTarget = new Vector3(0f, 0f, 0f);
		public Vector3 vEye;
		public Vector3 vLookInit = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
		public Vector3 vLook;  // Camera vLook Vector
		public float zNear = 0.001f, zFar = 300.0f;
		public float fovY = (float)Math.PI / 4;
		public float eyeDist = 10.2f;
		public float viewZangle, viewXangle;
		public const float MoveSpeed = 0.02f;
		public const float RotationSpeed = 0.005f;
		public const float ZoomSpeed = 0.22f;

		vaoMesh cube;
		Texture texture;
		ProjectiveIFaceControler iface3D;

		void initGL(){
			GL.Enable (EnableCap.CullFace);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			cube = vaoMesh.CreateCube ();
			texture = new Texture ("image/textest.png");
		}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			MouseMove += HelloCube_MouseMove;
			MouseWheelChanged += Hello3D_MouseWheelChanged;

			iface3D = Add3DInterface (800, 800,
				Matrix4.CreateScale (6f) *
				Matrix4.CreateRotationX (MathHelper.PiOver2) *
				Matrix4.CreateTranslation (Vector3.UnitY * -1.1f));
			Load (@"Interfaces/Divers/0.crow").DataSource = this;
			initGL ();
			shader.Enable ();
		}

		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);
			UpdateViewMatrix ();
		}

		public override void OnRender (FrameEventArgs e)
		{			
			base.OnRender (e);

			shader.SetMVP(modelview * projection);

			GL.BindTexture (TextureTarget.Texture2D, texture);
			cube.Render (BeginMode.Triangles);
			GL.BindTexture (TextureTarget.Texture2D, 0);
		}

		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView (fovY, r.Width / (float)r.Height, zNear, zFar);
			vLook = vLookInit.Transform(
				Matrix4.CreateRotationX (viewXangle)*
				Matrix4.CreateRotationZ (viewZangle));
			vLook.Normalize();
			vEye = vEyeTarget + vLook * eyeDist;
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);
			GL.GetInteger(GetPName.Viewport, viewport);

			iface3D.UpdateView (projection, modelview, viewport, vEye);
		}

		void HelloCube_MouseMove(object sender, OpenTK.Input.MouseMoveEventArgs otk_e)
		{
			if (otk_e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
				viewZangle -= (float)otk_e.XDelta * RotationSpeed;
				viewXangle -= (float)otk_e.YDelta * RotationSpeed;
				UpdateViewMatrix ();
			} else if (otk_e.Mouse.LeftButton == OpenTK.Input.ButtonState.Pressed) {
				return;
			} else if (otk_e.Mouse.RightButton == OpenTK.Input.ButtonState.Pressed) {
				Vector2 v2Look = vLook.Xy.Normalized ();
				Vector2 disp = v2Look.PerpendicularLeft * otk_e.XDelta * MoveSpeed +
					v2Look * otk_e.YDelta * MoveSpeed;
				vEyeTarget += new Vector3 (disp.X, disp.Y, 0);
				UpdateViewMatrix ();
			}
		}
		void Hello3D_MouseWheelChanged (object sender, OpenTK.Input.MouseWheelEventArgs e)
		{
			float speed = ZoomSpeed;
			if (Keyboard[OpenTK.Input.Key.ControlLeft])
				speed *= 20.0f;

			eyeDist -= e.Delta * speed;
			if (eyeDist < zNear)
				eyeDist = zNear;
			else if (eyeDist > zFar)
				eyeDist = zFar;
			UpdateViewMatrix ();
		}
	}
}