//
// XCursor.cs
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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace Crow
{
	public class XCursorFile
	{
		const uint XC_TYPE_IMG = 0xfffd0002;

		class toc
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
			List<toc> tocList = new List<toc> ();
			XCursorFile tmp = new XCursorFile ();

			using (BinaryReader sr = new BinaryReader (s)) {
				byte[] data;
				//magic: CARD32 ’Xcur’ (0x58, 0x63, 0x75, 0x72)
				if (new string (sr.ReadChars (4)) != "Xcur") {
					Console.WriteLine ("XCursor Load error: Wrong magic");
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

		public static XCursorFile Load(string path)
		{
			return loadFromStream (Interface.GetStreamFromPath (path));
		}

		static XCursor imageLoad(BinaryReader sr)
		{
			XCursor tmp = new XCursor();
			//			header: 36 Image headers are 36 bytes
			uint header = sr.ReadUInt32();
			//			type: 0xfffd0002 Image type is 0xfffd0002
			uint type = sr.ReadUInt32();
			//			subtype: CARD32 Image subtype is the nominal size
			uint subtype = sr.ReadUInt32();
			//			version: 1
			uint version = sr.ReadUInt32();
			//			width: CARD32 Must be less than or equal to 0x7fff
			tmp.Width = sr.ReadUInt32();
			//			height: CARD32 Must be less than or equal to 0x7fff
			tmp.Height = sr.ReadUInt32();
			//			xhot: CARD32 Must be less than or equal to width
			tmp.Xhot = sr.ReadUInt32();
			//			yhot: CARD32 Must be less than or equal to height
			tmp.Yhot = sr.ReadUInt32();
			//			delay: CARD32 Delay between animation frames in milliseconds
			tmp.Delay = sr.ReadUInt32();
			//			pixels: LISTofCARD32 Packed ARGB format pixels
			tmp.data = sr.ReadBytes((int)(tmp.Width * tmp.Height * 4));
			return tmp;
		}
	}
	public class XCursor
	{
		public static XCursor Default;
		public static XCursor Cross;
		public static XCursor Arrow;
		public static XCursor Text;
		public static XCursor SW;
		public static XCursor SE;
		public static XCursor NW;
		public static XCursor NE;
		public static XCursor N;
		public static XCursor S;
		public static XCursor V;
		public static XCursor H;

		public uint Width;
		public uint Height;
		public uint Xhot;
		public uint Yhot;
		public uint Delay;
		public byte[] data;

		public XCursor ()
		{
		}
//		public static implicit operator MouseCursor(XCursor xc)
//		{
//			return new MouseCursor((int)xc.Xhot, (int)xc.Yhot, (int)xc.Width, (int)xc.Height,xc.data);
//		}
	}
}

