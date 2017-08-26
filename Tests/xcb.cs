//
// xcb.cs
//
// Author:
//       jp <>
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

using XCBConnection = System.IntPtr;
using XCBSetup = System.IntPtr;
using XCBScreen = System.IntPtr;
using XCBWindow = System.UInt32;
using XCBColorMap = System.UInt32;
using XCBVisualId = System.UInt32;

using System.Runtime.InteropServices;

namespace Native
{
	

	public static class Xcb
	{		

		const string lib = "libxcb.so.1";

		public enum Result {
			SUCCESS = 0,
			ERROR = 1,				/** xcb connection errors because of socket, pipe and other stream errors. */
			EXT_NOTSUPPORTED = 2,	/** xcb connection shutdown because of extension not supported */		
			MEM_INSUFFICIENT = 3,	/** malloc(), calloc() and realloc() error upon failure, for eg ENOMEM */		
			REQ_LEN_EXCEED = 4,		/** Connection closed, exceeding request length that server accepts. */		
			PARSE_ERR = 5,			/** Connection closed, error during parsing display string. */		
			INVALID_SCREEN = 6,		/** Connection closed because the server does not have a screen matching the display. */		
			FDPASSING_FAILED = 7,	/** Connection closed because some FD passing operation failed */
		}
		public enum EventMask : uint {
			NO_EVENT = 0,
			KEY_PRESS = 1,
			KEY_RELEASE = 2,
			BUTTON_PRESS = 4,
			BUTTON_RELEASE = 8,
			ENTER_WINDOW = 16,
			LEAVE_WINDOW = 32,
			POINTER_MOTION = 64,
			POINTER_MOTION_HINT = 128,
			BUTTON_1_MOTION = 256,
			BUTTON_2_MOTION = 512,
			BUTTON_3_MOTION = 1024,
			BUTTON_4_MOTION = 2048,
			BUTTON_5_MOTION = 4096,
			BUTTON_MOTION = 8192,
			KEYMAP_STATE = 16384,
			EXPOSURE = 32768,
			VISIBILITY_CHANGE = 65536,
			STRUCTURE_NOTIFY = 131072,
			RESIZE_REDIRECT = 262144,
			SUBSTRUCTURE_NOTIFY = 524288,
			SUBSTRUCTURE_REDIRECT = 1048576,
			FOCUS_CHANGE = 2097152,
			PROPERTY_CHANGE = 4194304,
			COLOR_MAP_CHANGE = 8388608,
			OWNER_GRAB_BUTTON = 16777216
		}
		public enum WindowClass : ushort {
			COPY_FROM_PARENT = 0,
			INPUT_OUTPUT = 1,
			INPUT_ONLY = 2
		}
		public enum Cw : uint {
			BACK_PIXMAP = 1,
			BACK_PIXEL = 2,
			BORDER_PIXMAP = 4,
			BORDER_PIXEL = 8,
			BIT_GRAVITY = 16,
			WIN_GRAVITY = 32,
			BACKING_STORE = 64,
			BACKING_PLANES = 128,
			BACKING_PIXEL = 256,
			OVERRIDE_REDIRECT = 512,
			SAVE_UNDER = 1024,
			EVENT_MASK = 2048,
			DONT_PROPAGATE = 4096,
			COLORMAP = 8192,
			CURSOR = 16384
		}

		public enum EventType : byte {
			KEY_PRESS = 2,
			KEY_RELEASE = 3,
			BUTTON_PRESS = 4,
			BUTTON_RELEASE = 5,
			MOTION_NOTIFY = 6,
			ENTER_NOTIFY = 7,
			LEAVE_NOTIFY = 8,
			FOCUS_IN = 9,
			FOCUS_OUT = 10,
			KEYMAP_NOTIFY = 11,
			EXPOSE = 12
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct GenericIterator {
			public IntPtr data;   /**< Data of the current iterator */
			public int rem;    /**< remaining elements */
			public int index;  /**< index of the current iterator */
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct Screen {
			public XCBWindow	root;
			public XCBColorMap	default_colormap;
			public uint			white_pixel;
			public uint			black_pixel;
			public uint			current_input_masks;
			public ushort		width_in_pixels;
			public ushort		height_in_pixels;
			public ushort		width_in_millimeters;
			public ushort		height_in_millimeters;
			public ushort		min_installed_maps;
			public ushort		max_installed_maps;
			public XCBVisualId	root_visual;
			public byte			backing_stores;
			public byte			save_unders;
			public byte			root_depth;
			public byte			allowed_depths_len;
		}
		[StructLayout(LayoutKind.Explicit,Size=8)]
//		[StructLayout(LayoutKind.Sequential)]
		public struct Depth {
			[FieldOffset(0)]	public byte		depth;
			[FieldOffset(2)]	public ushort	visuals_len;
//			public byte  depth;
//			public byte  pad0;
//			public ushort visuals_len;
//			public byte  pad1;
//			public byte  pad2;
//			public byte  pad3;
//			public byte  pad4;
		}

		[StructLayout(LayoutKind.Explicit, Size=24)]
		public struct VisualType {
			[FieldOffset(0)]public XCBVisualId	visual_id;
			[FieldOffset(4)]public byte			_class;
			[FieldOffset(5)]public byte			bits_per_rgb_value;
			[FieldOffset(6)]public ushort		colormap_entries;
			[FieldOffset(8)]public uint			red_mask;
			[FieldOffset(12)]public uint			green_mask;
			[FieldOffset(16)]public uint			blue_mask;
		}
		[StructLayout(LayoutKind.Explicit, Size = 36)]
		public struct GenericEvent {
			[FieldOffset(0)]	public EventType	response_type;	// Type of the response
			[FieldOffset(2)]	public ushort		sequence;		// Sequence number
			[FieldOffset(32)]	public uint			full_sequence;	// full sequence
		};

//		struct GenericReply {
//			byte	response_type;
//			byte	pad0;
//			UInt16	sequence;
//			UInt32	length;
//		}

		[DllImport(lib, EntryPoint = "xcb_connect", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static XCBConnection Connect([MarshalAs(UnmanagedType.LPStr)] string displayname = null, IntPtr screen = default(IntPtr));
		[DllImport(lib, EntryPoint = "xcb_disconnect", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static void Disconnect(XCBConnection connection);

		[DllImport(lib, EntryPoint = "xcb_connection_has_error", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static Result ConnectionHasError(XCBConnection connection);
		[DllImport(lib, EntryPoint = "xcb_get_setup", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static XCBSetup GetSetup(XCBConnection connection);
		[DllImport(lib, EntryPoint = "xcb_setup_roots_iterator", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static GenericIterator SetupRootsIterator (XCBSetup setup);
		[DllImport(lib, EntryPoint = "xcb_generate_id", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static XCBWindow GenerateId(XCBConnection connection);

		/*xcb_void_cookie_t
		xcb_create_window (xcb_connection_t *c,
			uint8_t           depth,
			xcb_window_t      wid,
			xcb_window_t      parent,
			int16_t           x,
			int16_t           y,
			uint16_t          width,
			uint16_t          height,
			uint16_t          border_width,
			uint16_t          _class,
			xcb_visualid_t    visual,
			uint32_t          value_mask,
			const void       *value_list);*/

		[DllImport(lib, EntryPoint = "xcb_create_window", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static uint CreateWindow (
			XCBConnection conn, byte depth, XCBWindow wid, XCBWindow parent,
			short x, short y, ushort width, ushort height, ushort border_width, ushort _class,
			XCBVisualId visual, Cw value_mask, IntPtr value_list);
		
		[DllImport(lib, EntryPoint = "xcb_map_window", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static uint MapWindow (XCBConnection conn, XCBWindow window);

		[DllImport(lib, EntryPoint = "xcb_screen_next", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static void ScreenNext (ref GenericIterator iter);
//		[DllImport(lib, EntryPoint = "xcb_screen_allowed_depths_length", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
//		public extern static IntPtr ScreenAllowedDepthsLength (ref Xcb.Screen screen);
		[DllImport(lib, EntryPoint = "xcb_screen_allowed_depths_iterator", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static GenericIterator ScreenAllowedDepthsIterator (ref Xcb.Screen screen);
		[DllImport(lib, EntryPoint = "xcb_depth_next", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static void DepthNext (ref GenericIterator iter);
		[DllImport(lib, EntryPoint = "xcb_depth_visuals_iterator", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static GenericIterator DepthVisualsIterator (ref Xcb.Depth depth);
		[DllImport(lib, EntryPoint = "xcb_visualtype_next", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static void VisualtypeNext (ref GenericIterator iter);

		[DllImport(lib, EntryPoint = "xcb_flush", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static int Flush(XCBConnection conn);
		[DllImport(lib, EntryPoint = "xcb_wait_for_event", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static IntPtr WaitForEvent(XCBConnection conn);

		[DllImport("/mnt/devel/gts/tests/libxcb-helper.so", EntryPoint = "find_visual", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		public extern static IntPtr FindVisual(XCBConnection conn, XCBVisualId visualId);
	}
}

