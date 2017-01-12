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
	class Hello3D : CrowWindow3D
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

		vaoMesh cube;
		Texture texture;


		void initGL(){
			GL.Enable (EnableCap.CullFace);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			cube = vaoMesh.CreateCube ();
			texture = new Texture ("image/textest.png");

//			projection =
//				Matrix4.CreatePerspectiveFieldOfView (
//					MathHelper.PiOver4,
//					ClientRectangle.Width / (float)ClientRectangle.Height, 1.0f, 10.0f);
//			modelview = Matrix4.LookAt(vEye*eyeDist, Vector3.Zero, Vector3.UnitZ);
		}
//		Vector3 vEye = new Vector3(1,1,1).Normalized();
//		float eyeDist = 5f;
//		const float rotSpeed = -0.03f;
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			//MouseMove += HelloCube_MouseMove;

			CrowInterface.LoadInterface (@"Interfaces/Divers/0.crow");
			initGL ();
			shader.Enable ();
		}

//		void HelloCube_MouseMove (object sender, OpenTK.Input.MouseMoveEventArgs e)
//		{
//			if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
//				Vector2 vPerp = vEye.Xy.PerpendicularLeft;
//				vEye = vEye.Transform (
//					Matrix4.CreateFromAxisAngle (new Vector3 (vPerp.X, vPerp.Y, 0), e.YDelta * rotSpeed) *
//					Matrix4.CreateRotationZ (e.XDelta * rotSpeed)
//				).Normalized ();
//				modelview = Matrix4.LookAt (vEye * eyeDist, Vector3.Zero, Vector3.UnitZ);
//			}
//		}
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