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
				SkipWhiteSpaces ();

				if (eol)
					break;

				if (Peek () == '\n') {
					if (currentTok != TokenType.Unknown)
						throw new ParserException (currentLine, currentColumn, "Unexpected end of line");
					Read ();
					eol = true;
					continue;
				}

				if (curState == States.BlockComment) {
					if (currentTok != TokenType.Unknown)
						Debugger.Break ();

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

		Node addChildNode (Node curNode, CodeLine cl, int tokPtr) {
			Node n = new Node () { Name = cl.Tokens [tokPtr].Content, StartLine = cl };
			curNode.AddChild (n);
			if (cl.SyntacticNode == null)
				cl.SyntacticNode = n;
			return n;
		}
		void closeNodeAndGoUp (ref Node n, CodeLine cl){
			if (n.StartLine == cl){//prevent single line node
				n.Parent.Children.Remove (n);
				if (cl.SyntacticNode == n)
					cl.SyntacticNode = null;
			}else				
				n.EndLine = cl;
			n = n.Parent;
		}

		public override void SyntaxAnalysis ()
		{
			RootNode = new Node () { Name = "RootNode", Type="Root" };

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
							currentNode = addChildNode (currentNode, cl, tokPtr);
							closeNodeAndGoUp (ref currentNode, buffer [ptrLine]);
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
							currentNode = addChildNode (currentNode, cl, tokPtr);
						}else if (cl.Tokens [tokPtr].Content.StartsWith("#endregion"))
							closeNodeAndGoUp (ref currentNode, cl);
						break;
					}
					onlyWhiteSpace = false;
					tokPtr++;
				}
				ptrLine++;
			}
		}
	}
}

