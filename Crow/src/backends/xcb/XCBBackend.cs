﻿// Copyright (c) 2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Crow.XCB
{
	using xcb_window_t = System.UInt32;
	using xcb_colormap_t = System.UInt32;
	using xcb_visualid_t = System.UInt32;
	using xcb_keycode_t = System.Byte;
	using xcb_timestamp_t = System.UInt32;

	using xcb_void_cookie_t = System.UInt32;
	using xcb_intern_atom_cookie_t = System.UInt32;
	using xcb_atom_t = System.UInt32;

	public class XCBBackend : IBackend
	{
		const byte XCB_COPY_FROM_PARENT = 0;

		#region struct an enums

		enum xcb_window_class_t : ushort {
			COPY_FROM_PARENT = 0,
			INPUT_OUTPUT = 1,
			INPUT_ONLY = 2
		}

		enum xcb_button_t : byte {
			Left = 1,
			Middle,
			Right,
			WheelUp,
			WheelDown,
			But6,
			But7,
			But8,
			But9,
			But10,
		}

		[Flags]
		enum xcb_event_mask_t : uint {
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

		enum xcb_event_type : byte {
			KEY_PRESS = 2,
			KEY_RELEASE,
			BUTTON_PRESS,
			BUTTON_RELEASE,
			MOTION_NOTIFY,
			ENTER_NOTIFY,
			LEAVE_NOTIFY,
			FOCUS_IN,
			FOCUS_OUT,
			KEYMAP_NOTIFY,
			EXPOSE,
			GRAPHICS_EXPOSURE,
			NO_EXPOSURE,
			VISIBILITY_NOTIFY,
			CREATE_NOTIFY,
			DESTROY_NOTIFY,
			UNMAP_NOTIFY,
			MAP_NOTIFY,
			MAP_REQUEST,
			REPARENT_NOTIFY,
			CONFIGURE_NOTIFY,
			CONFIGURE_REQUEST,
			GRAVITY_NOTIFY,
			RESIZE_REQUEST,
			CIRCULATE_NOTIFY,
			CIRCULATE_REQUEST,
			PROPERTY_NOTIFY,
			SELECTION_CLEAR,
			SELECTION_REQUEST,
			SELECTION_NOTIFY,
			COLORMAP_NOTIFY,
			CLIENT_MESSAGE,
			MAPPING_NOTIFY,
			GE_GENERIC,
		}

		[Flags]
		enum xcb_cw_t : uint {
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
		enum xcb_prop_mode : byte
		{
			Replace,//Discard the previous property value and store the new data.
			Prepend,//Insert the new data before the beginning of existing data.The format must match existing property value.If the property is undefined, it is treated as defined with the correct type and format with zero-length data.
			Append,//Insert the new data after the beginning of existing data.The format must match existing property value.If the property is undefined, it is treated as defined with the correct type and format with zero-length data.
		}


	[StructLayout(LayoutKind.Sequential)]
		struct xcb_generic_event_t{
			public xcb_event_type response_type;  /**< Type of the response */
			public byte pad0;           /**< Padding */
			public UInt16 sequence;       /**< Sequence number */
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
			UInt32[] pad;
			public UInt32 full_sequence;  /**< full sequence */
		}

		[StructLayout(LayoutKind.Explicit, Size = 32)]
		struct xcb_event_t{
			[FieldOffsetAttribute(0)]
			public xcb_event_type response_type;

			[FieldOffsetAttribute(1)]
			public byte detail;
			[FieldOffsetAttribute(1)]
			public xcb_keycode_t keycode;
			[FieldOffsetAttribute(1)]
			public xcb_button_t button;

			[FieldOffsetAttribute(2)]
			public UInt16 sequence;
			[FieldOffsetAttribute(4)]
			public xcb_timestamp_t time;
			[FieldOffsetAttribute(8)]
			public xcb_window_t root;

			//expose event fields
			[FieldOffsetAttribute(4)]
			public xcb_window_t window;
			[FieldOffsetAttribute(8)]
			public UInt16 x;
			[FieldOffsetAttribute(10)]
			public UInt16 y;
			[FieldOffsetAttribute(12)]
			public UInt16 width;
			[FieldOffsetAttribute(14)]
			public UInt16 height;
			[FieldOffsetAttribute(14)]
			public UInt16 count;
			/// 

			[FieldOffsetAttribute(12)]
			public xcb_window_t evt;
			[FieldOffsetAttribute(16)]
			public xcb_window_t child;
			[FieldOffsetAttribute(20)]
			public UInt16 root_x;
			[FieldOffsetAttribute(22)]
			public UInt16 root_y;
			[FieldOffsetAttribute(24)]
			public UInt16 event_x;
			[FieldOffsetAttribute(26)]
			public UInt16 event_y;
			[FieldOffsetAttribute(28)]
			public UInt16 state;
			[FieldOffsetAttribute(30)]
			public byte same_screen;

			[FieldOffsetAttribute(31)]
			public byte same_screen_focus;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct xcb_key_press_event_t {
			public xcb_event_type response_type;
			public xcb_keycode_t detail;
			public UInt16 sequence;
			public xcb_timestamp_t time;
			public xcb_window_t root;
			public xcb_window_t evt;
			public xcb_window_t child;
			public UInt16 root_x;
			public UInt16 root_y;
			public UInt16 event_x;
			public UInt16 event_y;
			public UInt16 state;
			public byte same_screen;
			byte pad0;
		}
		[StructLayout(LayoutKind.Sequential)]
		struct xcb_button_press_event_t {
			public xcb_event_type response_type;
			public xcb_button_t detail;
			public UInt16 sequence;
			public xcb_timestamp_t time;
			public xcb_window_t root;
			public xcb_window_t evt;
			public xcb_window_t child;
			public UInt16 root_x;
			public UInt16 root_y;
			public UInt16 event_x;
			public UInt16 event_y;
			public UInt16 state;
			public byte same_screen;
			byte pad0;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct xcb_motion_notify_event_t {
			public xcb_event_type response_type;
			public byte detail;
			public UInt16 sequence;
			public xcb_timestamp_t time;
			public xcb_window_t root;
			public xcb_window_t evt;
			public xcb_window_t child;
			public UInt16 root_x;
			public UInt16 root_y;
			public UInt16 event_x;
			public UInt16 event_y;
			public UInt16 state;
			public byte same_screen;
			byte pad0;
		}
		[StructLayout(LayoutKind.Sequential)]
		struct xcb_iterator_t {
			public IntPtr data;
			public int rem;
			public int index;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct xcb_screen_t {
			public xcb_window_t root;
			public xcb_colormap_t default_colormap;
			public UInt32 white_pixel;
			public UInt32 black_pixel;
			public UInt32 current_input_masks;
			public UInt16 width_in_pixels;
			public UInt16 height_in_pixels;
			public UInt16 width_in_millimeters;
			public UInt16 height_in_millimeters;
			public UInt16 min_installed_maps;
			public UInt16 max_installed_maps;
			public xcb_visualid_t root_visual;
			public byte backing_stores;
			public byte save_unders;
			public byte root_depth;
			public byte allowed_depths_len;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct xcb_visualtype_t {
			public xcb_visualid_t visual_id;
			public byte _class;
			public byte  bits_per_rgb_value;
			public UInt16 colormap_entries;
			public UInt32 red_mask;
			public UInt32 green_mask;
			public UInt32 blue_mask;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			byte[] pad;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct xcb_generic_reply_t{
			public byte response_type;  /**< Type of the response */
			byte pad0;           /**< Padding */
			public UInt16 sequence;       /**< Sequence number */
			public UInt32 length;         /**< Length of the response */
		}
		[StructLayout (LayoutKind.Sequential)]
		struct xcb_intern_atom_reply_t
		{
			byte response_type;
			byte pad0;
			UInt16 sequence;
			UInt32 length;
			xcb_atom_t atom;
		}
		#endregion


		#region pinvoke XCB
		[DllImport ("xcb")]
		static extern IntPtr xcb_connect(string displayName, IntPtr screenNum);
		[DllImport ("xcb")]
		static extern IntPtr xcb_get_setup(IntPtr connection);		 
		[DllImport ("xcb")]
		static extern IntPtr xcb_flush(IntPtr connection);
		[DllImport ("xcb")]
		static extern UInt32 xcb_generate_id(IntPtr connection);

		[DllImport("xcb")]
		static extern xcb_iterator_t xcb_setup_roots_iterator(IntPtr setup);
		[DllImport("xcb")]
		static extern xcb_iterator_t xcb_screen_allowed_depths_iterator(IntPtr scr);
		[DllImport("xcb")]
		static extern xcb_iterator_t xcb_depth_visuals_iterator(IntPtr depth);

		[DllImport("xcb")]
		static extern void xcb_screen_next(ref xcb_iterator_t scr_iterator);
		[DllImport("xcb")]
		static extern void xcb_depth_next(ref xcb_iterator_t depth_iterator);
		[DllImport("xcb")]
		static extern void xcb_visualtype_next(ref xcb_iterator_t depth_visual_iterator);

		[DllImport("xcb")]
		static extern xcb_void_cookie_t xcb_create_window(IntPtr connection, byte depth, xcb_window_t win, UInt32 parent,
			Int16 x, Int16 y, UInt16 width, UInt16 height, UInt16 border,
			xcb_window_class_t _class, xcb_visualid_t visual, xcb_cw_t mask, IntPtr valueList);
		[DllImport("xcb")]
		static extern xcb_void_cookie_t xcb_map_window(IntPtr conn, xcb_window_t window);
		[DllImport("xcb")]
		static extern void xcb_disconnect(IntPtr connection);

		[DllImport("xcb")]
		static extern IntPtr xcb_poll_for_event(IntPtr connection);

		[DllImport ("xcb")]
		static extern xcb_void_cookie_t xcb_change_window_attributes (IntPtr conn, xcb_window_t window, xcb_cw_t value_mask, IntPtr value_list);

		[DllImport ("xcb")]//in xcbproto
		static extern xcb_void_cookie_t xcb_free_cursor (IntPtr connection, UInt32 cursor);

		[DllImport ("xcb")]
		static extern xcb_intern_atom_cookie_t xcb_intern_atom (IntPtr connection, byte onlyIfExists, UInt16 nameLength, string name);
		[DllImport ("xcb")]
		static extern IntPtr xcb_intern_atom_reply (IntPtr connection, xcb_intern_atom_cookie_t cookie, IntPtr xcb_generic_error);
		[DllImport ("xcb")]
		static extern xcb_void_cookie_t xcb_change_property (IntPtr connection, xcb_prop_mode mode, xcb_window_t window, xcb_atom_t property, xcb_atom_t type, byte format, UInt32 data_len, ref IntPtr data);

		//TODO: there should be a generic free method in xcb or at least xcb_free_event!!
		[DllImport ("X11")]
		static extern IntPtr XFree (IntPtr data);
		//[DllImport ("X11")]
		//static extern int XInitThreads ();
		//[DllImport ("X11")]
		//static extern void XLockDisplay (IntPtr display);
		//[DllImport ("X11")]
		//static extern void XUnlockDisplay (IntPtr display);

		#region xcb_cursor
		[DllImport ("xcb-cursor")]
		static extern int xcb_cursor_context_new (IntPtr conn, IntPtr screen, out IntPtr ctx);
		[DllImport ("xcb-cursor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		static extern UInt32 xcb_cursor_load_cursor (IntPtr ctx, [MarshalAs (UnmanagedType.LPStr)]string name);
		[DllImport ("xcb-cursor")]
		static extern void xcb_cursor_context_free (IntPtr ctx);

		#endregion

		#endregion

		Interface iFace;

		IntPtr conn;
		xcb_window_t win;

		IntPtr cursorCtx;
		Dictionary<MouseCursor, xcb_window_t> cursors;

		XKB.XCBKeyboard Keyboard;

		xcb_atom_t wmQuitAtom;

		#region IBackend implementation
		public void Init (Interface _iFace)
		{
			iFace = _iFace;

			conn = xcb_connect (null, IntPtr.Zero);

			Keyboard = new  XKB.XCBKeyboard (conn, iFace);

			xcb_iterator_t scr_it = xcb_setup_roots_iterator (xcb_get_setup (conn));
			IntPtr screen = scr_it.data;

			xcb_screen_t scr = (xcb_screen_t)Marshal.PtrToStructure (screen, typeof(xcb_screen_t));

			win = xcb_generate_id (conn);

			xcb_cw_t mask = xcb_cw_t.BACK_PIXEL | xcb_cw_t.EVENT_MASK;
			uint [] values = {
				scr.black_pixel,
				(uint)(
					xcb_event_mask_t.EXPOSURE |
					xcb_event_mask_t.POINTER_MOTION |
					xcb_event_mask_t.BUTTON_PRESS |
					xcb_event_mask_t.BUTTON_RELEASE |
					xcb_event_mask_t.KEY_PRESS |
					xcb_event_mask_t.KEY_RELEASE
				)
			};

			xcb_intern_atom_cookie_t cookie = xcb_intern_atom (conn, 1, 12, "WM_PROTOCOLS");
			IntPtr reply = xcb_intern_atom_reply (conn, cookie, IntPtr.Zero);

			xcb_intern_atom_cookie_t cookie2 = xcb_intern_atom (conn, 0, 16, "WM_DELETE_WINDOW");
			IntPtr reply2 = xcb_intern_atom_reply (conn, cookie2, IntPtr.Zero);


			GCHandle hndValues = GCHandle.Alloc (values, GCHandleType.Pinned);

			xcb_create_window (conn, XCB_COPY_FROM_PARENT, win, scr.root, 0,0,(ushort)iFace.ClientRectangle.Width, (ushort)iFace.ClientRectangle.Height,0,
					xcb_window_class_t.INPUT_OUTPUT, scr.root_visual, mask, hndValues.AddrOfPinnedObject());

			hndValues.Free ();

			IntPtr tmp = Marshal.ReadIntPtr (reply2, 8);
			wmQuitAtom = (xcb_atom_t)tmp;

			xcb_change_property (conn, xcb_prop_mode.Replace, win, (xcb_atom_t)Marshal.ReadInt32(reply,8), 4, 32, 1, ref tmp);

			xcb_map_window (conn, win);

			xcb_flush (conn);

			IntPtr visual = findVisual (scr_it, scr.root_visual);

			if (xcb_cursor_context_new (conn, screen, out cursorCtx) < 0)
				throw new Exception ("Could not initialize xcb-cursor");

			loadCursors ();

			iFace.surf = new Cairo.XcbSurface (conn, win, visual, iFace.ClientRectangle.Width, iFace.ClientRectangle.Height);
		}

		public void CleanUp ()
		{
			Keyboard.Destroy ();

			foreach (xcb_window_t cur in cursors.Values) 
				xcb_free_cursor (conn, cur);

			xcb_cursor_context_free (cursorCtx);

			xcb_disconnect (conn);	
		}
		public void Flush () {
			xcb_flush (conn);
		}

		public void ProcessEvents ()
		{
			IntPtr evtPtr = xcb_poll_for_event (conn);
			if (evtPtr == IntPtr.Zero)
				return;
			xcb_event_t evt = (xcb_event_t)Marshal.PtrToStructure (evtPtr, typeof(xcb_event_t));

			switch ((xcb_event_type)((uint)evt.response_type & ~0x80u)) {
			case xcb_event_type.EXPOSE:
				if (evt.width > 0)
					iFace.ProcessResize (new Rectangle (0, 0, evt.width, evt.height));
				break;
			case xcb_event_type.MOTION_NOTIFY:
				iFace.OnMouseMove (evt.event_x, evt.event_y);
				break;
			case xcb_event_type.BUTTON_PRESS:
				if (evt.button == xcb_button_t.WheelUp)
					iFace.OnMouseWheelChanged (Interface.WheelIncrement);
				else if(evt.button == xcb_button_t.WheelDown)
					iFace.OnMouseWheelChanged (-Interface.WheelIncrement);
				else
					iFace.OnMouseButtonDown ((MouseButton)(evt.detail - 1));				
				break;
			case xcb_event_type.BUTTON_RELEASE:
				if (evt.button == xcb_button_t.WheelUp || evt.button == xcb_button_t.WheelDown)
					break;
				iFace.OnMouseButtonUp ((MouseButton)(evt.detail - 1));
				break;
			case xcb_event_type.KEY_PRESS:
				Keyboard.HandleEvent (evt.keycode, true);
				break;
			case xcb_event_type.KEY_RELEASE:
				Keyboard.HandleEvent (evt.keycode, false);
				break;
			case xcb_event_type.CLIENT_MESSAGE:
				if ((xcb_atom_t)Marshal.ReadInt32 (evtPtr, 12)==wmQuitAtom)
					iFace.Quit ();
				break;
			default:
				Console.WriteLine ($"unknown event: {evt.response_type}");
				break;
			}
			XFree (evtPtr);
		}
		public bool IsDown (Key key) {
			return false;
		}
		public bool Shift {
			get { return Keyboard.Shift; }
		}
		public bool Ctrl {
			get { return Keyboard.Ctrl; }
		}
		public bool Alt {
			get { return Keyboard.Alt;}
		}

		public MouseCursor Cursor {
			set {
				GCHandle hndValues = GCHandle.Alloc (cursors [value], GCHandleType.Pinned);
				xcb_void_cookie_t res = xcb_change_window_attributes (conn, win, xcb_cw_t.CURSOR, hndValues.AddrOfPinnedObject ());
				hndValues.Free ();
				xcb_flush (conn);
			}
		}
		#endregion

		UInt32 tryGetCursor (params string[] names)
		{
			for (int i = 0; i < names.Length; i++) {
				xcb_window_t cur = xcb_cursor_load_cursor (cursorCtx, names[i]);
				if (cur != 0)
					return cur;
			}
			return 0;
		}


		void loadCursors ()
		{
			cursors = new Dictionary<MouseCursor, xcb_window_t> ();
			//Load cursors
			cursors.Add (MouseCursor.Arrow, tryGetCursor("arrow", "default"));
			cursors.Add (MouseCursor.IBeam, tryGetCursor ("text", "ibeam"));
			cursors.Add (MouseCursor.Crosshair, tryGetCursor ("cross", "crosshair"));
			cursors.Add (MouseCursor.Hand, tryGetCursor ("hand", "hand2", "hand1", "pointing_hand"));
			cursors.Add (MouseCursor.Move, tryGetCursor ("move"));
			cursors.Add (MouseCursor.Circle, tryGetCursor ("circle"));
			cursors.Add (MouseCursor.H, tryGetCursor ("ew-resize"));
			cursors.Add (MouseCursor.V, tryGetCursor ("ns-resize"));
			cursors.Add (MouseCursor.NW, tryGetCursor ("nw-resize"));
			cursors.Add (MouseCursor.NE, tryGetCursor ("ne-resize"));
			cursors.Add (MouseCursor.SW, tryGetCursor ("sw-resize"));
			cursors.Add (MouseCursor.SE, tryGetCursor ("se-resize"));
			cursors.Add (MouseCursor.TopLeft, tryGetCursor ("nw-resize"));
			cursors.Add (MouseCursor.Top, tryGetCursor ("n-resize"));
			cursors.Add (MouseCursor.TopRight, tryGetCursor ("ne-resize"));
			cursors.Add (MouseCursor.Left, tryGetCursor ("w-resize"));
			cursors.Add (MouseCursor.Right, tryGetCursor ("e-resize"));
			cursors.Add (MouseCursor.BottomLeft, tryGetCursor ("sw-resize"));
			cursors.Add (MouseCursor.Bottom, tryGetCursor ("s-resize"));
			cursors.Add (MouseCursor.BottomRight, tryGetCursor ("se-resize"));
		}

		static IntPtr findVisual (xcb_iterator_t scr_it, xcb_visualid_t visualId){
			for (; scr_it.rem > 0; xcb_screen_next (ref scr_it)) {
				xcb_iterator_t depth_it = xcb_screen_allowed_depths_iterator (scr_it.data);
				for (; depth_it.rem > 0; xcb_depth_next (ref depth_it)) {
					xcb_iterator_t visual_it = xcb_depth_visuals_iterator (depth_it.data);
					for (; visual_it.rem > 0; xcb_visualtype_next (ref visual_it)) {
						xcb_visualtype_t visual = (xcb_visualtype_t)Marshal.PtrToStructure (visual_it.data, typeof(xcb_visualtype_t));
						if (visualId == visual.visual_id)
							return visual_it.data;
					}
				}
			}
			return IntPtr.Zero;
		}

	}
}
