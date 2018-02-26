using System;
using Crow;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;

namespace Crow.Coding
{
	public class StyleParser : Parser
	{
		enum States { init, classNames, members }

		public StyleParser (CodeBuffer _buffer) : base(_buffer)
		{
		}

		#region Regular Expression for validity checks
		private static Regex rxValidChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		private static Regex rxNameStartChar = new Regex(@"_|\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}");															
		private static Regex rxNameChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		private static Regex rxDecimal = new Regex(@"[0-9]+");
		private static Regex rxHexadecimal = new Regex(@"[0-9a-fA-F]+");
		#endregion

		#region Character ValidityCheck
		public bool nextCharIsValidCharStartName
		{
			get { return rxNameStartChar.IsMatch(new string(new char[]{Peek()})); }
		}
		public bool nextCharIsValidCharName
		{
			get { return rxNameChar.IsMatch(new string(new char[]{Peek()})); }
		}
		#endregion

		States curState = States.classNames;

		public override void ParseCurrentLine ()
		{
			Debug.WriteLine (string.Format("parsing line:{0}", currentLine));
			CodeLine cl = buffer [currentLine];
			cl.Tokens = new List<Token> ();
			WpToken = null;

			//retrieve current parser state from previous line
			if (currentLine > 0)
				curState = (States)buffer[currentLine - 1].EndingState;
			else
				curState = States.init;

			States previousEndingState = (States)cl.EndingState;

			while (! eol) {
				SkipWhiteSpaces ();

				if (eol)
					break;

				if (Peek () == '\n') {
					if (currentTok != TokenType.Unknown)
						throw new ParsingException (this, "Unexpected end of line");
					Read ();
					eol = true;
					continue;
				}

				switch (Peek()) {
				case '/':
					readToCurrTok (true);
					switch (Peek ()) {
					case '/':
						currentTok += ReadLine ();
						saveAndResetCurrentTok (TokenType.LineComment);
						break;
					default:
						currentTok += ReadLine ();
						saveAndResetCurrentTok (TokenType.Unknown);
						break;
					}
					break;
				case ',':
					if (currentTok.Type != TokenType.Identifier || curState == States.members )					
						throw new ParsingException (this, "Unexpected char ','");
					saveAndResetAfterWhiteSpaceSkipping (TokenType.Type);//save previous token as class
					readToCurrTok (true);
					saveAndResetCurrentTok (TokenType.UnaryOp);
					curState = States.classNames;
					break;
				case '{':
					if (currentTok.Type != TokenType.Identifier || curState == States.members)
						throw new ParsingException (this, "Unexpected char '}'");

					saveAndResetAfterWhiteSpaceSkipping (TokenType.Type);//save previous token as class

					readToCurrTok (true);
					saveAndResetCurrentTok (TokenType.OpenBlock);
					curState = States.members;
					break;
				case '}':
					if (curState != States.members)
						throw new ParsingException (this, "Unexpected char '}'");
					readToCurrTok (true);
					saveAndResetCurrentTok (TokenType.CloseBlock);
					curState = States.classNames;
					break;
				case '=':
					if (currentTok.Type != TokenType.Identifier)
						throw new ParsingException (this, "Unexpected char '='");

					saveAndResetAfterWhiteSpaceSkipping ();//save previous token as propertyname

					curState = States.members;

					readToCurrTok (true);
					saveAndResetCurrentTok (TokenType.Affectation);

					SkipWhiteSpaces ();

					currentTok+=ReadLineUntil(";");
					saveAndResetCurrentTok (TokenType.StringLitteral);

					if (Peek() != ';')
						throw new ParsingException (this, "Expecting ';'");
					readToCurrTok (true);
					saveAndResetCurrentTok (TokenType.StatementEnding);
					break;
				default:
					if (currentTok.Type != TokenType.Unknown)
						throw new ParsingException (this, "error");
					
					if (nextCharIsValidCharStartName) {						
						readToCurrTok (true);
						while (nextCharIsValidCharName)
							readToCurrTok ();
					}
					currentTok.Type = TokenType.Identifier;
					currentTok.End = CurrentPosition;
					break;
				}
			}

			if (cl.EndingState != (int)curState && currentLine < buffer.LineCount - 1)
				buffer [currentLine + 1].Tokens = null;

			cl.EndingState = (int)curState;
		}
		public override void SyntaxAnalysis ()
		{
			RootNode = new Node () { Name = "RootNode", Type="Root" };
		}
	}
}

