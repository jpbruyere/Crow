using System;
using Crow;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;

namespace Crow.Coding
{
	public class CSharpParser : Parser
	{
		#region keywords
		string[] keywords = new string[] {
			"abstract",
			"as",
			"ascending",
			"async",
			"await",
			"base",
			"bool",
			"break",
			"byte",
			"case",
			"catch",
			"char",
			"checked",
			"class",
			"const",
			"continue",
			"decimal",
			"default",
			"delegate",
			"descending",
			"do",
			"double",
			"dynamic",
			"else",
			"enum",
			"equals",
			"event",
			"explicit",
			"extern",
			"false",
			"finally",
			"fixed",
			"float",
			"for",
			"foreach",
			"from",
			"get",
			"goto",
			"group",
			"if",
			"implicit",
			"in",
			"int",
			"interface",
			"internal",
			"is",
			"join",
			"let",
			"lock",
			"long",
			"nameof",
			"namespace",
			"new",
			"null",
			"object",
			"operator",
			"orderby",
			"out",
			"override",
			"params",
			"partial",
			"private",
			"protected",
			"public",
			"readonly",
			"ref",
			"return",
			"sbyte",
			"sealed",
			"select",
			"set",
			"short",
			"sizeof",
			"stackalloc",
			"static",
			"string",
			"struct",
			"switch",
			"this",
			"throw",
			"true",
			"try",
			"typeof",
			"uint",
			"ulong",
			"unchecked",
			"unsafe",
			"ushort",
			"using",
			"value",
			"var",
			"virtual",
			"void",
			"volatile",
			"when",
			"where",
			"while",
			"yield "			
		};
		#endregion

		public enum States
		{
			init,       
			BlockComment,
			InNameSpace,
			InClass,
			InMember,
			Unknown,
		}

		public CSharpParser (CodeBuffer _buffer) : base(_buffer)
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

		States curState = States.init;
		States savedState = States.init;

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

				if (curState == States.BlockComment) {
					if (currentTok != TokenType.Unknown)
						Debugger.Break ();

					currentTok.Start = CurrentPosition;
					currentTok.Type = (Parser.TokenType)TokenType.BlockComment;
					currentTok += ReadLineUntil ("*/");
					if (Peek (2) == "*/") {
						readToCurrTok (2);
						curState = savedState;
					}
					saveAndResetCurrentTok ();
					continue;
				}

				switch (Peek()) {
				case '#':
					readToCurrTok (true);
					currentTok += ReadLine ();
					saveAndResetCurrentTok (TokenType.Preprocessor);
					break;
				case '/':
					readToCurrTok (true);
					switch (Peek ()) {
					case '*':
						readToCurrTok ();
						currentTok += ReadLine ();
						//currentTok.Type = (Parser.TokenType)TokenType.BlockComment;
						savedState = curState;
						curState = States.BlockComment;
						saveAndResetCurrentTok (TokenType.BlockComment);
						break;
					case '/':
						//readToCurrTok ();
						currentTok += ReadLine ();
						saveAndResetCurrentTok (TokenType.LineComment);
						//currentTok.Type = (Parser.TokenType)TokenType.LineComment;
						break;
					default:
						currentTok += ReadLine ();
						saveAndResetCurrentTok (TokenType.Unknown);
						break;
					}
					break;
				default:					
					if (nextCharIsValidCharStartName) {						
						readToCurrTok (true);
						while (nextCharIsValidCharName)
							readToCurrTok ();

						if (keywords.Contains (currentTok.Content))
							saveAndResetCurrentTok (TokenType.Keyword);
						else
							saveAndResetCurrentTok (TokenType.Identifier);
						continue;
					}
					readToCurrTok (true);
					currentTok+=ReadLine ();
					saveAndResetCurrentTok (TokenType.Unknown);
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

