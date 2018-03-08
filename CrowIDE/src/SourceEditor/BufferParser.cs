using System;
using System.IO;
using Crow;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Crow.Coding
{
	/// <summary>
	/// base class for tokenizing sources
	/// </summary>
	public abstract class BufferParser
	{
		/// <summary>
		/// Default tokens, this enum may be overriden in derived parser with the new keyword,
		/// see XMLParser for example.
		/// </summary>
		public enum TokenType {
			Unknown = 0,
			WhiteSpace = 1,
			NewLine = 2,
			LineComment = 3,
			BlockCommentStart = 4,
			BlockComment = 5,
			BlockCommentEnd = 6,
			Preprocessor = 7,
			Identifier = 8,
			Keyword = 9,
			OpenBlock = 10,
			CloseBlock = 11,
			StatementEnding = 12,
			OperatorOrPunctuation = 13,
			IntegerLitteral = 14,
			RealLitteral = 15,
			StringLitteralOpening = 16,
			StringLitteralClosing = 17,
			StringLitteral = 18,
			CharLitteralOpening = 19,
			CharLitteralClosing = 20,
			CharLitteral = 21,
			BoolLitteral = 22,
			NullLitteral = 23,			 
			Type = 24,
		}

		#region CTOR
		public BufferParser (CodeBuffer _buffer)
		{
			buffer = _buffer;

			buffer.LineUpadateEvent += Buffer_LineUpadateEvent;
			//buffer.LineAdditionEvent += Buffer_LineAdditionEvent;;
			buffer.LineRemoveEvent += Buffer_LineRemoveEvent;
			buffer.BufferCleared += Buffer_BufferCleared;
		}

		#endregion

		#region Buffer events handlers
		void Buffer_BufferCleared (object sender, EventArgs e)
		{

		}
		void Buffer_LineAdditionEvent (object sender, CodeBufferEventArgs e)
		{

		}
		void Buffer_LineRemoveEvent (object sender, CodeBufferEventArgs e)
		{
			reparseSource ();
		}
		void Buffer_LineUpadateEvent (object sender, CodeBufferEventArgs e)
		{
			for (int i = 0; i < e.LineCount; i++)
				TryParseBufferLine (e.LineStart + i);
			reparseSource ();
		}
		#endregion

		internal int currentLine = 0;
		internal int currentColumn = 0;

		int syntTreeDepth = 0;
		public int SyntacticTreeDepth {
			get { return syntTreeDepth;}
			set {
				syntTreeDepth = value;
				if (syntTreeDepth > SyntacticTreeMaxDepth)
					SyntacticTreeMaxDepth = syntTreeDepth;
			}
		}
		public int SyntacticTreeMaxDepth = 0;

		protected CodeBuffer buffer;
		protected Token currentTok;
		protected bool eol = true;
		protected Point CurrentPosition {
			get { return new Point (currentLine, currentColumn); }
			set {
				currentLine = value.Y;
				currentColumn = value.X;
			}
		}

		public Node RootNode;

		public abstract void ParseCurrentLine();
		public abstract void SyntaxAnalysis ();
		public void reparseSource () {
			for (int i = 0; i < buffer.LineCount; i++) {
				if (!buffer[i].IsParsed)
					TryParseBufferLine (i);
			}
			try {
				SyntaxAnalysis ();
			} catch (Exception ex) {
				Debug.WriteLine ("Syntax Error: " + ex.ToString ());
				if (ex is ParserException)
					SetLineInError (ex as ParserException);
			}
		}
		public void TryParseBufferLine(int lPtr) {
			buffer [lPtr].exception = null;
			currentLine = lPtr;
			currentColumn = 0;
			eol = false;

			try {
				ParseCurrentLine ();
			} catch (Exception ex) {
				Debug.WriteLine (ex.ToString ());
				if (ex is ParserException)
					SetLineInError (ex as ParserException);
			}

		}

		public virtual void SetLineInError(ParserException ex) {
			currentTok = default(Token);
			if (ex.Line >= buffer.LineCount)
				ex.Line = buffer.LineCount - 1;
			if (buffer [ex.Line].IsFolded)
				buffer.ToogleFolding (ex.Line);
			buffer [ex.Line].SetLineInError (ex);
		}
		public virtual string LineBrkRegex {
			get { return @"\r\n|\r|\n|\\\\n"; }
		}
		void updateFolding () {
			//			Stack<TokenList> foldings = new Stack<TokenList>();
			//			bool inStartTag = false;
			//
			//			for (int i = 0; i < parser.Tokens.Count; i++) {
			//				TokenList tl = parser.Tokens [i];
			//				tl.foldingTo = null;
			//				int fstTK = tl.FirstNonBlankTokenIndex;
			//				if (fstTK > 0 && fstTK < tl.Count - 1) {
			//					if (tl [fstTK + 1] != XMLParser.TokenType.ElementName)
			//						continue;
			//					if (tl [fstTK] == XMLParser.TokenType.ElementStart) {
			//						//search closing tag
			//						int tkPtr = fstTK+2;
			//						while (tkPtr < tl.Count) {
			//							if (tl [tkPtr] == XMLParser.TokenType.ElementClosing)
			//
			//							tkPtr++;
			//						}
			//						if (tl.EndingState == (int)XMLParser.States.Content)
			//							foldings.Push (tl);
			//						else if (tl.EndingState == (int)XMLParser.States.StartTag)
			//							inStartTag = true;
			//						continue;
			//					}
			//					if (tl [fstTK] == XMLParser.TokenType.ElementEnd) {
			//						TokenList tls = foldings.Pop ();
			//						int fstTKs = tls.FirstNonBlankTokenIndex;
			//						if (tls [fstTK + 1].Content == tl [fstTK + 1].Content) {
			//							tl.foldingTo = tls;
			//							continue;
			//						}
			//						parser.CurrentPosition = tls [fstTK + 1].Start;
			//						parser.SetLineInError(new ParserException(parser, "closing tag not corresponding"));
			//					}
			//
			//				}
			//			}
		}

		#region low level parsing
		protected void addCharToCurTok(char c, Point position){
			currentTok.Start = position;
			currentTok += c;
		}
		/// <summary>
		/// Read one char from current position in buffer and store it into the current token
		/// </summary>
		/// <param name="startOfTok">if true, set the Start position of the current token to the current position</param>
		protected void readToCurrTok(bool startOfTok = false){
			if (startOfTok)
				currentTok.Start = CurrentPosition;
			currentTok += Read();
		}
		/// <summary>
		/// read n char from the buffer and store it into the current token
		/// </summary>
		protected void readToCurrTok(int length) {
			for (int i = 0; i < length; i++)
				currentTok += Read ();
		}
		/// <summary>
		/// Save current token into current TokensLine and raz current token
		/// </summary>
		protected void saveAndResetCurrentTok() {
			currentTok.End = CurrentPosition;
			buffer[currentLine].Tokens.Add (currentTok);
			currentTok = default(Token);
		}
		/// <summary>
		/// read one char and add current token to current TokensLine, current token is reset
		/// </summary>
		/// <param name="type">Type of the token</param>
		/// <param name="startToc">set start of token to current position</param>
		protected void readAndResetCurrentTok(System.Enum type, bool startToc = false) {
			readToCurrTok ();
			saveAndResetCurrentTok (type);
		}
		/// <summary>
		/// Save current tok
		/// </summary>
		/// <param name="type">set the type of the tok</param>
		protected void saveAndResetCurrentTok(System.Enum type) {
			currentTok.Type = (TokenType)type;
			saveAndResetCurrentTok ();
		}
		protected void setPreviousTokOfTypeTo (TokenType inType, TokenType newType) {
			for (int i = currentLine; i >= 0; i--) {
				int j = buffer [i].Tokens.Count - 1;
				while (j >= 0) {
					if (buffer [i].Tokens [j].Type == inType) {
						Token t = buffer [i].Tokens [j];
						t.Type = newType;
						buffer [i].Tokens [j] = t;
						return;
					}
					j--;
				}				
			}
		}
		/// <summary>
		/// Peek next char, emit '\n' if current column > buffer's line length
		/// Throw error if eof is true
		/// </summary>
		protected virtual char Peek() {
			if (eol)
				throw new ParserException (currentLine, currentColumn, "Unexpected End of line");
			return currentColumn < buffer [currentLine].Length ?
				buffer [currentLine] [currentColumn] : '\n';
		}
		/// <summary>
		/// Peek n char from buffer or less if remaining char in buffer's line is less than requested
		/// if end of line is reached, no '\n' will be emitted, instead, empty string is returned. '\n' should be checked only
		/// with single char Peek().
		/// Throw error is eof is true
		/// </summary>
		/// <param name="length">Length.</param>
		protected virtual string Peek(int length) {
			if (eol)
				throw new ParserException (currentLine, currentColumn, "Unexpected End of Line");
			int lg = Math.Min(length, Math.Max (buffer [currentLine].Length - currentColumn, buffer [currentLine].Length - currentColumn - length));
			if (lg == 0)
				return "";
			return buffer [currentLine].Content.Substring (currentColumn, lg);
		}
		/// <summary>
		/// read one char from buffer at current position, if '\n' is read, current line is incremented
		/// and column is reset to 0
		/// </summary>
		protected virtual char Read() {
			char c = Peek ();
			if (c == '\n')
				eol = true;
			currentColumn++;
			return c;
		}
		protected virtual string Read(int charCount){
			string tmp = "";
			for (int i = 0; i < charCount; i++) {
				if (eol)
					break;
				tmp += Read ();
			}
			return tmp;
		}
		/// <summary>
		/// read until end of line is reached
		/// </summary>
		/// <returns>string read</returns>
		protected virtual string ReadLine () {
			StringBuilder tmp = new StringBuilder();
			char c = Read ();
			while (!eol) {
				tmp.Append (c);
				c = Read ();
			}
			return tmp.ToString();
		}
		/// <summary>
		/// read until end expression is reached or end of line.
		/// </summary>
		/// <returns>string read minus the ending expression that has to be read after</returns>
		/// <param name="endExp">Expression to search for</param>
		protected virtual string ReadLineUntil (string endExp){
			string tmp = "";

			while (!eol) {
				if (buffer [currentLine].Length - currentColumn - endExp.Length < 0) {
					tmp += ReadLine();
					break;
				}
				if (string.Equals (Peek (endExp.Length), endExp))
					return tmp;
				tmp += Read();
			}
			return tmp;
		}
		/// <summary>
		/// skip white spaces, but not line break. Save spaces in a WhiteSpace token.
		/// </summary>
		protected void SkipWhiteSpaces () {
			if (currentTok.Type != TokenType.Unknown)
				throw new ParserException (currentLine, currentColumn, "current token should be reset to unknown (0) before skiping white spaces");
			while (!eol) {
				if (!char.IsWhiteSpace (Peek ())||Peek()=='\n')
					break;
				readToCurrTok (currentTok.Type == TokenType.Unknown);
				currentTok.Type = TokenType.WhiteSpace;
			}
			if (currentTok.Type != TokenType.Unknown)
				saveAndResetCurrentTok ();
		}
		#endregion

		protected void throwParserException(string msg){
			throw new ParserException (currentLine, currentColumn, msg);
		}
	}
}