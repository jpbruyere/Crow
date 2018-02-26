using System;
using Crow;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

namespace Crow.Coding
{
	public class XMLParser : Parser
	{
		public new enum TokenType {
			Unknown = Parser.TokenType.Unknown,
			WhiteSpace = Parser.TokenType.WhiteSpace,
			NewLine = Parser.TokenType.NewLine,
			LineComment = Parser.TokenType.LineComment,
			BlockCommentStart = Parser.TokenType.BlockCommentStart,
			BlockComment = Parser.TokenType.BlockComment,
			BlockCommentEnd = Parser.TokenType.BlockCommentEnd,
			ElementName = Parser.TokenType.Type,
			AttributeName = Parser.TokenType.Identifier,
			ElementClosing = Parser.TokenType.StatementEnding,
			Affectation = Parser.TokenType.Affectation,
			AttributeValueOpening = Parser.TokenType.StringLitteralOpening,
			AttributeValueClosing = Parser.TokenType.StringLitteralClosing,
			AttributeValue = Parser.TokenType.StringLitteral,
			XMLDecl = Parser.TokenType.Preprocessor,
			ElementStart = 50,
			ElementEnd = 51,
		}

		public enum States
		{
			init,       //first statement of prolog, xmldecl should only apear in this state
			prolog,     //misc before doctypedecl
			InternalSubset,    //doctype declaration subset
			ExternalSubsetInit,
			ExternalSubset,
			BlockComment,
			DTDEnd,//doctype finished
			XML,//normal xml
			StartTag,//inside start tag
			Content,//after start tag with no closing slash
			EndTag
		}

		#region CTOR
		public XMLParser (CodeBuffer _buffer) : base(_buffer) {}
		#endregion

		enum Keywords
		{
			DOCTYPE,
			ELEMENT,
			ATTLIST,
			ENTITY,
			NOTATION
		}

		States curState = States.init;

		#region Regular Expression for validity checks
		//private static Regex rxValidChar = new Regex("[\u0020-\uD7FF]");
		private static Regex rxValidChar = new Regex(@"\u0009|\u000A|\u000D|[\u0020-\uD7FF]|[\uE000-\uFFFD]");   //| [\u10000-\u10FFFF] unable to set those plans
		private static Regex rxNameStartChar = new Regex(@":|[A-Z]|_|[a-z]|[\u00C0-\u00D6]|[\u00D8-\u00F6]|[\u00F8-\u02FF]|[\u0370-\u037D]|[\u037F-\u1FFF]|[\u200C-\u200D]|[\u2070-\u218F]|[\u2C00-\u2FEF]|[\u3001-\uD7FF]|[\uF900-\uFDCF]|[\uFDF0-\uFFFD]"); // | [\u10000-\uEFFFF]
		private static Regex rxNameChar = new Regex(@":|[A-Z]|_|[a-z]|[\u00C0-\u00D6]|[\u00D8-\u00F6]|[\u00F8-\u02FF]|[\u0370-\u037D]|[\u037F-\u1FFF]|[\u200C-\u200D]|[\u2070-\u218F]|[\u2C00-\u2FEF]|[\u3001-\uD7FF]|[\uF900-\uFDCF]|[\uFDF0-\uFFFD]|-|\.|[0-9]|\u00B7|[\u0300-\u036F]|[\u203F-\u2040]");//[\u10000-\uEFFFF]|
		private static Regex rxDecimal = new Regex(@"[0-9]+");
		private static Regex rxHexadecimal = new Regex(@"[0-9a-fA-F]+");
		private static Regex rxAttributeValue = new Regex(@"[^<]");
		private static Regex rxEntityValue = new Regex(@"[^<]");
		private static Regex rxPubidChar = new Regex(@"\u0020|\u000D|\u000A|[a-zA-Z0-9]|[-\(\)\+\,\./:=\?;!\*#@\$_%]");
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

		public override void SetLineInError (ParsingException ex)
		{
			base.SetLineInError (ex);
			//buffer[ex.Line].Tokens.EndingState = (int)States.init;
		}

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
					currentTok += ReadLineUntil ("-->");
					if (Peek (3) == "-->") {
						readToCurrTok (3);
						curState = States.XML;
					}
					saveAndResetCurrentTok ();
					continue;
				}

				switch (Peek()) {
				case '<':
					readToCurrTok (true);
					switch (Peek()) {
					case '?':
						if (curState != States.init)
							throw new ParsingException (this, "xml decl may appear only on first line");
						readToCurrTok ();
						currentTok += ReadLineUntil ("?>");
						if (Peek (2) != "?>")
							throw new ParsingException (this, "expecting '?>'");
						readToCurrTok (2);
						saveAndResetCurrentTok (TokenType.XMLDecl);
						curState = States.prolog;
						break;
					case '!':
						readToCurrTok ();
						switch (Peek()) {
						case '-':
							readToCurrTok ();
							if (Peek () != '-')
								throw new ParsingException (this, "Expecting comment start tag");
							readToCurrTok ();
							currentTok += ReadLineUntil ("--");
							if (Peek (3) == "-->") {
								readToCurrTok (3);
							}else
								curState = States.BlockComment;
							saveAndResetCurrentTok (TokenType.BlockComment);
							break;
						default:
							throw new ParsingException(this, "error");
						}
						break;
					default:
						if (!(curState == States.Content || curState == States.XML || curState == States.init || curState == States.prolog))
							throw new ParsingException (this, "Unexpected char: '<'");
						if (Peek () == '/') {
							curState = States.EndTag;
							readToCurrTok ();
							saveAndResetCurrentTok (TokenType.ElementEnd);
						} else {
							curState = States.StartTag;
							saveAndResetCurrentTok (TokenType.ElementStart);
						}

						if (!nextCharIsValidCharStartName)
							throw new ParsingException (this, "Expected element name");

						readToCurrTok (true);
						while (nextCharIsValidCharName)
							readToCurrTok ();

						saveAndResetCurrentTok (TokenType.ElementName);
						break;
					}
					break;
				case '/':
					if (curState != States.StartTag)
						throw new ParsingException (this, "Unexpected char: '/'");
					readToCurrTok (true);
					if (Peek () != '>')
						throw new ParsingException (this, "Expecting '>'");
					readAndResetCurrentTok (TokenType.ElementEnd);

					curState = States.XML;
					break;
				case '>':
					readAndResetCurrentTok (TokenType.ElementClosing, true);
					switch (curState) {
					case States.EndTag:
						curState = States.XML;
						break;
					case States.StartTag:
						curState = States.Content;
						break;
					default:
						throw new ParsingException (this, "Unexpected char: '>'");
					}
					break;
				default:
					switch (curState) {
					case States.StartTag:
						if (!nextCharIsValidCharStartName)
							throw new ParsingException (this, "Expected attribute name");
						readToCurrTok (true);
						while (nextCharIsValidCharName)
							readToCurrTok ();
						saveAndResetCurrentTok (TokenType.AttributeName);

						SkipWhiteSpaces ();

						if (Peek () != '=')
							throw new ParsingException (this, "Expecting: '='");
						readAndResetCurrentTok (TokenType.Affectation, true);

						SkipWhiteSpaces ();

						char openAttVal = Peek ();
						if (openAttVal != '"' && openAttVal != '\'')
							throw new ParsingException (this, "Expecting attribute value enclosed either in '\"' or in \"'\"");
						readAndResetCurrentTok (TokenType.AttributeValueOpening, true);

						currentTok.Start = CurrentPosition;
						currentTok.Content = ReadLineUntil (new string (new char[]{ openAttVal }));
						saveAndResetCurrentTok (TokenType.AttributeValue);

						if (Peek () != openAttVal)
							throw new ParsingException (this, string.Format ("Expecting {0}", openAttVal));
						readAndResetCurrentTok (TokenType.AttributeValueClosing, true);
						break;
					default:
						throw new ParsingException (this, "unexpected char: " + Peek ());
					}
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

			Node currentNode = RootNode;

			for (int i = 0; i < buffer.LineCount; i++) {
				CodeLine cl = buffer[i];
				if (cl.Tokens == null)
					continue;
				cl.SyntacticNode = null;

				int tokPtr = 0;
				while (tokPtr < cl.Tokens.Count) {
					switch ((XMLParser.TokenType)cl.Tokens [tokPtr].Type) {
					case TokenType.ElementStart:
						tokPtr++;
						Node newElt = new Node () { Name = cl.Tokens [tokPtr].Content, StartLine = cl };
						currentNode.AddChild (newElt);
						currentNode = newElt;
						if (cl.SyntacticNode == null)
							cl.SyntacticNode = newElt;
						break;
					case TokenType.ElementEnd:
						tokPtr++;
						if (tokPtr < cl.Tokens.Count) {
							if ((XMLParser.TokenType)cl.Tokens [tokPtr].Type == TokenType.ElementName &&
								cl.Tokens [tokPtr].Content != currentNode.Name)
								throw new ParsingException (this, "Closing tag mismatch");
						}
						currentNode.EndLine = cl;
						currentNode = currentNode.Parent;
						break;
					case TokenType.ElementClosing:
						//currentNode = currentNode.Parent;
						break;
					default:
						break;
					}
					tokPtr++;
				}
			}
		}
	}
}

