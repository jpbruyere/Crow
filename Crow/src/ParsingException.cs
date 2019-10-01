using System;

namespace Crow.Coding
{
	public class ParserException : Exception
	{
		public int Line;
		public int Column;
		public ParserException(int line, int column, string txt, string source = null)
			: base(txt)
		{
			Line = line;
			Column = column;
			Source = source;
		}
		public ParserException(int line, int column, string txt, Exception innerException, string source = null)
			: base(txt, innerException)
		{
			Line = line;
			Column = column;
			Source = source;
		}
		public override string ToString ()
		{
			return string.Format("{3}:({0},{1}): {2}", Line, Column, Message, Source);
		}
	}
}

