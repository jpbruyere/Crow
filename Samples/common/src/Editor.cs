// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Crow.Text;
using System.Collections.Generic;
using Crow.Drawing;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections;

namespace Crow
{
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
		ListBox overlay;
		IList suggestions;
		volatile bool disableSuggestions;
		public IList Suggestions {
			get => suggestions;
			set {
				suggestions = value;
				NotifyValueChangedAuto (suggestions);
				if (suggestions == null || suggestions.Count == 0)
					hideOverlay ();
				else
					showOverlay ();				
			}
		}
		bool suggestionsActive => overlay != null && overlay.IsVisible;
		Token currentToken;
		SyntaxNode currentNode;
		string[] allWidgetNames = typeof (Widget).Assembly.GetExportedTypes ().Where(t=>typeof(Widget).IsAssignableFrom (t))
					.Select (s => s.Name).ToArray ();


		IEnumerable<MemberInfo> getAllCrowTypeMembers (string crowTypeName) {
			Type crowType = IML.Instantiator.GetWidgetTypeFromName (crowTypeName);
			return crowType.GetMembers (BindingFlags.Public | BindingFlags.Instance).
				Where (m=>((m is PropertyInfo pi && pi.CanWrite) || (m is EventInfo)) &&
						m.GetCustomAttribute<XmlIgnoreAttribute>() == null);
		}
		MemberInfo getCrowTypeMember (string crowTypeName, string memberName) {
			Type crowType = IML.Instantiator.GetWidgetTypeFromName (crowTypeName);			
			return crowType.GetMember (memberName, BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();
		}		

		public override void OnTextChanged(object sender, TextChangeEventArgs e)
		{
			base.OnTextChanged(sender, e);
			//Task.Run(()=>parse());

			parse();
			
			if (!disableSuggestions && HasFocus)
				tryGetSuggestions ();

			//Console.WriteLine ($"{pos}: {suggestionTok.AsString (_text)} {suggestionTok}");
		}
		void tryGetSuggestions () {
			if (!currentLoc.HasValue)
				return;
			int pos = lines.GetAbsolutePosition (CurrentLoc.Value);	
			currentToken = source.FindTokenIncludingPosition (pos);
			currentNode = source.FindNodeIncludingPosition (pos);
			Console.WriteLine ($"Current Token: {currentToken} Current Node: {currentNode}");

			if (currentToken.Type == TokenType.ElementOpen) {
				Suggestions = new List<string> (allWidgetNames);
			} else if (currentToken.Type == TokenType.ElementName) {
				Suggestions = allWidgetNames.Where (s => s.StartsWith (currentToken.AsString (_text), StringComparison.OrdinalIgnoreCase)).ToList ();
			} else if (currentNode is AttributeSyntax attribNode) {
				if (currentNode.Parent is ElementTagSyntax eltTag) {
					if (eltTag.NameToken.HasValue) {
						if (currentToken.Type == TokenType.AttributeName) {
							Suggestions = getAllCrowTypeMembers (eltTag.NameToken.Value.AsString (_text))
								.Where (s => s.Name.StartsWith (currentToken.AsString (_text), StringComparison.OrdinalIgnoreCase)).ToList ();
						} else if (attribNode.NameToken.HasValue) {
							if (currentToken.Type == TokenType.AttributeValue) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.NameToken.Value.AsString (_text), attribNode.NameToken.Value.AsString (_text));
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										Suggestions = IFace.Styling.Keys
											.Where (s => s.StartsWith (currentToken.AsString (_text), StringComparison.OrdinalIgnoreCase)).ToList ();
									else if (pi.PropertyType.IsEnum)
										Suggestions = Enum.GetNames (pi.PropertyType)
											.Where (s => s.StartsWith (currentToken.AsString (_text), StringComparison.OrdinalIgnoreCase)).ToList ();
									else if (pi.PropertyType == typeof(bool))
										Suggestions = (new string[] {"true", "false"}).
											Where (s => s.StartsWith (currentToken.AsString (_text), StringComparison.OrdinalIgnoreCase)).ToList ();
									else if (pi.PropertyType == typeof (Measure))
										Suggestions = (new string[] {"Stretched", "Fit"}).
											Where (s => s.StartsWith (currentToken.AsString (_text), StringComparison.OrdinalIgnoreCase)).ToList ();
									else if (pi.PropertyType == typeof (Fill)) 
										Suggestions = EnumsNET.Enums.GetValues<Colors> ()
											.Where (s => s.ToString().StartsWith (currentToken.AsString (_text), StringComparison.OrdinalIgnoreCase)).ToList ();
								}
							} else if (currentToken.Type == TokenType.AttributeValueOpen) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.NameToken.Value.AsString (_text), attribNode.NameToken.Value.AsString (_text));
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										Suggestions = IFace.Styling.Keys.ToList ();
									if (pi.PropertyType.IsEnum)
										Suggestions = Enum.GetNames (pi.PropertyType).ToList ();
									else if (pi.PropertyType == typeof(bool))
										Suggestions = new List<string> (new string[] {"true", "false"});
									else if (pi.PropertyType == typeof (Fill)) 
										Suggestions = EnumsNET.Enums.GetValues<Colors> ().ToList ();
									else if (pi.PropertyType == typeof (Measure))
										Suggestions = new List<string> (new string[] {"Stretched", "Fit"});
								}
							}
						}
					}
				}			
			} else if (currentToken.Type != TokenType.AttributeValueClose && 
					currentToken.Type != TokenType.EmptyElementClosing && 
					currentToken.Type != TokenType.ClosingSign && 
					currentNode is ElementStartTagSyntax eltStartTag) {
				if (currentToken.Type == TokenType.AttributeName)
					Suggestions = getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (_text))
						.Where (s => s.Name.StartsWith (currentToken.AsString (_text), StringComparison.OrdinalIgnoreCase)).ToList ();
				//else if (currentToken.Type == TokenType.ElementName)
				//	Suggestions = getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (_text)).ToList ();
			} else {
				/*SyntaxNode curNode = source.FindNodeIncludingPosition (pos);
				Console.WriteLine ($"Current Node: {curNode}");
				if (curNode is ElementStartTagSyntax eltStartTag &&
					(currentToken.Type != TokenType.ClosingSign && currentToken.Type != TokenType.EmptyElementClosing && currentToken.Type != TokenType.Unknown)) {
					Suggestions = getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (_text)).ToList ();
				} else*/
					hideOverlay ();
			}
		}
		void showOverlay () {
			lock (IFace.UpdateMutex) {
				if (overlay == null) {
					overlay = IFace.LoadIMLFragment<ListBox>(@"
						<ListBox Style='suggestionsListBox' Data='{Suggestions}' UseLoadingThread='False' >
							<ItemTemplate>
								<ListItem Height='Fit' Margin='0' Focusable='false' HorizontalAlignment='Left' 
												Selected = '{Background=${ControlHighlight}}'
												Unselected = '{Background=Transparent}'>
									<Label Text='{}' HorizontalAlignment='Left' />
								</ListItem>							
							</ItemTemplate>
							<ItemTemplate DataType='System.Reflection.MemberInfo'>
								<ListItem Height='Fit' Margin='0' Focusable='false' HorizontalAlignment='Left' 
												Selected = '{Background=${ControlHighlight}}'
												Unselected = '{Background=Transparent}'>
									<HorizontalStack>
										<Image Picture='{GetIcon}' Width='16' Height='16'/>
										<Label Text='{Name}' HorizontalAlignment='Left' />
									</HorizontalStack>
								</ListItem>							
							</ItemTemplate>							
							<ItemTemplate DataType='Colors'>
								<ListItem Height='Fit' Margin='0' Focusable='false' HorizontalAlignment='Left' 
												Selected = '{Background=${ControlHighlight}}'
												Unselected = '{Background=Transparent}'>
									<HorizontalStack>
										<Widget Background='{}' Width='20' Height='14'/>
										<Label Text='{}' HorizontalAlignment='Left' />
									</HorizontalStack>
								</ListItem>							
							</ItemTemplate>
						</ListBox>
					");
					overlay.DataSource = this;
					overlay.Loaded += (sender, arg) => (sender as ListBox).SelectedIndex = 0;				
				} else
					overlay.IsVisible = true;
				overlay.RegisterForLayouting(LayoutingType.Sizing);	
			}
		}
		void hideOverlay () {
			if (overlay == null)
				return;
			overlay.IsVisible = false;
		}
		void completeToken () {			
			string selectedSugg = overlay.SelectedItem is MemberInfo mi ?
				mi.Name : overlay.SelectedItem?.ToString ();
			if (selectedSugg == null)
				return;
			if (currentToken.Type == TokenType.ElementOpen ||
				currentToken.Type == TokenType.WhiteSpace ||
				currentToken.Type == TokenType.AttributeValueOpen)
				update (new TextChange (currentToken.End, 0, selectedSugg));
			else if (currentToken.Type == TokenType.AttributeName && currentNode is AttributeSyntax attrib) {
					if (attrib.ValueToken.HasValue) {
						TextChange tc = new TextChange (currentToken.Start, currentToken.Length, selectedSugg);						
						update (tc);
						selectionStart = lines.GetLocation (attrib.ValueToken.Value.Start + tc.CharDiff + 1);
						CurrentLoc = lines.GetLocation (attrib.ValueToken.Value.End + tc.CharDiff - 1);
					} else {
						update (new TextChange (currentToken.Start, currentToken.Length, selectedSugg + "=\"\""));
						MoveLeft ();
					}					
			} else 
				update (new TextChange (currentToken.Start, currentToken.Length, selectedSugg));
			hideOverlay ();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e) {
			hideOverlay ();
			base.onMouseDown (sender, e);
		}
		
		public override void onKeyDown(object sender, KeyEventArgs e)
		{
			TextSpan selection = Selection;

			if (SelectionIsEmpty) {
				if (suggestionsActive) {
					switch (e.Key) {
					case Key.Escape:
						hideOverlay ();
						return;
					case Key.Left:
					case Key.Right:
						hideOverlay ();
						break;
					case Key.End:
					case Key.Home:
					case Key.Down:
					case Key.Up:
					case Key.PageDown:
					case Key.PageUp:
						overlay.onKeyDown (this, e);
						return;
					case Key.Tab:
					case Key.Enter:
					case Key.KeypadEnter:
						completeToken ();
						return;
					}
				} else if (e.Key == Key.Space && IFace.Ctrl) {
					tryGetSuggestions ();
					return;
				}
			} else if (e.Key == Key.Tab && !selection.IsEmpty) {
				int lineStart = lines.GetLocation (selection.Start).Line;
				int lineEnd = lines.GetLocation (selection.End).Line;

				disableSuggestions = true;

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

				disableSuggestions = false;

				return;
			}
			base.onKeyDown(sender, e);
		}		

		protected override void drawContent (Context gr) {
			try {
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

						if (overlay != null && overlay.IsVisible) {
							Point p = new Point((int)currentLoc.Value.VisualCharXPosition - ScrollX, (int)(lineHeight * (currentLoc.Value.Line + 1) - ScrollY));
							if (p.Y < 0 || p.X < 0)
								hideOverlay ();
							else {
								p += ScreenCoordinates (Slot).TopLeft;
								overlay.Left = p.X;
								overlay.Top = p.Y;
							}
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
					//gr.Translate (ScrollX, ScrollY);
				}
			} catch {
				
			}
		}			
	}
}