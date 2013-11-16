using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Diagnostics;
using OTKGL;
using OpenTK.Input;

namespace OTKGL
{



    public static class Mouse3d
    {
        #region winApi

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct Point
        {

            /// LONG->int
            public int x;

            /// LONG->int
            public int y;
        }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct HICON__
        {

            /// int
            public int unused;
        }

#if _WIN32 || _WIN64
        /// Return Type: HCURSOR->HICON->HICON__*
        ///hCursor: HCURSOR->HICON->HICON__*
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursor")]
        public static extern System.IntPtr SetCursor([System.Runtime.InteropServices.InAttribute()] System.IntPtr hCursor);
#elif __linux__
		public static System.IntPtr SetCursor(System.IntPtr hCursor)
		{
			return (IntPtr)0;
		}
#endif

        /// Return Type: BOOL->int
        ///lpPoint: LPPOINT->tagPOINT*
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "GetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GetCursorPos([System.Runtime.InteropServices.OutAttribute()] out Point lpPoint);

        /// Return Type: BOOL->int
        ///X: int
        ///Y: int
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        #endregion

        public static float RotationSpeed = 0.005f;

        public static Vector3 Position;
        public static bool MouseLeftIsDown = false;
        public static bool MouseRightIsDown = false;
        public static bool MouseMiddleIsDown = false;


        static int oldMouseX;
        static int oldMouseY;

        public static void setPosition(Vector3 newPos)
        {
            Position = newPos;
            Point pt;
            GetCursorPos(out pt);
            oldMouseX = pt.x;
            oldMouseY = pt.y;
        }

        public static void update()
        {
            //SetCursor((IntPtr)0);

            Point pt;
            GetCursorPos(out pt);

            //mise a jour mouse 3d

            int mDiffX = oldMouseX - pt.x;
            int mDiffY = oldMouseY - pt.y;

            
            
            GetCursorPos(out pt);

            oldMouseX = pt.x;
            oldMouseY = pt.y;

            Delta = new Vector2(mDiffX, mDiffY);


            //if (Delta != Vector2.Zero)
            //{
            //    if (MouseRightIsDown )
            //    {
            //        //camera rotation
            //        Matrix4 m = Matrix4.CreateRotationZ(Delta.X * RotationSpeed);
            //        vLook = Vector3.Transform(vLook, m);

            //        //vecteur perpendiculaire sur le plan x,y
            //        Vector3 vLookPerpendicularOnXYPlane = new Vector3(new Vector2(vLook).PerpendicularLeft);
            //        vLookPerpendicularOnXYPlane.Normalize();                    

            //        Matrix4 m2 = Matrix4.Rotate(vLookPerpendicularOnXYPlane, -Delta.Y * RotationSpeed);

            //        vLook = Vector3.Transform(vLook, m2);
            //    }

            //}
            //return vLook;
        }

        public static Vector2 Delta = Vector2.Zero;


        public static float X
        { get { return Position.X; } }
        public static float Y
        { get { return Position.Y; } }


        public static void init()
        {            
            float initpos = 40f;
            Position = new Vector3(initpos,initpos,World.CurrentWorld.getHeight(initpos,initpos));
        }
        public static void render()
        { 
            GL.PushAttrib(AttribMask.EnableBit);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.DepthTest);

            GL.PointSize(5.0f);
            GL.Color3(Color.Aqua);

            GL.Begin(BeginMode.Points);
            GL.Vertex3(Position);
            GL.End();

            GL.Color3(Color.White);
            GL.PopAttrib();
        }

        public static void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                MouseLeftIsDown = true;
            if (e.Button == MouseButton.Right)
                MouseRightIsDown = true;
            if (e.Button == MouseButton.Middle)
                MouseMiddleIsDown = true;
        }
        public static void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                MouseLeftIsDown = false;
            if (e.Button == MouseButton.Right)
                MouseRightIsDown = false;
            if (e.Button == MouseButton.Middle)
                MouseMiddleIsDown = false;

        }

    }
}
