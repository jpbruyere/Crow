// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using Crow.Coding;
using System.Text;

namespace Crow
{
	/// <summary>
	/// Parser for style files.
	/// </summary>
	//TODO: style key shared by different class may use only first encouneter class setter, which can cause bug.
	public class StyleReader : StreamReader {
		enum States { classNames, members, value, endOfStatement }

		States curState = States.classNames;
		int column = 1;
		int line = 1;

		#region Character ValidityCheck
		/*static Regex rxValidChar = new Regex (@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		static Regex rxNameStartChar = new Regex (@"_|\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}");
		static Regex rxNameChar = new Regex (@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		static Regex rxDecimal = new Regex (@"[0-9]+");
		static Regex rxHexadecimal = new Regex (@"[0-9a-fA-F]+");*/

		bool nextCharIsValidCharStartName => Char.IsLetter (PeekChar ()) || PeekChar () == '_';
		bool nextCharIsValidCharName {
			get {
				if (Char.IsLetterOrDigit (PeekChar ()))
					return true;
				UnicodeCategory uc = Char.GetUnicodeCategory (PeekChar ());
				return
					uc == UnicodeCategory.NonSpacingMark ||
					uc == UnicodeCategory.SpacingCombiningMark ||
					uc == UnicodeCategory.ConnectorPunctuation ||
					uc == UnicodeCategory.Format;
			}
		}
		#endregion

		char ReadChar () {
			column++;
			return (Char)Read();
		}
		char PeekChar () {
			return (Char)Peek();
		}
		void SkipWhiteSpaceAndLineBreak (){
			while (!EndOfStream){				
				if (!PeekChar ().IsWhiteSpaceOrNewLine ())
					break;
				if (ReadChar () == '\n') {
					line++;
					column = 0;
				}
			}
		}

		/// <summary>
		/// Parse the full style stream and load the result in 'Styling' and 'StylingConstant'
		/// fields of the interface passed as argument.
		/// </summary>
		public void Parse (Dictionary<string, string> StylingConstants, Dictionary<string, Style> Styling, string resId)
		{
			column = 1;
			line = 1;
			curState = States.classNames;

			//string styleKey = resId.Substring (0, resId.Length - 6);
			StringBuilder token = new StringBuilder(128);
			StringBuilder constantId = new StringBuilder (128);

			List<string> targetsClasses = new List<string> ();
			string currentProperty = null;

			while (!EndOfStream) {
				SkipWhiteSpaceAndLineBreak ();
				if (EndOfStream)
					break;

				switch (Peek ()) {
				case '/':
					ReadChar ();
					if (PeekChar () != '/')
						throw new ParserException (line, column, "Unexpected char '/'", resId);
					ReadLine ();
					break;
				case ',':
					ReadChar ();
					if (curState != States.classNames || token.Length == 0)
						throw new ParserException (line, column, "Unexpected char ','", resId);
					targetsClasses.Add (token.ToString());
					token.Clear();
					curState = States.classNames;
					break;
				case '{':
					ReadChar ();
					if (curState != States.classNames || token.Length == 0)
						throw new ParserException (line, column, "Unexpected char '{'", resId);
					targetsClasses.Add (token.ToString());
					token.Clear();
					curState = States.members;
					break;
				case '}':
					ReadChar ();
					if (curState != States.members)
						throw new ParserException (line, column, "Unexpected char '}'", resId);
					curState = States.classNames;
					targetsClasses.Clear ();
					break;
				case '=':
					ReadChar ();
					if (!(curState == States.members || curState == States.classNames) || token.Length == 0)
						throw new ParserException (line, column, "Unexpected char '='", resId);
					currentProperty = token.ToString ();
					token.Clear ();
					curState = States.value;
					break;
				case '"':
					if (curState != States.value)
						throw new ParserException (line, column, "Unexpected char '\"'", resId);
					ReadChar ();

					while (!EndOfStream) {
						char c = ReadChar ();
						if (c == '$') {
							if (PeekChar () == '{') {
								ReadChar ();
								//constant replacement								
								while (!EndOfStream) {
									c = ReadChar ();
									if (c == '}')
										break;
									constantId.Append (c);
								}
								if (constantId.Length == 0)
									throw new ParserException (line, column, "Empty constant id in styling", resId);
								string cst = constantId.ToString ();
								constantId.Clear ();
								if (!StylingConstants.ContainsKey (cst))
									throw new ParserException (line, column, $"Constant id not found in styling ({cst})", resId);
								token.Append (StylingConstants[cst]);
								continue;
							}
						} else if (c == '\"') {
							curState = States.endOfStatement;
							break;
						}
						token.Append (c);
					}
					break;
				case ';':
					if (curState != States.endOfStatement)
						throw new ParserException (line, column, "Unexpected end of statement", resId);
					ReadChar ();
					if (targetsClasses.Count == 0) {
						//style constants
						StylingConstants[currentProperty] = token.ToString ();
						curState = States.classNames;
					} else {
						foreach (string tc in targetsClasses) {
							if (!Styling.ContainsKey (tc))
								Styling [tc] = new Style ();
							Styling[tc][currentProperty] = token.ToString ();
#if DESIGN_MODE
							Styling [tc].Locations[currentProperty] = new FileLocation(resId, line, column - token.Length - 1, token.Length);
#endif
						}
						curState = States.members;
					}
					token.Clear ();
					currentProperty = null;
					break;
				default:
					if (curState == States.value)
						throw new ParserException (line, column, "expecting value enclosed in '\"'", resId);
					if (curState == States.endOfStatement)
						throw new ParserException (line, column, "expecting end of statement", resId);

					if (nextCharIsValidCharStartName) {
						token.Append (ReadChar ());
						while (nextCharIsValidCharName)
							token.Append (ReadChar ());
					}
					break;
				}
			}

		}

		public StyleReader (Stream stream)
			: base(stream)
		{
		}
	}
}
