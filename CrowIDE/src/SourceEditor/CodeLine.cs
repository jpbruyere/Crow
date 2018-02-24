using System;
using System.Text;
using System.Collections.Generic;

namespace Crow.Coding
{
	public class CodeLine
	{
		public string Content;
		public List<Token> Tokens;
		public int EndingState = 0;
		public Node SyntacticNode;
		public ParsingException exception;

		public CodeLine (string _content){
			Content = _content;
			Tokens = null;
			exception = null;
		}

		public char this[int i]
		{
			get { return Content[i]; }
			set {
				if (Content [i] == value)
					return;
				StringBuilder sb = new StringBuilder(Content);
				sb[i] = value;
				Content = sb.ToString();
				Tokens = null;
				//LineUpadateEvent.Raise (this, new CodeBufferEventArgs (i));
			}
		}
		public bool IsFoldable { get { return SyntacticNode != null; } }
		public bool IsFolded = false;
		public bool IsParsed {
			get { return Tokens != null; }
		}
		public string PrintableContent {
			get {
				return string.IsNullOrEmpty (Content) ? "" : Content.Replace ("\t", new String (' ', Interface.TabSize));
			}
		}
		public int PrintableLength {
			get {
				return PrintableContent.Length;
			}
		}
		public int Length {
			get {
				return string.IsNullOrEmpty (Content) ? 0 : Content.Length;
			}
		}

		public void SetLineInError (ParsingException ex) {
			Tokens = null; 
			exception = ex;
		}

//		public static implicit operator string(CodeLine sl) {
//			return sl == null ? "" : sl.Content;
//		}
		public static implicit operator CodeLine(string s) {
			return new CodeLine(s);
		}
		public static bool operator ==(string s1, CodeLine s2)
		{
			return string.Equals (s1, s2.Content);
		}
		public static bool operator !=(string s1, CodeLine s2)
		{
			return !string.Equals (s1, s2.Content);
		}
	}
}

