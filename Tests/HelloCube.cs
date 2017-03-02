//
// HelloCube.cs
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
	class HelloCube : CrowWindow
	{
		[STAThread]
		static void Main ()
		{
			HelloCube win = new HelloCube ();
			win.Run (30);
		}

		public HelloCube ()
			: base(800, 600,"Crow Test with OpenTK")
		{
		}

		vaoMesh cube;
		Texture texture;
		Matrix4 projection, modelview;

		void initGL(){
			GL.Enable (EnableCap.CullFace);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			cube = vaoMesh.CreateCube ();
			texture = new Texture ("image/textest.png");

			projection =
				Matrix4.CreatePerspectiveFieldOfView (
					MathHelper.PiOver4,
					ClientRectangle.Width / (float)ClientRectangle.Height, 1.0f, 10.0f);
			modelview = Matrix4.LookAt(new Vector3(5,5,5), Vector3.Zero, Vector3.UnitZ);
		}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			AddWidget(
				new Window ()
				{
					Caption = "Hello World"
				}
			);
			initGL ();
		}
		public override void OnRender (FrameEventArgs e)
		{			
			base.OnRender (e);

			shader.SetMVP(modelview * projection);

			GL.BindTexture (TextureTarget.Texture2D, texture);
			cube.Render (BeginMode.Triangles);
			GL.BindTexture (TextureTarget.Texture2D, 0);
		}
	}
}