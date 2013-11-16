using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace go
{
    public static class Win32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);
        #region  mouse winApi

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
    }
}
