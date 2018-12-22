//
// XLibBackend.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OpenToolkit.NT.Native;

namespace Crow.Win32
{

    public class Win32Backend : IBackend
    {
        public void CleanUp()
        {
            User32.Window.ReleaseDC(handle, hdc);
            User32.Window.DestroyWindow(handle);
        }

        public void Flush()
        {
			iFace.surf.Flush ();
            //throw new NotImplementedException();
        }

        WindowProc WindowProcedureDelegate;
        string className = "myWindowClass";
        IntPtr instance = Marshal.GetHINSTANCE(typeof(Win32Backend).Module);
        IntPtr handle = IntPtr.Zero;
        IntPtr hdc = IntPtr.Zero;
		Interface iFace;

        public void Init(Interface _iFace)
        {
			iFace = _iFace;

            WindowProcedureDelegate = WindowProcedure;

            Rect rect = new Rect
            {
                Left = iFace.ClientRectangle.Left,
                Top = iFace.ClientRectangle.Top,
                Right = iFace.ClientRectangle.Right,
                Bottom = iFace.ClientRectangle.Bottom
            };

            User32.Window.AdjustWindowRectEx(ref rect, 0, false, 0);

            ExtendedWindowClass wc = new ExtendedWindowClass
            {
                Size = ExtendedWindowClass.SizeInBytes,
                Style = 0,
                WindowProc = WindowProcedureDelegate,
                ClassExtra = 0,
                WindowExtra = 0,
                Instance = instance,
                Icon = IntPtr.Zero,
                Cursor = User32.Cursor.LoadCursor(CursorName.Arrow),
                //Background = Gdi32.GetStockObject(GetStockObjectType.BlackBrush),
                MenuName = null,
                ClassName = className,
            };

            ushort atom = User32.WindowClass.RegisterClassEx(ref wc);

            handle = User32.Window.CreateWindowEx(
                ExtendedWindowStyles.ClientEdge,
                className,
                "The title of my window",
                WindowStyles.OverlappedWindow,

                rect.Left,
                rect.Top,
                rect.Width,
                rect.Height,
                IntPtr.Zero,
                IntPtr.Zero,
                instance,
                IntPtr.Zero
            );

            User32.Window.ShowWindow(handle, ShowWindowCommand.Show);

            hdc = User32.Window.GetDC(handle);
            iFace.surf = new Crow.Cairo.Win32Surface(hdc);

        }

        public void ProcessEvents()
        {
			Msg msg;
            while (User32.Message.PeekMessage(out msg, IntPtr.Zero, 0, 0, PeekMessageActions.Remove))
            {
                User32.Message.TranslateMessage(ref msg);
                User32.Message.DispatchMessage(ref msg);
            }
        }

		public bool IsDown (Key key) {
			return false;
		}
		public bool Shift {
			get {
				throw new NotImplementedException ();
			}
		}

		public bool Ctrl {
			get {
				throw new NotImplementedException ();
			}
		}

		public bool Alt {
			get {
				throw new NotImplementedException ();
			}
		}

		void HandleWindowPositionChanged(IntPtr handle, WindowMessage message, IntPtr wParam, IntPtr lParam)
		{
			var pos = Marshal.PtrToStructure<WindowPosition>(lParam);
			if (pos.HWnd == handle)
			{
				Rectangle bounds = new Rectangle(pos.X, pos.Y, pos.Width, pos.Height);

				User32.Window.SetWindowPos
				(
					handle, 
					IntPtr.Zero, 
					bounds.X, 
					bounds.Y, 
					bounds.Width,
					bounds.Height,
					SetWindowPosFlags.NoZOrder | SetWindowPosFlags.NoOwnerZOrder |
					SetWindowPosFlags.NoActivate | SetWindowPosFlags.NoSendChanging
				);
				bounds.Left = bounds.Top = 0;
				iFace.ProcessResize (bounds);
			}
		}

		static Key GetKey(int code)
		{
			switch (code)
			{
			// 0 - 15
			case 0: return Key.NoSymbol;
			case 1: return Key.Escape;
			case 2: return Key.key_1;
			case 3: return Key.key_2;
			case 4: return Key.key_3;
			case 5: return Key.key_4;
			case 6: return Key.key_5;
			case 7: return Key.key_6;
			case 8: return Key.key_7;
			case 9: return Key.key_8;
			case 10: return Key.key_9;
			case 11: return Key.key_0;
			case 12: return Key.minus;
			case 13: return Key.plus;
			case 14: return Key.BackSpace;
			case 15: return Key.Tab;

				// 16-31
			case 16: return Key.Q;
			case 17: return Key.W;
			case 18: return Key.E;
			case 19: return Key.R;
			case 20: return Key.T;
			case 21: return Key.Y;
			case 22: return Key.U;
			case 23: return Key.I;
			case 24: return Key.O;
			case 25: return Key.P;
			case 26: return Key.bracketleft;
			case 27: return Key.bracketright;
			case 28: return Key.Return;
			case 29: return Key.Control_L;
			case 30: return Key.A;
			case 31: return Key.S;

				// 32 - 47
			case 32: return Key.D;
			case 33: return Key.F;
			case 34: return Key.G;
			case 35: return Key.H;
			case 36: return Key.J;
			case 37: return Key.K;
			case 38: return Key.L;
			case 39: return Key.semicolon;
			case 40: return Key.quotedbl;
			case 41: return Key.grave;
			case 42: return Key.Shift_L;
			case 43: return Key.backslash;
			case 44: return Key.Z;
			case 45: return Key.X;
			case 46: return Key.C;
			case 47: return Key.V;

				// 48 - 63
			case 48: return Key.B;
			case 49: return Key.N;
			case 50: return Key.M;
			case 51: return Key.comma;
			case 52: return Key.period;
			case 53: return Key.slash;
			case 54: return Key.Shift_R;
			case 55: return Key.Print;
			case 56: return Key.Alt_L;
			case 57: return Key.space;
			case 58: return Key.Caps_Lock;
			case 59: return Key.F1;
			case 60: return Key.F2;
			case 61: return Key.F3;
			case 62: return Key.F4;
			case 63: return Key.F5;

				// 64 - 79
			case 64: return Key.F6;
			case 65: return Key.F7;
			case 66: return Key.F8;
			case 67: return Key.F9;
			case 68: return Key.F10;
			case 69: return Key.Num_Lock;
			case 70: return Key.Scroll_Lock;
			case 71: return Key.Home;
			case 72: return Key.Up;
			case 73: return Key.Page_Up;
			case 74: return Key.KP_Subtract;
			case 75: return Key.Left;
			case 76: return Key.KP_5;
			case 77: return Key.Right;
			case 78: return Key.KP_Add;
			case 79: return Key.End;

				// 80 - 95
			case 80: return Key.Down;
			case 81: return Key.Page_Down;
			case 82: return Key.Insert;
			case 83: return Key.Delete;
			case 84: return Key.NoSymbol;
			case 85: return Key.NoSymbol;
			case 86: return Key.NoSymbol;
			case 87: return Key.F11;
			case 88: return Key.F12;
			case 89: return Key.Pause;
			case 90: return Key.NoSymbol;
			case 91: return Key.Meta_L;
			case 92: return Key.Meta_R;
			case 93: return Key.Menu;
			case 94: return Key.NoSymbol;
			case 95: return Key.NoSymbol;

				// 96 - 106
			case 96: return Key.NoSymbol;
			case 97: return Key.NoSymbol;
			case 98: return Key.NoSymbol;
			case 99: return Key.NoSymbol;
			case 100: return Key.F13;
			case 101: return Key.F14;
			case 102: return Key.F15;
			case 103: return Key.F16;
			case 104: return Key.F17;
			case 105: return Key.F18;
			case 106: return Key.F19;

			default: return Key.NoSymbol;
			}
		}
		const long ExtendedBit = 1 << 24; // Used to distinguish left and right control, alt and enter keys.

		void handleKeyboard (IntPtr lParam, IntPtr wParam, bool pressed) {
			bool extended = (lParam.ToInt64() & ExtendedBit) != 0;
			uint scancode = (uint)((lParam.ToInt64() >> 16) & 0xFF);
			//ushort repeat_count = unchecked((ushort)((ulong)lParam.ToInt64() & 0xffffu));
//			VirtualKey vkey = (VirtualKey)wParam;
//			var key = WinKeyMap.TranslateKey(scancode, vkey, extended, false, out bool is_valid);
			Key k = GetKey ((int)scancode);

			if (pressed) 
				iFace.ProcessKeyDown (k);
			else
				iFace.ProcessKeyUp (k);			
		}
		void handleChar (IntPtr lParam, IntPtr wParam) {
			char c = IntPtr.Size == 4 ?
				(char)wParam.ToInt32() :
				(char)wParam.ToInt64();

			if (!char.IsControl (c))
				iFace.ProcessKeyPress (c);
		}



        IntPtr WindowProcedure(IntPtr handle, WindowMessage message, IntPtr wParam, IntPtr lParam)
        {
            var result = IntPtr.Zero;


            switch (message)
            {
            case WindowMessage.MouseMove:
                iFace.ProcessMouseMove
                ((int)((uint)lParam.ToInt32() & 0x0000FFFF), (int)(((uint)lParam.ToInt32() & 0xFFFF0000) >> 16));
                break;
            case WindowMessage.LButtonDown:
                iFace.ProcessMouseButtonDown(Crow.MouseButton.Left);
                return IntPtr.Zero;
            case WindowMessage.RButtonDown:
                iFace.ProcessMouseButtonDown(Crow.MouseButton.Right);
                return IntPtr.Zero;
            case WindowMessage.MButtonDown:
                iFace.ProcessMouseButtonDown(Crow.MouseButton.Middle);
                return IntPtr.Zero;
            case WindowMessage.LButtonUp:
                iFace.ProcessMouseButtonUp(Crow.MouseButton.Left);
                return IntPtr.Zero;
            case WindowMessage.RButtonUp:
                iFace.ProcessMouseButtonUp(Crow.MouseButton.Right);
                return IntPtr.Zero;
            case WindowMessage.MButtonUp:
                iFace.ProcessMouseButtonUp(Crow.MouseButton.Middle);
                return IntPtr.Zero;
			case WindowMessage.MouseWheel:
				iFace.ProcessMouseWheelChanged ((((long)wParam << 32) >> 48) / 120.0f);					
				return IntPtr.Zero;					
			case WindowMessage.MouseHWheel:
				iFace.ProcessMouseWheelChanged ((((long)wParam << 32) >> 48) / 120.0f);
				return IntPtr.Zero;					
			case WindowMessage.KeyDown:
			case WindowMessage.SystemKeyDown:
				handleKeyboard (lParam, wParam, true);
				return IntPtr.Zero;
			case WindowMessage.KeyUp:
			case WindowMessage.SystemKeyUp:					
				handleKeyboard (lParam, wParam, false);
				return IntPtr.Zero;
			case WindowMessage.Char:
				handleChar (lParam, wParam);
				break;
			case WindowMessage.WindowPosChanged:
				HandleWindowPositionChanged(handle, message, wParam, lParam);
				break;


            /*case WindowMessage.Activate:
                HandleActivate(handle, message, wParam, lParam);
                break;

            case WindowMessage.EnterMenuLoop:
            case WindowMessage.EnterSizeMove:
                HandleEnterModalLoop(handle, message, wParam, lParam);
                break;

            case WindowMessage.ExitMenuLoop:
            case WindowMessage.ExitSizeMove:
                HandleExitModalLoop(handle, message, wParam, lParam);
                break;

            case WindowMessage.EraseBackground:
                // This is triggered only when the client area changes.
                // As such it does not affect steady-state performance.
                break;


            case WindowMessage.StyleChanged:
                HandleStyleChanged(handle, message, wParam, lParam);
                break;

            case WindowMessage.Size:
                HandleSize(handle, message, wParam, lParam);
                break;

            case WindowMessage.SetCursor:
                result = HandleSetCursor(handle, message, wParam, lParam);
                break;

            case WindowMessage.CaptureChanged:
                HandleCaptureChanged(handle, message, wParam, lParam);
                break;

            case WindowMessage.Char:
                HandleChar(handle, message, wParam, lParam);
                break;



            case WindowMessage.MouseLeave:
                HandleMouseLeave(handle, message, wParam, lParam);
                break;

            case WindowMessage.MouseWheel:
                HandleMouseWheel(handle, message, wParam, lParam);
                return IntPtr.Zero;

            case WindowMessage.MouseHWheel:
                HandleMouseHWheel(handle, message, wParam, lParam);
                return IntPtr.Zero;


            // Keyboard events:
            case WindowMessage.KeyDown:
            case WindowMessage.KeyUp:
            case WindowMessage.SystemKeyDown:
            case WindowMessage.SystemKeyUp:
                HandleKeyboard(handle, message, wParam, lParam);
                return IntPtr.Zero;

            case WindowMessage.SystemChar:
                return IntPtr.Zero;

            case WindowMessage.KillFocus:
                HandleKillFocus(handle, message, wParam, lParam);
                break;

            case WindowMessage.DropFiles:
                HandleDropFiles(handle, message, wParam, lParam);
                break;

            case WindowMessage.Create:
                HandleCreate(handle, message, wParam, lParam);
                break;
            case WindowMessage.Paint:
                dc = User32.Window.BeginPaint(handle, out paintStruct);

                return IntPtr.Zero;*/
            case WindowMessage.Close:
                User32.Window.DestroyWindow(handle);                   
                return IntPtr.Zero;
            case WindowMessage.Destroy:
                User32.Message.PostQuitMessage(0);
                //User32.WindowClass.UnregisterClass(className, instance);
                break;
            }

            if (result != IntPtr.Zero)
                return result;

            return User32.Window.DefWindowProc(handle, message, wParam, lParam);
        }

		public void SetCursor (MouseCursors newCur)
		{
			throw new NotImplementedException ();
		}

		public void SetCursorPosition (int x, int y)
		{
			throw new NotImplementedException ();
		}
	}
}

