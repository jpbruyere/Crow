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

namespace Crow
{
	/// <summary>
	/// Parser for style files.
	/// </summary>
	//TODO: style key shared by different class may use only first encouneter class setter, which can cause bug.
	public class StyleReader : StreamReader
	{
		enum readerState { classNames, propertyName, expression }
		readerState state = readerState.classNames;
		string resourceId;
		int column = 1;
		int line = 1;

		public StyleReader (Assembly assembly, string resId)
			: base(assembly.GetManifestResourceStream (resId))
		{
			resourceId = resId;
			string styleKey = resId.Substring (0, resId.Length - 6);
			string token = "";
			List<string> targetsClasses = new List<string> ();
			string currentProperty = "";

			int curlyBracketCount = 0;

			while (!EndOfStream) {
				char c = (Char)Read ();
				if (c == '/' && !EndOfStream) {
					if ((char)Peek () == '/') {//process comment, skip until newline
						ReadLine ();
						continue;
					}
				}
				switch (state) {
				case readerState.classNames:
					if (c.IsWhiteSpaceOrNewLine () || c == ',' || c == '{') {
						if (!string.IsNullOrEmpty (token))
							targetsClasses.Add (token);
						if (c == '{')
							state = readerState.propertyName;
						token = "";
					}else if (c=='='){
						//this file contains only properties,
						//resource Id (minus .style extention) will determine the single target class
						if (targetsClasses.Count > 1)
							throwParserException ("Unexpected token '='");
						else if (targetsClasses.Count == 1) {
							if (!string.IsNullOrEmpty (token))
								throwParserException ("Unexpected token '='");
							currentProperty = targetsClasses [0];
							targetsClasses [0] = styleKey;
						}else{
							if (string.IsNullOrEmpty (token))
								throwParserException ("Unexpected token '='");
							targetsClasses.Add (styleKey);
							currentProperty = token;
							token = "";
						}
						state = readerState.expression;
					}else
						token += c;
					break;
				case readerState.propertyName:
					if (c.IsWhiteSpaceOrNewLine () || c == '=') {
						if (!string.IsNullOrEmpty (token))
							currentProperty = token;
						if (c == '=')
							state = readerState.expression;

						token = "";
					}else if (c == '}'){
						if (!string.IsNullOrEmpty (token))
							throwParserException ("Unexpected token '" + c + "'");
						targetsClasses = new List<string> ();
						currentProperty = "";
						state = readerState.classNames;
					} else
						token += c;
					break;
				case readerState.expression:
					bool expressionIsFinished = false;
					if (curlyBracketCount == 0) {
						if (c == '{'){
							if (!string.IsNullOrEmpty(token.Trim()))
								throwParserException ("Unexpected token '{'");
							curlyBracketCount++;
							token = "{";
						}else if (c == '}')
							throwParserException ("Unexpected token '{'");
						else if (c == ';') {
							expressionIsFinished = true;
						} else
							token += c;
					} else {
						if (c == '{')
							curlyBracketCount++;
						else if (c == '}') {
							curlyBracketCount--;
							if (curlyBracketCount == 0)
								expressionIsFinished = true;
						}
						token += c;
					}
					if (expressionIsFinished) {
						if (!string.IsNullOrEmpty (token)) {
							string expression = token.Trim ();

							foreach (string tc in targetsClasses) {
								if (!Interface.Styling.ContainsKey (tc))
									Interface.Styling [tc] = new Style ();
								else if (Interface.Styling [tc].ContainsKey (currentProperty))
									continue;
								Interface.Styling [tc] [currentProperty] = expression;
							}
							token = "";
						}
						//allow omiting ';' if curly bracket close expression
						while (!EndOfStream) {
							if (Char.IsWhiteSpace((char)Peek()))
								Read();
							else
								break;
						}
						if (this.Peek () == ';')
							this.Read ();
						state = readerState.propertyName;							
					}
					break;
				}
			}

			if (curlyBracketCount > 0)
				throwParserException ("Unexpected end of file");
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

		void throwParserException(string message){
			throw new Exception (string.Format ("Style Reader Exception ({0},{1}): {2} in {3}.",
				line, column, message, resourceId));
		}
	}
}
