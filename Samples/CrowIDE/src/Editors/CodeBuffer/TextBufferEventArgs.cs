using System;

namespace Crow.Text
{
	public class TextBufferEventArgs : EventArgs {
		public int LineStart;
		public int LineCount;

		public TextBufferEventArgs(int lineNumber) {
			LineStart = lineNumber;
			LineCount = 1;
		}
		public TextBufferEventArgs(int lineStart, int lineCount) {
			LineStart = lineStart;
			LineCount = lineCount;
		}
	}

}

