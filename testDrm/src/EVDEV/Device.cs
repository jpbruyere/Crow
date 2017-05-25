//
// Device.cs
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
using System.Runtime.InteropServices;

namespace Linux.Evdev
{
	public enum GrabMode {
		Grab	= 3,
		Ungrab	= 4
	}

	#region Native structs
	[StructLayout(LayoutKind.Sequential)]
	public struct InputAbsInfo
	{
		public int Value;
		public int Minimum;
		public int Maximum;
		public int Fuzz;
		public int Flat;
		public int Resolution;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct InputId
	{
		public ushort BusType;
		public ushort Vendor;
		public ushort Product;
		public ushort Version;
	}

	[StructLayout(LayoutKind.Sequential)]
	unsafe public struct KeyMapEntry
	{
		public byte Flags;
		public byte Length;
		public ushort Index;
		public uint Keycode;
		fixed sbyte scancode[32];

		public string ScanCode {
			get {
				fixed(sbyte* bytes = scancode) {
					string test = new string (bytes);
					return test;
				}

			}
		}
		public override string ToString ()
		{
			return string.Format ("Flags={0} Length={1} Index={2} KeyCode={3} Scancode={4}",
				Flags, Length, Index, Keycode, ScanCode);
		}

	}

	[StructLayout(LayoutKind.Sequential)]
	public struct InputEvent
	{
		public TimeVal Time;
		ushort type;
		public ushort Code;
		public int Value;

		public EvType Type { get { return (EvType)type; } }
		public string CodeName { get { return Marshal.PtrToStringAuto(Device.libevdev_event_code_get_name ((uint)type,(uint)Code)); } }
		public override string ToString ()
		{
			return string.Format ("[Event: {0}:{1} Type={2} Code={3} Value={4} CodeName={5}]",
				Time.Seconds, Time.MicroSeconds, Type, Code, Value, CodeName);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TimeVal
	{
		public IntPtr Seconds;
		public IntPtr MicroSeconds;
	}
	#endregion

	public class Device : IDisposable
	{
		public const int SUCCESS = 0;
		public const int SYNC = 1;

		const string libevdev = "libevdev";

		#region pinvoke
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_new_from_fd (int fd, out IntPtr handle);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void libevdev_free (IntPtr dev);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr libevdev_get_name (IntPtr dev);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_get_id_bustype (IntPtr dev);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_get_id_product (IntPtr dev);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_get_id_vendor (IntPtr dev);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_get_id_version (IntPtr dev);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr libevdev_get_phys (IntPtr dev);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr libevdev_get_uniq (IntPtr dev);

		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr libevdev_event_type_get_name (uint type);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr libevdev_event_code_get_name (uint type, uint code);

		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_get_repeat (IntPtr dev, out int delay, out int period);

		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_has_event_type (IntPtr dev, EvType type);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_has_event_code (IntPtr dev, EvType type, uint code);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_has_property (IntPtr dev, PropertyType prop);

		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_grab (IntPtr dev, GrabMode mode);
		[DllImport(libevdev, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int libevdev_next_event (IntPtr dev, ReadFlag flags, out InputEvent evt);
		#endregion

		int fd = -1;
		IntPtr handle = IntPtr.Zero;

		public Device (string devPath)
		{
			fd = Libc.open(devPath, OpenFlags.ReadOnly | OpenFlags.NonBlock);
			if (fd < 0)
				throw new Exception (string.Format ("EVDEV: faile to open {0}", devPath));
			int ret = libevdev_new_from_fd (fd, out handle);
			if (ret < 0)
				throw new Exception (string.Format ("EVDEV: ibevdev_new_from_fd failed on {0}: {1}", devPath, ret));			
		}
		public Device (int num) : this ("/dev/input/event" + num)
		{			
		}

		public string Name { get { return Marshal.PtrToStringAuto(libevdev_get_name (handle)); }}
		public string PhysLocation { get { return Marshal.PtrToStringAuto(libevdev_get_phys (handle)); }}
		public string UniqueId { get { return Marshal.PtrToStringAuto (libevdev_get_uniq (handle)); }}
		public int BusTypeId { get { return libevdev_get_id_bustype (handle); }}
		public int ProductId { get { return libevdev_get_id_product (handle); }}
		public int VendorId { get { return libevdev_get_id_vendor (handle); }}
		public int VersionId { get { return libevdev_get_id_version (handle); }}

		public bool HasEventOfType (EvType evType) => libevdev_has_event_type (handle, evType) > 0;
		public bool HasEventCodeOfType (EvType evType, uint evCode) => libevdev_has_event_code (handle, evType, evCode) > 0;
		public bool HasEventOfType (PropertyType propType) => libevdev_has_property (handle, propType) > 0;

		public bool GetNextEvent (out InputEvent evt){
			return libevdev_next_event (handle, ReadFlag.Normal, out evt) == 0;
		}

		public bool TryGrab (){
			return libevdev_grab (handle, GrabMode.Grab) == 0;
		}
		public bool TryRelease (){
			return libevdev_grab (handle, GrabMode.Ungrab) == 0;
		}
		const uint EVIOCGKEYCODE	= 0x80084504;
		const uint EVIOCGKEYCODE_V2	= 0x80284504;

		public void test(){
			int[] codes = new int[2];

			for (int i=0; i<130; i++) {
				
				codes[0] = i;
				int ret = Libc.ioctl (fd, EVIOCGKEYCODE, codes);
				if(ret!=0) {
					Console.WriteLine ("evdev ioctl error: " + ret);
				}else
					Console.WriteLine ("[0]= {0}, [1] = {1}\n",	codes[0], codes[1]);
			}
		}
		public void test2(){
			KeyMapEntry kme = default(KeyMapEntry);

			for (int i=0; i<130; i++) {
				kme.Flags = 1;
				kme.Index = (ushort)i;
				kme.Length = sizeof(uint);
				int ret = Libc.ioctl (fd, EVIOCGKEYCODE_V2, ref kme);
				if(ret!=0) {
					Console.WriteLine ("evdev ioctl error: " + ret);
				}else
					Console.WriteLine (kme.ToString());
			}
		}
//		public KeyMapEntry GetKeyMapEntry (KeyType k){
//			KeyMapEntry kme;
//			return kme;
//		}
		#region IDisposable implementation
		~Device(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (handle != IntPtr.Zero)
				libevdev_free (handle);
			handle = IntPtr.Zero;
			if (fd > 0)
				Libc.close(fd);
			fd = -1;
		}
		#endregion
	}
}

