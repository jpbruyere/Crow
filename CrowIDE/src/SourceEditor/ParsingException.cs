using System;

namespace Crow.Coding
{
	public class ParsingException : Exception
	{
		public int Line;
		public int Column;
		public ParsingException(Parser parser, string txt)
			: base(string.Format("Parser exception ({0},{1}): {2}", parser.currentLine, parser.currentColumn, txt))
		{
			Line = parser.currentLine;
			Column = parser.currentColumn;
		}
		public ParsingException(Parser parser, string txt, Exception innerException)
			: base(txt, innerException)
		{
			txt = string.Format("Parser exception ({0},{1}): {2}", parser.currentLine, parser.currentColumn, txt);
		}
	}
}

