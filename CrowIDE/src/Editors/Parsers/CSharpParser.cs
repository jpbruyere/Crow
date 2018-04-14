using System;
using Crow;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;

namespace Crow.Coding
{
	public class CSharpParser : BufferParser
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
		static Regex rxValidChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		static Regex rxNameStartChar = new Regex(@"_|\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}");
		static Regex rxNameChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		static Regex rxNewLineChar = new Regex(@"\u000D|\u000A|\u0085|\u2028|\u2029");
		static Regex rxWhiteSpaceChar = new Regex(@"\p{Zs}|\u0009|\u000B|\u000C");
		static Regex rxDecimal = new Regex(@"[0-9]+");
		static Regex rxHexadecimal = new Regex(@"[0-9a-fA-F]+");

		public static bool CharIsValidCharStartName (char c) {
			return rxNameStartChar.IsMatch(new string(new char[]{c}));
		}
		public static bool CharIsValidCharName (char c) {
			return rxNameChar.IsMatch(new string(new char[]{c}));
		}

		public bool nextCharIsValidCharStartName
		{
			get { return CharIsValidCharStartName(Peek()); }
		}
		public bool nextCharIsValidCharName
		{
			get { return CharIsValidCharName(Peek()); }
		}
		#endregion

		States curState = States.init;
		States savedState = States.init;

		public override void ParseCurrentLine ()
		{
			//Debug.WriteLine (string.Format("parsing line:{0}", currentLine));
			CodeLine cl = buffer [currentLine];
			cl.Tokens = new List<Token> ();


			//retrieve current parser state from previous line
			if (currentLine > 0)
				curState = (States)buffer[currentLine - 1].EndingState;
			else
				curState = States.init;

			States previousEndingState = (States)cl.EndingState;

			while (! eol) {
				if (currentTok.IsNull)
					SkipWhiteSpaces ();

				if (curState == States.BlockComment) {
					currentTok.Start = CurrentPosition;
					currentTok.Type = (BufferParser.TokenType)TokenType.BlockComment;
					currentTok += ReadLineUntil ("*/");
					if (Peek (2) == "*/") {
						readToCurrTok (2);
						curState = savedState;
					}
					saveAndResetCurrentTok ();
					continue;
				}

				switch (Peek()) {
				case '\n':
					eol = true;
					if (!currentTok.IsNull)
						saveAndResetCurrentTok ();
					break;
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
				case '{':
					if (currentTok.IsNull)
						readAndResetCurrentTok (TokenType.OpenBlock, true);
					else
						readToCurrTok ();
					break;
				case '}':
					if (currentTok.IsNull)
						readAndResetCurrentTok (TokenType.CloseBlock, true);
					else
						readToCurrTok ();					
					break;
				case '\\'://unicode escape sequence
					if (!(currentTok.Type == TokenType.Identifier ||
					    currentTok.IsEmpty || currentTok.Type == TokenType.StringLitteral || currentTok.Type == TokenType.CharLitteral)) {
						saveAndResetCurrentTok ();
					}
					Point pos = CurrentPosition;
					Read ();
					char escChar = Read ();

					if (escChar == 'u') {
						char c = char.ConvertFromUtf32 (int.Parse (Read (4), System.Globalization.NumberStyles.HexNumber))[0];
						if (currentTok.IsEmpty) {
							if (!CharIsValidCharStartName (c))
								throwParserException ("expecting identifier start");							
							currentTok.Start = pos;
							currentTok.Type = TokenType.Identifier;
						} else if (currentTok.Type == TokenType.Identifier) {
							if (!CharIsValidCharName (c))
								throwParserException ("expecting identifier valid char");						
						}
						currentTok += c;
						break;
					}
					currentTok += new String (new char[] { '\\', escChar });
					break;
				case '\'':
					if (currentTok.IsNull) {
						readAndResetCurrentTok (TokenType.CharLitteralOpening, true);
						currentTok.Type = TokenType.CharLitteral;
					} else if (currentTok.Type == TokenType.CharLitteral) {
						saveAndResetCurrentTok ();
						readAndResetCurrentTok (TokenType.CharLitteralClosing, true);
					} else if (currentTok.Type == TokenType.StringLitteral){
						readToCurrTok ();
					} else
						throwParserException ("unexpected character: (\')");						
					break;
				case '"':
					if (currentTok.IsNull) {
						readAndResetCurrentTok (TokenType.StringLitteralOpening, true);
						currentTok.Type = TokenType.StringLitteral;
					} else if (currentTok.Type == TokenType.StringLitteral) {
						saveAndResetCurrentTok ();
						readAndResetCurrentTok (TokenType.StringLitteralClosing, true);
					} else
						throwParserException ("unexpected character: (\")");
					break;
				default:
					if (currentTok.Type == TokenType.StringLitteral || currentTok.Type == TokenType.CharLitteral) {
						readToCurrTok (currentTok.IsEmpty);
					} else if (currentTok.IsNull) {
						if (nextCharIsValidCharStartName) {						
							readToCurrTok (true);
							while (nextCharIsValidCharName)
								readToCurrTok ();

							if (keywords.Contains (currentTok.Content))
								saveAndResetCurrentTok (TokenType.Keyword);
							else
								saveAndResetCurrentTok (TokenType.Identifier);
							continue;
						} else
							readAndResetCurrentTok(TokenType.Unknown, true);
					} else
						readAndResetCurrentTok(TokenType.Unknown, true);					
					break;
				}
			}

			if (cl.EndingState != (int)curState && currentLine < buffer.LineCount - 1)
				buffer [currentLine + 1].Tokens = null;

			cl.EndingState = (int)curState;
		}
		
		public override void SyntaxAnalysis ()
		{
			initSyntaxAnalysis ();
			Node currentNode = RootNode;

			int ptrLine = 0;
			while (ptrLine < buffer.LineCount) {
				CodeLine cl = buffer [ptrLine];
				if (cl.Tokens == null){
					ptrLine++;
					continue;
				}
				cl.SyntacticNode = null;

				int tokPtr = 0;
				bool onlyWhiteSpace = true;
				while (tokPtr < cl.Tokens.Count) {
					if (cl.Tokens [tokPtr].Type == TokenType.WhiteSpace) {
						tokPtr++;
						continue;
					}

					if (cl.Tokens [tokPtr].Type == TokenType.LineComment && onlyWhiteSpace) {
						int startLine = ptrLine;
						ptrLine++;
						while (ptrLine < buffer.LineCount) {
							int idx = buffer [ptrLine].FirstNonBlankTokIndex;
							if (idx < 0) 
								break;
							if (buffer [ptrLine].Tokens [idx].Type != TokenType.LineComment)
								break;
							ptrLine++;
						}
						ptrLine--;
						if (ptrLine - startLine > 0) {
							currentNode = addChildNode (currentNode, cl, tokPtr, "comment");
							closeNodeAndGoUp (ref currentNode, buffer [ptrLine], "comment");
						}
						break;
					}

					switch (cl.Tokens [tokPtr].Type) {
					case TokenType.OpenBlock:
						currentNode = addChildNode (currentNode, cl, tokPtr);
						break;
					case TokenType.CloseBlock:						
						closeNodeAndGoUp (ref currentNode, cl);
						break;
					case TokenType.Preprocessor:
						if (cl.Tokens [tokPtr].Content.StartsWith ("#region")) {
							currentNode = addChildNode (currentNode, cl, tokPtr, "region");
						} else if (cl.Tokens [tokPtr].Content.StartsWith ("#endregion")) {
							
							closeNodeAndGoUp (ref currentNode, cl,"region");
						}
						break;
					}
					onlyWhiteSpace = false;
					tokPtr++;
				}
				ptrLine++;
			}
			ptrLine = 0;
			while (ptrLine < buffer.LineCount) {
				CodeLine cl = buffer [ptrLine];
				if (cl.IsFoldable) {
					if (cl.SyntacticNode.Type == "comment" || cl.SyntacticNode.Type == "region")
						cl.IsFolded = true;
				}
				ptrLine++;
			}
		}
	}
}

