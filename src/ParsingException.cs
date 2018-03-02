using System;

namespace Crow.Coding
{
	public class ParserException : Exception
	{
		public int Line;
		public int Column;
		public ParserException(int line, int column, string txt, string source = null)
			: base(string.Format("{3}:({0},{1}): {2}", line, column, txt, source))
		{
			Line = line;
			Column = column;
		}
		public ParserException(int line, int column, string txt, Exception innerException, string source = null)
			: base(string.Format("{3}:({0},{1}): {2}", line, column, txt, source), innerException)
		{}
	}
}

