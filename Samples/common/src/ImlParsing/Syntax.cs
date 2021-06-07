// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crow
{
	public class SyntaxException : Exception {
		public readonly Token Token;		
		public SyntaxException(string message, Token token = default, Exception innerException = null)
				: base (message, innerException) {
			Token = token;
		}
	}
	public class SyntaxAnalyser {		
		XmlSource source;
		IEnumerable<Token> tokens => source.Tokens;
		public SyntaxNode Root => CurrentNode;
		public SyntaxAnalyser (XmlSource source) {
			this.source = source;
			//tokens = source.Tokens.Where (t => !t.Type.HasFlag (TokenType.Trivia));
		}


		/*List<SyntaxNode> readAttributes (IEnumerator<Token> iter) {
			List<SyntaxNode> attributes = new List<SyntaxNode> (10);
			while (iter.MoveNext ()) {
				switch (iter.Current.Type) {
				case TokenType.EmptyElementClosing:
				case TokenType.ClosingSign:
					return attributes;
				case TokenType.AttributeName:
					Token attribName = iter.Current;
					moveNextOrThrow (iter);
					Token equalSign = accept (iter, TokenType.EqualSign);
					moveNextOrThrow (iter);
					Token attribValue = accept (iter, TokenType.AttributeValue);
					attributes.Add (new AttributeSyntax (source, attribName, equalSign, attribValue));
					break;
				}
			}
			throw new SyntaxException ("Unexpected end of source");
		}

		Token accept (IEnumerator<Token> iter, TokenType acceptedTokenType) {				
			if (iter.Current.Type == acceptedTokenType)
				return iter.Current;
			else
				throw new SyntaxException ("Unexpected token", iter.Current);
		}
		void moveNextOrThrow (IEnumerator<Token> iter) {
			if (!iter.MoveNext ())
				throw new SyntaxException ("Unexpected end of source");
		}
		ElementStartTagSyntax readElementStart (IEnumerator<Token> iter) {
			Token eltOpen = iter.Current;
			moveNextOrThrow (iter);
			Token eltName = accept (iter, TokenType.ElementName);
			
			List<SyntaxNode> attributes = readAttributes (iter);

			if (iter.Current.Type == TokenType.EmptyElementClosing || iter.Current.Type == TokenType.ClosingSign)
				return new ElementStartTagSyntax(source, eltOpen, eltName, iter.Current, attributes);

			throw new SyntaxException ("Unexpected token", iter.Current);
		}
		ElementEndTagSyntax readElementEnd (IEnumerator<Token> iter) {
			Token eltOpen = iter.Current;
			moveNextOrThrow (iter);
			Token eltName = accept (iter, TokenType.ElementName);
			moveNextOrThrow (iter);			
			return new ElementEndTagSyntax(source, eltOpen, eltName, accept (iter, TokenType.ClosingSign));
		}*/

		SyntaxNode CurrentNode;
		Token previousTok;
		IEnumerator<Token> iter;
		public List<SyntaxException> Exceptions { get; private set; }

		void finishCurrentNode () {

		}

		public void Process () {
			Exceptions = new List<SyntaxException> ();
			CurrentNode = new IMLRootSyntax (source);
			previousTok = default;
			iter = tokens.GetEnumerator ();			

			bool notEndOfSource = iter.MoveNext ();
			while (notEndOfSource) {
				if (!iter.Current.Type.HasFlag (TokenType.Trivia)) {
					if (CurrentNode is ElementStartTagSyntax tag) {
						if (iter.Current.Type == TokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (iter.Current);
							attribute.NameToken = iter.Current;
							CurrentNode = CurrentNode.AddChild (attribute);
						} else if (iter.Current.Type == TokenType.ElementName)
							tag.NameToken = iter.Current;
						else if (iter.Current.Type == TokenType.ClosingSign) {
							tag.EndToken = iter.Current;						
							CurrentNode = tag.Parent;
							CurrentNode.RemoveChild (tag);
							CurrentNode = CurrentNode.AddChild (new ElementSyntax (tag));
						} else if (iter.Current.Type == TokenType.EmptyElementClosing) {
							tag.EndToken = iter.Current;
							CurrentNode = tag.Parent;
							CurrentNode.RemoveChild (tag);
							CurrentNode.AddChild (new EmptyElementSyntax (tag));
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							CurrentNode.EndToken = previousTok;
							CurrentNode = CurrentNode.Parent;
							continue;						
						}
					} else if (CurrentNode is ElementSyntax elt) {
						if (iter.Current.Type == TokenType.ElementOpen)
							CurrentNode = CurrentNode.AddChild (new ElementStartTagSyntax (iter.Current));
						else if (iter.Current.Type == TokenType.EndElementOpen) {						
							elt.EndTag = new ElementEndTagSyntax (iter.Current);						
							CurrentNode = elt.AddChild (elt.EndTag);
						}
					} else if (CurrentNode is AttributeSyntax attrib) {
						if (iter.Current.Type == TokenType.EqualSign)
							if (attrib.EqualToken.HasValue)
								Exceptions.Add (new SyntaxException  ("Extra equal sign in attribute syntax", iter.Current));
							else
								attrib.EqualToken = iter.Current;
						else if (iter.Current.Type == TokenType.AttributeValueOpen)
							attrib.ValueOpenToken = iter.Current;
						else if (iter.Current.Type == TokenType.AttributeValue)
							attrib.ValueToken = iter.Current;
						else if (iter.Current.Type == TokenType.AttributeValueClose) {
							attrib.ValueCloseToken = attrib.EndToken = iter.Current;
							CurrentNode = CurrentNode.Parent;
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							CurrentNode.EndToken = previousTok;
							CurrentNode = CurrentNode.Parent;
							continue;						
						}
					} else if (CurrentNode is ElementEndTagSyntax eltEndTag) {
						if (iter.Current.Type == TokenType.ElementName)
							eltEndTag.NameToken = iter.Current;
						else if (iter.Current.Type == TokenType.ClosingSign) {
							eltEndTag.EndToken = eltEndTag.Parent.EndToken = iter.Current;
							CurrentNode = eltEndTag.Parent.Parent;
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							eltEndTag.EndToken = eltEndTag.Parent.EndToken = previousTok;
							CurrentNode = CurrentNode.Parent.Parent;
							continue;						
						}
					} else if (CurrentNode is IMLRootSyntax) {
						switch (iter.Current.Type) {
							case TokenType.ElementOpen:
								CurrentNode = CurrentNode.AddChild (new ElementStartTagSyntax (iter.Current));
								break;
							case TokenType.PI_Start:
								CurrentNode = CurrentNode.AddChild (new ProcessingInstructionSyntax (iter.Current));
								break;
							default:
								Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
								break;
						}
					} else if (CurrentNode is ProcessingInstructionSyntax pi) {
						if (iter.Current.Type == TokenType.PI_Target)
							pi.NameToken = iter.Current;
						else if (iter.Current.Type == TokenType.PI_End) {
							pi.EndToken = iter.Current;
							CurrentNode = CurrentNode.Parent;
						} else if (iter.Current.Type == TokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (iter.Current);
							attribute.NameToken = iter.Current;
							CurrentNode = CurrentNode.AddChild (attribute);
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							pi.EndToken = previousTok;
							CurrentNode = CurrentNode.Parent;
							continue;						
						}
					}
				}
				
				previousTok = iter.Current;
				notEndOfSource = iter.MoveNext ();
			}
			while (CurrentNode.Parent != null) {
				if (!CurrentNode.EndToken.HasValue)
					CurrentNode.EndToken = previousTok;
				CurrentNode = CurrentNode.Parent;
			}			
		}
	}
	public class SyntaxNode {
		public SyntaxNode Parent { get; private set; }
		List<SyntaxNode> children = new List<SyntaxNode> ();
		
		public readonly Token StartToken;
		public Token? EndToken { get; internal set; }
		public SyntaxNode (Token tokStart, Token? tokEnd = null) {			
			StartToken = tokStart;
			EndToken = tokEnd;
		}

		public virtual bool IsComplete => EndToken.HasValue;

		internal SyntaxNode AddChild (SyntaxNode child) {
			children.Add (child);
			child.Parent = this;
			return child;
		}
		internal void RemoveChild (SyntaxNode child) {
			children.Remove (child);
			child.Parent = null;
		}
		public T GetChild<T> () => children.OfType<T> ().FirstOrDefault ();
		public SyntaxNode FindNodeIncludingPosition (int pos) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (pos))
					return node.FindNodeIncludingPosition (pos);
			}
			return this;
		}
		public T FindNodeIncludingPosition<T> (int pos) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (pos))
					return node.FindNodeIncludingPosition<T> (pos);
			}

			return this is T tt ? tt : default;
		}
		public virtual IMLRootSyntax Root => Parent.Root;
		public  XmlSource Source => Root.source;
		public bool Contains (int pos) =>
			EndToken.HasValue ?
				StartToken.Start <= pos && EndToken.Value.End > pos : false;

		public void Dump (int level = 0) {
			Console.WriteLine ($"{new string('\t', level)}{this}");
			foreach (SyntaxNode node in children)
				node.Dump (level + 1);
		}
		public override string ToString() => $"{this.GetType().Name}: {StartToken} -> {EndToken}";
	}
	public class IMLRootSyntax : SyntaxNode {
		internal readonly XmlSource source;
		public override IMLRootSyntax Root => this;
		public IMLRootSyntax (XmlSource source)
			: base (source.Tokens.FirstOrDefault (), source.Tokens.LastOrDefault ()) {
			this.source = source;
		}
	}
	public class ProcessingInstructionSyntax : SyntaxNode {
		public Token PIStartToken => StartToken;
		public Token? PIEndToken => EndToken.HasValue && EndToken.Value.Type == TokenType.PI_End ? EndToken : null;
		public Token? NameToken { get; internal set; }
		public override bool IsComplete => base.IsComplete & NameToken.HasValue;
		public ProcessingInstructionSyntax (Token startTok)
			: base (startTok) {
		}
	}

	public abstract class ElementTagSyntax : SyntaxNode {
		public Token OpenToken => StartToken;
		public Token? NameToken { get; internal set; }
		public Token? CloseToken => EndToken.HasValue && EndToken.Value.Type == TokenType.ClosingSign ? EndToken : null;
		public override bool IsComplete => base.IsComplete & NameToken.HasValue & CloseToken.HasValue;
		protected ElementTagSyntax (Token startTok)
			: base (startTok) {
		}
	}	
	public class ElementStartTagSyntax : ElementTagSyntax {
		public ElementStartTagSyntax (Token startTok)
			: base (startTok) {
		}
	}
	public class ElementEndTagSyntax : ElementTagSyntax {
		public ElementEndTagSyntax (Token startTok)
			: base (startTok) {
		}
	}
	
	public class EmptyElementSyntax : SyntaxNode {
		public readonly ElementStartTagSyntax StartTag;
		public EmptyElementSyntax (ElementStartTagSyntax startNode) : base (startNode.StartToken, startNode.EndToken) {
			StartTag = startNode;			
			AddChild (StartTag);
		}
	}

	public class ElementSyntax : SyntaxNode {
		public readonly ElementStartTagSyntax StartTag;
		public ElementEndTagSyntax EndTag { get; internal set; }

		public override bool IsComplete => base.IsComplete & StartTag.IsComplete & (EndTag != null && EndTag.IsComplete);

		public ElementSyntax (ElementStartTagSyntax startTag)
			: base (startTag.StartToken) {			
			StartTag = startTag;
			AddChild (StartTag);
		}
	}

	public class AttributeSyntax : SyntaxNode {
		public Token? NameToken { get; internal set; }
		public Token? EqualToken { get; internal set; }
		public Token? ValueOpenToken { get; internal set; }		
		public Token? ValueCloseToken { get; internal set; }		
		public Token? ValueToken { get; internal set; }		
		public AttributeSyntax (Token startTok) : base  (startTok) {}
		public override bool IsComplete => base.IsComplete & NameToken.HasValue & EqualToken.HasValue & ValueToken.HasValue & ValueOpenToken.HasValue & ValueCloseToken.HasValue;
	}
}