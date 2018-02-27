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
		enum States { init, classNames, members, value, endOfStatement }

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
					if (curState != States.init || curState != States.classNames )
						throw new ParsingException (this, "Unexpected char ','");
					readAndResetCurrentTok (TokenType.UnaryOp, true);
					curState = States.classNames;
					break;
				case '{':
					if (!(curState == States.init || curState == States.classNames))
						throw new ParsingException (this, "Unexpected char '{'");
					readAndResetCurrentTok (TokenType.OpenBlock, true);
					curState = States.members;
					break;
				case '}':
					if (curState != States.members)
						throw new ParsingException (this, "Unexpected char '}'");
					readAndResetCurrentTok (TokenType.CloseBlock, true);
					curState = States.classNames;
					break;
				case '=':
					if (curState == States.classNames)
						throw new ParsingException (this, "Unexpected char '='");
					readAndResetCurrentTok (TokenType.Affectation, true);
					curState = States.value;
					break;
				case '"':
					if (curState != States.value)
						throw new ParsingException (this, "Unexpected char '\"'");					
					readAndResetCurrentTok (TokenType.StringLitteralOpening, true);

					while (!eol) {
						currentTok += ReadLineUntil ("\"");
						if (currentTok.Content [currentTok.Content.Length - 1] == '\\')
							readToCurrTok ();
						else
							break;
					}
					if (eol)
						throw new ParsingException (this, "Unexpected end of line");
					saveAndResetCurrentTok (TokenType.StringLitteral);

					readAndResetCurrentTok (TokenType.StringLitteralClosing, true);
					curState = States.endOfStatement;
					break;
				case ';':
					if (curState != States.endOfStatement)
						throw new ParsingException (this, "Unexpected end of statement");					
					readAndResetCurrentTok (TokenType.StatementEnding, true);
					curState = States.members;
					break;
				default:
					if (currentTok.Type != TokenType.Unknown)
						throw new ParsingException (this, "error curtok not null");
					if (curState == States.value)
						throw new ParsingException (this, "expecting value enclosed in '\"'");
					if (curState == States.endOfStatement)
						throw new ParsingException (this, "expecting end of statement");					
					
					if (nextCharIsValidCharStartName) {						
						readToCurrTok (true);
						while (nextCharIsValidCharName)
							readToCurrTok ();
					}
					saveAndResetCurrentTok (TokenType.Identifier);
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

