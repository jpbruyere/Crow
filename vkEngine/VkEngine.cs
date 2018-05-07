//
// VkEngine.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2018 jp
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
using vkh;

namespace vkglfw
{
	public delegate void VKEmousebuttonfun(IntPtr win, MouseButton but, KeyAction action, KeyModifiers mods);
	public delegate void VKEcursorposfun(IntPtr win, double x, double y);
	public delegate void VKEscrollfun(IntPtr win, double xdelta, double ydelta);
	public delegate void VKEkeyfun(IntPtr win, Key key, int scancode, KeyAction action, KeyModifiers mods);
	public delegate void VKEcharfun(IntPtr win, uint codepoint);

	public class VkEngine : IDisposable
	{
		const string libvkglfw = "vkglfw";

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkengine_create (VkPhysicalDeviceType devType, uint width, uint height);

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkengine_close (IntPtr e);

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkengine_destroy (IntPtr vkengine_handle);

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkengine_blitter_run (IntPtr vkengine_handle, IntPtr vkImage);

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkengine_get_device (IntPtr vkengine_handle);

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkengine_get_physical_device (IntPtr vkengine_handle);

		//[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern void vkengine_get_queues_properties (IntPtr e, ref IntPtr ptrProps, out uint count);

		//[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		//internal static extern void vkengine_free_ptr (IntPtr ptr);

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr vkengine_get_queue (IntPtr vkengine_handle);

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern uint vkengine_get_queue_fam_idx (IntPtr vkengine_handle);

		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkengine_set_mouse_but_callback (IntPtr e, VKEmousebuttonfun onMouseBut);
		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkengine_set_cursor_pos_callback (IntPtr e, VKEcursorposfun onMouseMove);
		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkengine_set_scroll_callback (IntPtr e, VKEscrollfun onScroll);
		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkengine_set_key_callback (IntPtr e, VKEkeyfun onKey);
		[DllImport (libvkglfw, CallingConvention=CallingConvention.Cdecl)]
		internal static extern void vkengine_set_char_callback (IntPtr e, VKEcharfun onChar);


		void onMouseButton(IntPtr win, MouseButton but, KeyAction action, KeyModifiers mods){}
		void onMouseMove(IntPtr win, double x, double y){
			Console.WriteLine("mouse ({0},{1})", x, y);
		}
		void onScroll(IntPtr win, double xdelta, double ydelta){}
		void onKey(IntPtr win, Key key, int scancode, KeyAction action, KeyModifiers mods){
			if (key == Key.Escape && action == KeyAction.Press)
				vkengine_close (handle);
		}
		void onChar(IntPtr win, uint codepoint){}

		IntPtr handle = IntPtr.Zero;

		public VkEngine (int width, int height)
		{
			handle = vkengine_create (VkPhysicalDeviceType.DiscreteGPU, (uint)width, (uint)height);

			vkengine_set_mouse_but_callback (handle, onMouseButton);
			vkengine_set_cursor_pos_callback (handle, onMouseMove);
			vkengine_set_scroll_callback (handle, onScroll);
			vkengine_set_key_callback (handle, onKey);
			vkengine_set_char_callback (handle, onChar);
		}
		~VkEngine ()
		{
			Dispose (false);
		}

//		public QueueFamilyProperties[] AvailableQueues {
//			get {
//				IntPtr ptr = IntPtr.Zero, p;
//				uint count;
//				vkengine_get_queues_properties (handle, ref ptr, out count);
//				QueueFamilyProperties[] qfps = new QueueFamilyProperties[count];
//				p = ptr;
//				for (int i = 0; i < count; i++) {
//					qfps[i] = (QueueFamilyProperties)Marshal.PtrToStructure(p, typeof(QueueFamilyProperties));
//					p += Marshal.SizeOf(typeof(QueueFamilyProperties));
//				}
//				vkengine_free_ptr (ptr);
//				return qfps;
//			}
//		}

		public IntPtr Handle { get { return handle; }}
		public IntPtr Device { get { return vkengine_get_device (handle); }}
		public IntPtr Phy { get { return vkengine_get_physical_device (handle); }}
		public IntPtr Queue { get { return vkengine_get_queue (handle); }}
		public uint QueueFamIdx { get { return vkengine_get_queue_fam_idx (handle); }}

		public void Run (vkvg.Surface surf) {
			vkengine_blitter_run (handle, surf.VkImage);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing || handle == IntPtr.Zero)
				return;

			vkengine_destroy (handle);
			handle = IntPtr.Zero;
		}
		#endregion
	}
}

