//
//  HelloCube.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
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
using Crow;

namespace Tests
{
	class HelloCube : OpenTKGameWindow
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

			CrowInterface.AddWidget(
				new Window ()
				{
					Title = "Hello World",
					Width = 200,
					Height = 200
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