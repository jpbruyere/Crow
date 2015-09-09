//
//  XCursor.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.IO;
using System.Diagnostics;
using OpenTK;
using System.Collections.Generic;

namespace go
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

		static XCursorFile loadFromRessource (string resId)
		{
			Stream stream = null;

			//first, search for ressource in main executable assembly
			stream = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(resId);
			if (stream == null)//try to find ressource in golib assembly				
				stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resId);
			if (stream == null) {
				Debug.WriteLine ("Ressource not found: " + resId);
				return null;
			}

			using (MemoryStream ms = new MemoryStream ()) {
				stream.CopyTo (ms);
				ms.Seek (0, SeekOrigin.Begin);
				return loadFromStream (ms);
			}			
		}
		static XCursorFile loadFromFile (string path)
		{			
			using (Stream s = new FileStream (path, 
				FileMode.Open, FileAccess.Read)) {
				return loadFromStream (s);
			}		
		}
		static XCursorFile loadFromStream(Stream s)
		{
			List<toc> tocList = new List<toc> ();
			XCursorFile tmp = new XCursorFile ();

			using (BinaryReader sr = new BinaryReader (s)) {
				byte[] data;
				//magic: CARD32 ’Xcur’ (0x58, 0x63, 0x75, 0x72)
				if (new string (sr.ReadChars (4)) != "Xcur") {
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

		public static XCursorFile Load(string path)
		{
			if (path.StartsWith ("#"))
				return loadFromRessource (path.Substring(1));
			else
				return loadFromFile (path);									
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
		public static implicit operator MouseCursor(XCursor xc)
		{
			return new MouseCursor((int)xc.Xhot, (int)xc.Yhot, (int)xc.Width, (int)xc.Height,xc.data);
		}
	}
}

