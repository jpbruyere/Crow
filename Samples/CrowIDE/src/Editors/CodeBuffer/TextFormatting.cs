using System;

namespace Crow.Coding
{
	public struct TextFormatting {
		public Color Foreground;
		public Color Background;
		public bool Bold;
		public bool Italic;

		public TextFormatting(Color fg, Color bg, bool bold = false, bool italic = false){
			Foreground = fg;
			Background = bg;
			Bold = bold;
			Italic = italic;
		}
	}
}

