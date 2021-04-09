using System.Security.Principal;
using System.Threading;
// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Crow.Text;
using System.Collections.Generic;
using Crow.Cairo;
using System.Threading.Tasks;
using System.Linq;

namespace Crow
{
	[Flags]
	public enum TokenType {
		Unknown,
		Trivia					= 0x0100,
		WhiteSpace				= 0x4100,
		Tabulation				= 0x4101,
		LineBreak				= 0x4102,
		LineComment				= 0x0103,
		BlockCommentStart		= 0x0104,
		BlockComment			= 0x0105,
		BlockCommentEnd			= 0x0106,
		Name					= 0x0200,
		ElementName				= 0x0201,
		AttributeName			= 0x0202,
		PI_Target				= 0x0203,
		Punctuation				= 0x0400,
		PI_Start				= 0x0401,// '<?'
		PI_End					= 0x0402,// '?>'
		Operator 				= 0x0800,
		EqualSign 				= 0x0801,
		AttributeValue 			= 0x2000,
		Keyword 				= 0x1000,
		ElementOpen 			= 0x0403,// '<'
		EndElementOpen			= 0x0404,// '</'
		EmptyElementClosing		= 0x0405,// '/>'
		ClosingSign				= 0x0406,// '>'
		DTDObjectOpen			= 0x04A0,// '<!'
		Content,
	}
	
	public struct Token {
		public readonly TokenType Type;
		public int Start;
		public readonly int Length;
		public int End => Start + Length;
		public TextSpan Span => new TextSpan (Start, End);

		public Token (TokenType type, int pos) {
			Type = type;
			Start = pos;
			Length = 1;
		}
		public Token (TokenType type, int start, int end) {
			Type = type;
			Start = start;
			Length = end - start;
		}		
		public override string ToString() => $"{Type},{Start} {Length}";
	}
	public class XmlSource {
		public Token[] Tokens;
		public readonly string Source;		

		public XmlSource (string _source) {
			Source = _source;
			Tokenizer tokenizer = new Tokenizer();
			Tokens = tokenizer.Tokenize (Source);

			foreach (Token t in Tokens)
				Console.WriteLine ($"{t,-40} {Source.AsSpan(t.Start, t.Length).ToString()}");
		}
		public class TokenizerException : Exception {
			public readonly int Position;
			public TokenizerException(string message, int position, Exception innerException = null)
					: base (message, innerException) {
				Position = position;
			}
		}

		class Tokenizer {
			enum States
			{
				Init,//first statement of prolog, xmldecl should only apear in this state
				prolog,//misc before doctypedecl
				ProcessingInstrucitons,
				DTD,
				DTDObject,//doctype finished				
				Xml,
				StartTag,//inside start tag
				Content,//after start tag with no closing slash
				EndTag
			}

			States curState = States.Init;
			List<Token> Toks = new List<Token>(100);

			public Tokenizer  () {}

			void skipWhiteSpaces (ref SpanCharReader reader) {
				while(!reader.EndOfSpan) {
					switch (reader.Peak) {
						case '\x85':
						case '\x2028':
						case '\xA':
							reader.Read();
							addTok (ref reader, TokenType.LineBreak);
							break;
						case '\xD':
							reader.Read();
							if (reader.IsNextCharIn ('\xA', '\x85'))
								reader.Read();
							addTok (ref reader, TokenType.LineBreak);														
							break;
						case '\x20':
						case '\x9':
							char c = reader.Read();									
							while (reader.TryPeak (c))
								reader.Read();
							addTok (ref reader, c == '\x20' ? TokenType.WhiteSpace : TokenType.Tabulation);
							break;
						default:
							return;
					}
				}
			}
			bool readName (ref SpanCharReader reader) {
				if (reader.EndOfSpan)
					return false;
				char c = reader.Peak;					
				if (char.IsLetter(c) || c == '_' || c == ':') {
					reader.Advance ();
					while (reader.TryPeak (ref c)) {									
						if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '\xB7'))
							return true;
						reader.Advance ();
					}
					return true;
				}
				return false;
			}

			int startOfTok;
			void addTok (ref SpanCharReader reader, TokenType tokType) {
				if (reader.CurrentPosition == startOfTok)
					return;
				Toks.Add (new Token(tokType, startOfTok, reader.CurrentPosition));
				startOfTok = reader.CurrentPosition;
			}
			public Token[] Tokenize (string source) {
				SpanCharReader reader = new SpanCharReader(source);
				
				startOfTok = 0;
				int curObjectLevel = 0;
				curState = States.Init;

				while(!reader.EndOfSpan) {

					skipWhiteSpaces (ref reader);

					if (reader.EndOfSpan)
						break;

					switch (reader.Peak) {				
					case '<':
						reader.Advance ();
						if (reader.TryPeak ('?')) {								
							reader.Advance ();
							addTok (ref reader, TokenType.PI_Start);
							readName (ref reader);
							addTok (ref reader, TokenType.PI_Target);
							curState = States.ProcessingInstrucitons;
						} else if (reader.TryPeak ('!')) {
							reader.Advance ();
							if (reader.TryPeak ("--")) {
								reader.Advance (2);
								addTok (ref reader, TokenType.BlockCommentStart);										
								if (reader.TryReadUntil ("-->")) {
									addTok (ref reader, TokenType.BlockComment);
									reader.Advance (3);											
									addTok (ref reader, TokenType.BlockCommentEnd);
								} else if (reader.TryPeak ("-->")) {
									reader.Advance (3);											
									addTok (ref reader, TokenType.BlockCommentEnd);
								}
							} else {
								addTok (ref reader, TokenType.DTDObjectOpen);
								if (readName (ref reader)) {
									addTok (ref reader, TokenType.Keyword);
									curState = States.DTDObject;
								}								
							}								
						} else if (reader.TryPeak('/')) {
							reader.Advance ();
							addTok (ref reader, TokenType.EndElementOpen);
							if (readName (ref reader)) {
								addTok (ref reader, TokenType.ElementName);
								if (reader.TryPeak('>')) {
									reader.Advance ();
									addTok (ref reader, TokenType.ClosingSign);

									if (--curObjectLevel > 0)
										curState = States.Content;
									else
										curState = States.Xml;
								} 
							}
						}else{							
							addTok (ref reader, TokenType.ElementOpen);							
							if (readName (ref reader)) {
								addTok (ref reader, TokenType.ElementName);								
								curState = States.StartTag;
							}
						}
						break;
					case '?':
						reader.Advance ();
						if (reader.TryPeak ('>')){
							reader.Advance ();
							addTok (ref reader, TokenType.PI_End);
						}else
							addTok (ref reader, TokenType.Unknown);						
						curState = States.prolog;						
						break;
					case '\'':
					case '"':
						char q = reader.Read();
						if (reader.TryReadUntil (q)) {
							reader.Advance ();
							addTok (ref reader, TokenType.AttributeValue);
						} else
							addTok (ref reader, TokenType.Unknown);
						break;
					case '=':
						reader.Advance();
						addTok (ref reader, TokenType.EqualSign);
						break;
					case '>':
						reader.Advance();
						addTok (ref reader, TokenType.ClosingSign);
						curObjectLevel++;
						curState = States.Content;
						break;
					case '/':
						reader.Advance();
						if (reader.TryRead ('>')) {
							addTok (ref reader, TokenType.EmptyElementClosing);
							if (--curObjectLevel > 0)
								curState = States.Content;
							else
								curState = States.Xml;
						}else
							addTok (ref reader, TokenType.Unknown);
						break;
					default:
						if (curState == States.StartTag || curState == States.ProcessingInstrucitons) {
							if (readName(ref reader))
								addTok (ref reader, TokenType.AttributeName);
							else if (reader.TryAdvance())
								addTok (ref reader, TokenType.Unknown);
						} else {
							reader.TryReadUntil ('<');
							addTok (ref reader, TokenType.Content);
						}
						break;
					}
				}

				return Toks.ToArray();
			}
			
		}

	}
	public class Editor : TextBox {
		XmlSource source;
		object TokenMutex = new object();

		void parse () {			
			XmlSource tmp = new XmlSource(_text);
			lock(TokenMutex)
				source = tmp;
			RegisterForGraphicUpdate();
		}
		protected override void onInitialized(object sender, EventArgs e)
		{
			base.onInitialized(sender, e);

		}
		Widget overlay;
		public override void OnTextChanged(object sender, TextChangeEventArgs e)
		{
			base.OnTextChanged(sender, e);
			//Task.Run(()=>parse());			
			parse();

			/*if (overlay == null && HasFocus)
				overlay = IFace.LoadIMLFragment(@"<Widget Width='50' Height='50' Background='Jet'/>");*/
		}
		public override void onKeyDown(object sender, KeyEventArgs e)
		{
			TextSpan selection = Selection;
			if (e.Key == Key.Tab && !selection.IsEmpty) {
				int lineStart = lines.GetLocation (selection.Start).Line;
				int lineEnd = lines.GetLocation (selection.End).Line;

				if (IFace.Shift) {
					for (int l = lineStart; l <= lineEnd; l++) {				
						if (Text[lines[l].Start] == '\t')
							update (new TextChange (lines[l].Start, 1, ""));
						else if (Char.IsWhiteSpace (Text[lines[l].Start])) {
							int i = 1;
							while (i < lines[l].Length && i < Interface.TAB_SIZE && Char.IsWhiteSpace (Text[i]))
								i++;
							update (new TextChange (lines[l].Start, i, ""));
						}
					}

				}else{
					for (int l = lineStart; l <= lineEnd; l++)		
						update (new TextChange (lines[l].Start, 0, "\t"));				
				}

                selectionStart = new CharLocation (lineStart, 0);
                CurrentLoc = new CharLocation (lineEnd, lines[lineEnd].Length);

				return;
			}
			base.onKeyDown(sender, e);			
		}
		int tabSize = 4;

		protected override void drawContent (Context gr) {
			lock(TokenMutex) {
				if (source == null || source.Tokens.Length == 0) {
					base.drawContent (gr);
					return;
				}
			
				Rectangle cb = ClientRectangle;
				fe = gr.FontExtents;
				double lineHeight = fe.Ascent + fe.Descent;

				CharLocation selStart = default, selEnd = default;
				bool selectionNotEmpty = false;

				if (HasFocus) {
					if (currentLoc?.Column < 0) {
						updateLocation (gr, cb.Width, ref currentLoc);
						NotifyValueChanged ("CurrentColumn", CurrentColumn);
					} else
						updateLocation (gr, cb.Width, ref currentLoc);

					if (overlay != null) {
						Point p = new Point((int)currentLoc.Value.VisualCharXPosition, (int)(lineHeight * (currentLoc.Value.Line + 1)));
						p += ScreenCoordinates (Slot).TopLeft;
						overlay.Left = p.X;
						overlay.Top = p.Y;
					}
					if (selectionStart.HasValue) {
						updateLocation (gr, cb.Width, ref selectionStart);
						if (CurrentLoc.Value != selectionStart.Value)
							selectionNotEmpty = true;
					}
					if (selectionNotEmpty) {
						if (CurrentLoc.Value.Line < selectionStart.Value.Line) {
							selStart = CurrentLoc.Value;
							selEnd = selectionStart.Value;
						} else if (CurrentLoc.Value.Line > selectionStart.Value.Line) {
							selStart = selectionStart.Value;
							selEnd = CurrentLoc.Value;
						} else if (CurrentLoc.Value.Column < selectionStart.Value.Column) {
							selStart = CurrentLoc.Value;
							selEnd = selectionStart.Value;
						} else {
							selStart = selectionStart.Value;
							selEnd = CurrentLoc.Value;
						}
					} else
						IFace.forceTextCursor = true;
				}

				double spacePixelWidth = gr.TextExtents (" ").XAdvance;
				int x = 0, y = 0;
				double pixX = cb.Left;


				Foreground.SetAsSource (IFace, gr);
				gr.Translate (-ScrollX, -ScrollY);


				ReadOnlySpan<char> sourceBytes = source.Source.AsSpan();
				Span<byte> bytes = stackalloc byte[128];
				TextExtents extents;
				int tokPtr = 0;
				Token tok = source.Tokens[tokPtr];
				bool multilineToken = false;				

				ReadOnlySpan<char> buff = sourceBytes;


				for (int i = 0; i < lines.Count; i++) {
					//if (!cancelLinePrint (lineHeight, lineHeight * y, cb.Height)) {

					if (multilineToken) {
						if (tok.End < lines[i].End) {//last incomplete line of multiline token
							buff = sourceBytes.Slice (lines[i].Start, tok.End - lines[i].Start);								
						} else {//print full line
							buff = sourceBytes.Slice (lines[i].Start, lines[i].Length);
						}
					}

					while (tok.Start < lines[i].End) {
						if (!multilineToken) {
							if (tok.End > lines[i].End) {//first line of multiline
								multilineToken = true;
								buff = sourceBytes.Slice (tok.Start, lines[i].End - tok.Start);
							} else
								buff = sourceBytes.Slice (tok.Start, tok.Length);

							if (tok.Type.HasFlag (TokenType.Punctuation))
								gr.SetSource(Colors.DarkGrey);
							else if (tok.Type.HasFlag (TokenType.Trivia))
								gr.SetSource(Colors.DimGrey);
							else if (tok.Type == TokenType.ElementName) {
								gr.SetSource(Colors.Green);
							}else if (tok.Type == TokenType.AttributeName) {
								gr.SetSource(Colors.Blue);
							}else if (tok.Type == TokenType.AttributeValue) {
								gr.SetSource(Colors.OrangeRed);
							}else if (tok.Type == TokenType.EqualSign) {
								gr.SetSource(Colors.Black);
							}else if (tok.Type == TokenType.PI_Target) {
								gr.SetSource(Colors.DarkSlateBlue);
							}else {
								gr.SetSource(Colors.Red);
							}									
						}

						int size = buff.Length * 4 + 1;
						if (bytes.Length < size)
							bytes = size > 512 ? new byte[size] : stackalloc byte[size];

						int encodedBytes = Crow.Text.Encoding.ToUtf8 (buff, bytes);

						if (encodedBytes > 0) {
							bytes[encodedBytes++] = 0;
							gr.TextExtents (bytes.Slice (0, encodedBytes), out extents);
							gr.MoveTo (pixX, lineHeight * y + fe.Ascent);
							gr.ShowText (bytes.Slice (0, encodedBytes));
							pixX += extents.XAdvance;
							x += buff.Length;								
						}

						if (multilineToken) {
							if (tok.End < lines[i].End)//last incomplete line of multiline token
								multilineToken = false;
							else
								break;
						}

						if (++tokPtr >= source.Tokens.Length)
							break;
						tok = source.Tokens[tokPtr];
					}

					if (HasFocus && selectionNotEmpty) {
						RectangleD lineRect = new RectangleD (cb.X,	lineHeight * y + cb.Top, pixX, lineHeight);
						RectangleD selRect = lineRect;
						
						if (i >= selStart.Line && i <= selEnd.Line) {
							if (selStart.Line == selEnd.Line) {
								selRect.X = selStart.VisualCharXPosition + cb.X;
								selRect.Width = selEnd.VisualCharXPosition - selStart.VisualCharXPosition;
							} else if (i == selStart.Line) {
								double newX = selStart.VisualCharXPosition + cb.X;
								selRect.Width -= (newX - selRect.X) - 10.0;
								selRect.X = newX;
							} else if (i == selEnd.Line)
								selRect.Width = selEnd.VisualCharXPosition - selRect.X + cb.X;
							else
								selRect.Width += 10.0;

							buff = sourceBytes.Slice(lines[i].Start, lines[i].Length);
							int size = buff.Length * 4 + 1;
							if (bytes.Length < size)
								bytes = size > 512 ? new byte[size] : stackalloc byte[size];

							int encodedBytes = Crow.Text.Encoding.ToUtf8 (buff, bytes);

							gr.SetSource (SelectionBackground);
							gr.Rectangle (selRect);
							if (encodedBytes < 0)
								gr.Fill ();
							else {
								gr.FillPreserve ();
								gr.Save ();
								gr.Clip ();
								gr.SetSource (SelectionForeground);
								gr.MoveTo (lineRect.X, lineRect.Y + fe.Ascent);
								gr.ShowText (bytes.Slice (0, encodedBytes));
								gr.Restore ();
							}
							Foreground.SetAsSource (IFace, gr);
						}
					}

					if (!multilineToken) {
						if (++tokPtr >= source.Tokens.Length)
							break;
						tok = source.Tokens[tokPtr];
					}

					x = 0;
					pixX = 0;
		
					y++;


						/*	} else if (tok2.Type == TokenType.Tabulation) {
								int spaceRounding = x % tabSize;
								int spaces = spaceRounding == 0 ?
									tabSize * tok2.Length :
									spaceRounding + tabSize * (tok2.Length - 1);
								x += spaces;
								pixX += spacePixelWidth * spaces;
								continue;
							} else if (tok2.Type == TokenType.WhiteSpace) {
								x += tok2.Length;
								pixX += spacePixelWidth * tok2.Length;*/																				
				}					
				gr.Translate (ScrollX, ScrollY);
			}
		}			
	}
}