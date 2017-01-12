//
//  OpenTKGameWindow.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Crow
{
	public class CrowWindow3D : CrowWindow
    {
		#region ctor
		public CrowWindow3D(int _width = 800, int _height = 600, string _title="Crow",
			int colors = 32, int depth = 24, int stencil = 0, int samples = 1,
			int major=3, int minor=3)
			: this(_width, _height, new OpenTK.Graphics.GraphicsMode(colors, depth, stencil, samples),
				_title,GameWindowFlags.Default,DisplayDevice.Default,
				major,minor,OpenTK.Graphics.GraphicsContextFlags.Default)
		{
		}
		public CrowWindow3D (int width, int height, OpenTK.Graphics.GraphicsMode mode, string title, GameWindowFlags options, DisplayDevice device, int major, int minor, OpenTK.Graphics.GraphicsContextFlags flags)
			: base(width,height,mode,title,options,device,major,minor,flags)
		{
		}

		#endregion

		#region graphic context
		protected Matrix4 modelview;
		protected int[] viewport = new int[4];
		protected Vector3 vEyeTarget = new Vector3(0f, 0f, 0f);
		protected Vector3 vEye;
		protected Vector3 vLookInit = Vector3.Normalize(new Vector3(0.0f, 1.0f, 0.0f));
		protected Vector3 vLook;  // Camera vLook Vector
		protected float zNear = 0.001f, zFar = 300.0f;
		protected float fovY = (float)Math.PI / 4;
		protected float eyeDist = 1.2f;
		protected float viewZangle, viewXangle;
		protected const float MoveSpeed = 0.02f;
		protected const float RotationSpeed = 0.005f;
		protected const float ZoomSpeed = 0.22f;

		public Matrix4 interfaceModelView;
		Rectangle iRect = new Rectangle(0,0,2048,2048);

		protected override Matrix4 InterfaceMVP {
			get { return interfaceModelView * modelview * projection; }
		}

		protected override void initGL(){
			GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
			shader = new Shader ();
			//quad = new Crow.vaoMesh (0, 0, 0, 1, iRect.Height / (float)iRect.Width, 1, -1);
			quad = new Crow.vaoMesh (0, 0, 0, 1, 1, 1, -1);
			interfaceModelView = Matrix4.CreateRotationX(MathHelper.PiOver2) * Matrix4.CreateTranslation(Vector3.UnitY);
			createContext ();
			CrowInterface.ProcessResize (iRect);
		}
		/// <summary>Create the texture for the interface redering</summary>
		protected override void createContext ()
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

		#endregion


		#region Game win overrides

		protected override void OnResize(EventArgs e)
		{
			//base.OnResize (e);
			UpdateViewMatrix ();
		}
		#endregion

		#region Mouse Handling
		Point mousePosition;
		bool mouseIsInInterface = false;

		void updateMousePosition(OpenTK.Input.MouseMoveEventArgs otk_e)
		{
			Matrix4 mv = interfaceModelView * modelview;
			Vector3 vMouse = UnProject(ref projection, ref mv, viewport, new Vector2 (otk_e.X, otk_e.Y)).Xyz;
			Vector3 vE = vEye.Transform (interfaceModelView.Inverted());
			Vector3 vMouseRay = Vector3.Normalize(vMouse - vE);
			float a = vE.Z / vMouseRay.Z;
			vMouse = vE - vMouseRay * a;
			//vMouse = vMouse.Transform (interfaceModelView.Inverted());
			mousePosition = new Point ((int)Math.Truncate ((vMouse.X + 0.5f) * iRect.Width),
				iRect.Height - (int)Math.Truncate ((vMouse.Y + 0.5f) * iRect.Height));
			mouseIsInInterface = mousePosition.X.IsInBetween (0, iRect.Width) & mousePosition.Y.IsInBetween (0, iRect.Height);
			//System.Diagnostics.Debug.WriteLine ("vMouse={0} newPos={1}", vMouse, mousePosition);
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
		}

		protected override void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs otk_e)
        {
			updateMousePosition (otk_e);

			if (mouseIsInInterface & !(Keyboard [OpenTK.Input.Key.ShiftLeft])) {
				if (CrowInterface.ProcessMouseMove (mousePosition.X, mousePosition.Y))
					return;
			} 
			if (otk_e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
				viewZangle -= (float)otk_e.XDelta * RotationSpeed;
				viewXangle += (float)otk_e.YDelta * RotationSpeed;
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

			//MouseMove.Raise (sender, otk_e);
        }

		protected override void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
        {
			if (mouseIsInInterface & !Keyboard [OpenTK.Input.Key.ShiftLeft])
				CrowInterface.ProcessMouseButtonUp ((int)otk_e.Button);
//			else
//				MouseButtonUp.Raise (sender, otk_e);
        }
		protected override void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		{
			if (mouseIsInInterface & !Keyboard [OpenTK.Input.Key.ShiftLeft])
				CrowInterface.ProcessMouseButtonDown ((int)otk_e.Button);
//			else
//				MouseButtonDown.Raise (sender, otk_e);
        }
		protected override void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
        {
			if (mouseIsInInterface & !Keyboard [OpenTK.Input.Key.ShiftLeft]) {
				CrowInterface.ProcessMouseWheelChanged (otk_e.DeltaPrecise);
				return;
			}

			float speed = ZoomSpeed;
			if (Keyboard[OpenTK.Input.Key.ControlLeft])
				speed *= 20.0f;

			eyeDist -= otk_e.Delta * speed;
			if (eyeDist < zNear)
				eyeDist = zNear;
			else if (eyeDist > zFar)
				eyeDist = zFar;
			UpdateViewMatrix ();

//			MouseWheelChanged.Raise (sender, otk_e);
        }
		#endregion

		static Vector4 UnProject(ref Matrix4 projection, ref Matrix4 view, int[] viewport, Vector2 mouse)
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

	public static class Extensions {
		public static Vector4 ToVector4(this Color c){
			float[] f = c.floatArray;
			return new Vector4 (f [0], f [1], f [2], f [3]);
		}
		public static Vector3 Transform(this Vector3 v, Matrix4 m){
			return Vector4.Transform(new Vector4(v, 1), m).Xyz;
		}
		public static bool IsInBetween(this int v, int min, int max){
			return v >= min & v <= max;
		}
	}
}
