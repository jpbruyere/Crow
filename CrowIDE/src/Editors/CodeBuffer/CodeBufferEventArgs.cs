using System;

namespace Crow.Coding
{
	public class CodeBufferEventArgs : EventArgs {
		public int LineStart;
		public int LineCount;

		public CodeBufferEventArgs(int lineNumber) {
			LineStart = lineNumber;
			LineCount = 1;
		}
		public CodeBufferEventArgs(int lineStart, int lineCount) {
			LineStart = lineStart;
			LineCount = lineCount;
		}
	}

}

