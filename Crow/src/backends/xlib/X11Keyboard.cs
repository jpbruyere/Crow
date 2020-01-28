// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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
		static extern IntPtr XGetModifierMapping (IntPtr disp);
		[DllImport ("libX11")]
		static extern uint XKeycodeToKeysym (IntPtr display, int keycode, int index);
		#endregion

		#region IKeyboard implementation

		public event EventHandler<KeyEventArgs> KeyDown;
		public event EventHandler<KeyEventArgs> KeyUp;
		//public event EventHandler<KeyPressEventArgs> KeyPress;

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

			IntPtr modmap_unmanaged = XGetModifierMapping (xDisp);
			int nummodmap = nummodmap = Marshal.ReadInt32(modmap_unmanaged);

			for (int i = 0; i < nummodmap; i++) {
				Console.WriteLine (Marshal.ReadByte(modmap_unmanaged, i + 4));
			}
			XFree (modmap_unmanaged);


			//unsafe {
			//	byte* modmap_unmanaged = XGetModifierMapping (xDisp);
			//	int nummodmap = 0;
			//	int* ptr = (int*)modmap_unmanaged;
			//	nummodmap = ptr [0];

			//	for (int i = 0; i< nummodmap; i++) {
			//		Console.WriteLine(modmap_unmanaged[i+4]);
			//	}
			//	XFree ((IntPtr)modmap_unmanaged);
			//}
		}
	}
}

