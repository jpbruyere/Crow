// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;

namespace Crow
{
	public struct FileLocation {
		public string FilePath;
		public int Line;
		public int Column;
		public int Length;

		public FileLocation(string filePath, int line, int column, int length = 0){
			FilePath = filePath;
			Line = line;
			Column = column;
			Length = length;
		}
		public override string ToString ()
		{
			return string.Format ("{0} ({1},{2})", FilePath, Line, Column);
		}
	}
	public class Style : Dictionary<string, object>
	{
		#if DESIGN_MODE
		public Dictionary<string, FileLocation> Locations = new Dictionary<string, FileLocation>();
		#endif
		//public Dictionary<string, Style> SubStyles;//TODO:implement substyles for all tags inside a style
		public Style () : base()
		{
		}

	}
}

