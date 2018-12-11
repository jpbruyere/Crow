//
// XCBKeyboard.cs
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
using System.Runtime.InteropServices;

namespace Crow.XLib
{
	public class X11Keyboard
	{
		#region PInvoke
		[DllImportAttribute("X11")]
		static extern IntPtr XFree(IntPtr data);
		[DllImport ("libX11")]
		static extern void XDisplayKeycodes (IntPtr disp, out int min, out int max);
		[DllImport ("libX11")]
		static extern IntPtr XGetKeyboardMapping (IntPtr disp, byte first_keycode, int keycode_count, 
			out int keysyms_per_keycode_return);
		[DllImport ("libX11")]
		unsafe static extern byte* XGetModifierMapping (IntPtr disp);
		[DllImport ("libX11")]
		static extern uint XKeycodeToKeysym (IntPtr display, int keycode, int index);
		#endregion

		#region IKeyboard implementation

		public event EventHandler<KeyEventArgs> KeyDown;
		public event EventHandler<KeyEventArgs> KeyUp;
		public event EventHandler<KeyPressEventArgs> KeyPress;

		public void HandleEvent (uint keycode, bool pressed) {
			/*int min_keycode, max_keycode, keysyms_per_keycode;
						XLib.NativeMethods.XDisplayKeycodes (xDisp, out min_keycode, out max_keycode);

						IntPtr ksp = XLib.NativeMethods.XGetKeyboardMapping (xDisp, (byte)min_keycode,
							             max_keycode + 1 - min_keycode, out keysyms_per_keycode);
						XLib.NativeMethods.XFree (ksp);*/

			uint keySym;
			keySym = XKeycodeToKeysym (xDisp, (int)keycode, 0);
			char c = (char)keySym;
			if (pressed)
				KeyDown.Raise (this, new KeyEventArgs ((Key)keySym, false));
			else 
				KeyUp.Raise (this, new KeyEventArgs ((Key)keySym, false));			
		}
		public bool IsDown (Key key) {
			throw new NotImplementedException();
		}
		public bool Shift {
			get {
				throw new NotImplementedException();
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
		public void Destroy () {

		}
		#endregion


		IntPtr xDisp;

		public X11Keyboard (IntPtr _xDisp)
		{
			xDisp = _xDisp;

			int min_keycode, max_keycode, keysyms_per_keycode;

			XDisplayKeycodes (xDisp, out min_keycode, out max_keycode);
			IntPtr ksp = XGetKeyboardMapping (xDisp, (byte) min_keycode,
				max_keycode + 1 - min_keycode, out keysyms_per_keycode);
			XFree (ksp);

			unsafe {
				byte* modmap_unmanaged = XGetModifierMapping (xDisp);
				int nummodmap = 0;
				int* ptr = (int*)modmap_unmanaged;
				nummodmap = ptr [0];

				for (int i = 0; i< nummodmap; i++) {
					Console.WriteLine(modmap_unmanaged[i+4]);
				}
				XFree ((IntPtr)modmap_unmanaged);
			}
		}
	}
}

