using System;

namespace Crow.Coding
{
	public class ParserException : Exception
	{
		public int Line;
		public int Column;
		public ParserException(int line, int column, string txt)
			: base(string.Format("Parser exception ({0},{1}): {2}", line, column, txt))
		{
			Line = line;
			Column = column;
		}
		public ParserException(int line, int column, string txt, Exception innerException)
			: base(string.Format("Parser exception ({0},{1}): {2}", line, column, txt), innerException)
		{}
	}
}

