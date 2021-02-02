// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Crow
{
	public class XCursorFile
	{
		const uint XC_TYPE_IMG = 0xfffd0002;

		struct toc
		{
			public uint type;
			public uint subtype;
			public uint pos;

			public toc(BinaryReader sr)
			{
				type = sr.ReadUInt32();
				subtype = sr.ReadUInt32();
				pos = sr.ReadUInt32();
			}
		}

		public List<XCursor> Cursors = new List<XCursor>();


		static XCursorFile loadFromStream(Stream s)
		{
			List<toc> tocList = new List<toc> (5);
			XCursorFile tmp = new XCursorFile ();

			using (BinaryReader sr = new BinaryReader (s)) {
				//magic: CARD32 ’Xcur’ (0x58, 0x63, 0x75, 0x72)				
				if (!sr.ReadChars (4).AsSpan ().SequenceEqual("Xcur".AsSpan())) {
					Debug.WriteLine ("XCursor Load error: Wrong magic");
					return null;
				}
				//header: CARD32 bytes in this header
				uint headerLength = sr.ReadUInt32 ();
				//version: CARD32 file version number
				uint version = sr.ReadUInt32 ();
				//ntoc: CARD32 number of toc entries
				uint nbToc = sr.ReadUInt32 ();
				//toc: LISTofTOC table of contents
				for (uint i = 0; i < nbToc; i++) {
					tocList.Add (new toc (sr));
				}

				foreach (toc t in tocList) {
					if (t.type != XC_TYPE_IMG)
						continue;
					sr.BaseStream.Seek (t.pos, SeekOrigin.Begin);
					tmp.Cursors.Add(imageLoad (sr));						
				}
			}
			return tmp;
		}

		public static XCursorFile Load(Interface iFace, string path)
		{
			return loadFromStream (iFace.GetStreamFromPath (path));
		}

		static XCursor imageLoad (BinaryReader sr)
		{
			XCursor tmp = new XCursor ();
			//			header: 36 Image headers are 36 bytes
			uint header = sr.ReadUInt32 ();
			//			type: 0xfffd0002 Image type is 0xfffd0002
			uint type = sr.ReadUInt32 ();
			//			subtype: CARD32 Image subtype is the nominal size
			uint subtype = sr.ReadUInt32 ();
			//			version: 1
			uint version = sr.ReadUInt32 ();
			//			width: CARD32 Must be less than or equal to 0x7fff
			tmp.Width = sr.ReadUInt32 ();
			//			height: CARD32 Must be less than or equal to 0x7fff
			tmp.Height = sr.ReadUInt32 ();
			//			xhot: CARD32 Must be less than or equal to width
			tmp.Xhot = sr.ReadUInt32 ();
			//			yhot: CARD32 Must be less than or equal to height
			tmp.Yhot = sr.ReadUInt32 ();
			//			delay: CARD32 Delay between animation frames in milliseconds
			tmp.Delay = sr.ReadUInt32 ();
			//			pixels: LISTofCARD32 Packed ARGB format pixels
			tmp.data = sr.ReadBytes ((int)(4 * tmp.Width * tmp.Height));
			return tmp;
		}
	}
	public class XCursor
	{
		public static readonly Dictionary<MouseCursor, XCursor> Cursors = new Dictionary<MouseCursor, XCursor>();
		public uint Width;
		public uint Height;
		public uint Xhot;
		public uint Yhot;
		public uint Delay;
		public byte[] data;

		public XCursor () {	}
		public static Glfw.CustomCursor Create (Interface iface, MouseCursor mc) {			
			const int minimumSize = 24;
			if (!Cursors.ContainsKey (mc))
				XCursor.Cursors[mc] = XCursorFile.Load (iface, $"#Crow.Cursors.{mc}").Cursors.First (cu => cu.Width >= minimumSize);				
			XCursor c = XCursor.Cursors[mc];
			return new Glfw.CustomCursor (c.Width, c.Height, c.data, c.Xhot, c.Yhot);			
		}
		//		public static implicit operator MouseCursor(XCursor xc)
		//		{
		//			return new MouseCursor((int)xc.Xhot, (int)xc.Yhot, (int)xc.Width, (int)xc.Height,xc.data);
		//		}
	}
}

