﻿// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Crow.Coding;

namespace Crow
{
	/// <summary>
	/// Parser for style files.
	/// </summary>
	//TODO: style key shared by different class may use only first encouneter class setter, which can cause bug.
	public class StyleReader : StreamReader
	{
		enum States { classNames, members, value, endOfStatement }

		States curState = States.classNames;
		int column = 1;
		int line = 1;

		#region Character ValidityCheck
		static Regex rxValidChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		static Regex rxNameStartChar = new Regex(@"_|\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}");															
		static Regex rxNameChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		static Regex rxDecimal = new Regex(@"[0-9]+");
		static Regex rxHexadecimal = new Regex(@"[0-9a-fA-F]+");

		bool nextCharIsValidCharStartName {
			get => rxNameStartChar.IsMatch(new string(new char[]{PeekChar()}));
		}
		bool nextCharIsValidCharName {
			get => rxNameChar.IsMatch(new string(new char[]{PeekChar()})); 
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
		/// <param name="iFace">the Interface to load the style for</param>
		public void Parse (Interface iFace, string resId)
		{
			column = 1;
			line = 1;
			curState = States.classNames;

			string styleKey = resId.Substring (0, resId.Length - 6);
			string token = "";
			List<string> targetsClasses = new List<string> ();
			string currentProperty = "";

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
					if (!(curState == States.classNames) || string.IsNullOrEmpty (token))
						throw new ParserException (line, column, "Unexpected char ','", resId);
					targetsClasses.Add (token);
					token = "";
					curState = States.classNames;
					break;
				case '{':
					ReadChar ();
					if (curState != States.classNames || string.IsNullOrEmpty (token))
						throw new ParserException (line, column, "Unexpected char '{'", resId);
					targetsClasses.Add (token);
					token = "";
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
					if (!(curState == States.members || curState == States.classNames) || string.IsNullOrEmpty (token))
						throw new ParserException (line, column, "Unexpected char '='", resId);
					currentProperty = token;
					token = "";
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
								string constantId = "";
								while (!EndOfStream) {
									c = ReadChar ();
									if (c == '}')
										break;
									constantId += c;
								}
								if (string.IsNullOrEmpty (constantId) || !iFace.StylingConstants.ContainsKey (constantId))
									throw new ParserException (line, column, "Empty constant id in styling", resId);
								token += iFace.StylingConstants [constantId];
								continue;
							}
						} else if (c == '\"') {
							curState = States.endOfStatement;
							break;
						}
						token += c;
					}
					break;
				case ';':
					if (curState != States.endOfStatement)
						throw new ParserException (line, column, "Unexpected end of statement", resId);
					ReadChar ();
					if (targetsClasses.Count == 0) {
						//style constant
						if (!iFace.StylingConstants.ContainsKey (currentProperty))
							iFace.StylingConstants.Add (currentProperty, token);
						curState = States.classNames;
					} else {
						foreach (string tc in targetsClasses) {
							if (!iFace.Styling.ContainsKey (tc))
								iFace.Styling [tc] = new Style ();
							else if (iFace.Styling [tc].ContainsKey (currentProperty))
								continue;
							iFace.Styling [tc] [currentProperty] = token;
#if DESIGN_MODE
						styling [tc].Locations[currentProperty] = new FileLocation(resId, line, column - token.Length - 1, token.Length);
#endif
							//System.Diagnostics.Debug.WriteLine ("Style: {3} : {0}.{1} = {2}", tc, currentProperty, token, resId);
						}
						curState = States.members;
					}
					token = "";
					currentProperty = "";
					break;
				default:
					if (curState == States.value)
						throw new ParserException (line, column, "expecting value enclosed in '\"'", resId);
					if (curState == States.endOfStatement)
						throw new ParserException (line, column, "expecting end of statement", resId);

					if (nextCharIsValidCharStartName) {
						token += ReadChar ();
						while (nextCharIsValidCharName)
							token += ReadChar ();
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
