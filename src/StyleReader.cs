//
// StyleReader.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
		enum States { init, classNames, members, value, endOfStatement }

		States curState = States.init;

		int column = 1;
		int line = 1;

		#region Character ValidityCheck
		static Regex rxValidChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		static Regex rxNameStartChar = new Regex(@"_|\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}");															
		static Regex rxNameChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		static Regex rxDecimal = new Regex(@"[0-9]+");
		static Regex rxHexadecimal = new Regex(@"[0-9a-fA-F]+");

		public bool nextCharIsValidCharStartName
		{
			get { return rxNameStartChar.IsMatch(new string(new char[]{PeekChar()})); }
		}
		public bool nextCharIsValidCharName
		{
			get { return rxNameChar.IsMatch(new string(new char[]{PeekChar()})); }
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

		public StyleReader (Dictionary<string, Style> styling, Stream stream, string resId)
			: base(stream)
		{			
			string styleKey = resId.Substring (0, resId.Length - 6);
			string token = "";
			List<string> targetsClasses = new List<string> ();
			string currentProperty = "";

			while (!EndOfStream) {
				SkipWhiteSpaceAndLineBreak ();
				if (EndOfStream)
					break;

				switch (Peek()) {
				case '/':
					ReadChar ();
					if (PeekChar () != '/')
						throw new ParserException (line, column, "Unexpected char '/'", resId);
					ReadLine ();
					break;
				case ',':
					ReadChar ();
					if (!(curState == States.init || curState == States.classNames) || string.IsNullOrEmpty (token))
						throw new ParserException (line, column, "Unexpected char ','", resId);
					targetsClasses.Add (token);
					token = "";
					curState = States.classNames;
					break;
				case '{':
					ReadChar ();
					if (!(curState == States.init || curState == States.classNames) || string.IsNullOrEmpty (token))
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
					if (!(curState == States.init || curState == States.members))
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
						char c = PeekChar();
						if (c == '\"') {
							ReadChar ();
							break;
						}
						token += ReadChar();
						if (c == '\\' && !EndOfStream)
							token += ReadChar();						
					}
					curState = States.endOfStatement;
					break;
				case ';':
					if (curState != States.endOfStatement)
						throw new ParserException (line, column, "Unexpected end of statement", resId);					
					ReadChar ();
					foreach (string tc in targetsClasses) {
						if (!styling.ContainsKey (tc))
							styling [tc] = new Style ();
						else if (styling [tc].ContainsKey (currentProperty))
							continue;
						styling [tc] [currentProperty] = token;
						#if DESIGN_MODE
						styling [tc].Locations[currentProperty] = new FileLocation(resId, line,column);
						#endif
						//System.Diagnostics.Debug.WriteLine ("Style: {3} : {0}.{1} = {2}", tc, currentProperty, token, resId);
					}
					token = "";
					curState = States.members;
					break;
				default:
					if (curState == States.value)
						throw new ParserException (line, column, "expecting value enclosed in '\"'", resId);
					if (curState == States.endOfStatement)
						throw new ParserException (line, column, "expecting end of statement", resId);

					if (nextCharIsValidCharStartName) {
						token += ReadChar();
						while (nextCharIsValidCharName)
							token += ReadChar();
					}
					break;
				}
			}
		}

		public override int Read ()
		{			
			int tmp = base.Read ();
			char c = (char)tmp;
			if (c == '\n') {
				line++;
				column = 1;
			} else if (c == '\t')
				column += Interface.TabSize;
			else if (c != '\r')
				column++;
			return tmp;
		}
	}
}
