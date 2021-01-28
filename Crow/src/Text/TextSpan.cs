using System;
using System.Collections.Generic;
using System.Text;

namespace Crow.Text
{
	public struct TextSpan
	{
		public readonly int Start;
		public readonly int End;
		public TextSpan (int start, int end) {
			Start = start;
			End = end;
		}

		public bool IsEmpty => Start == End;
		public int Length => End - Start;
	}
}
