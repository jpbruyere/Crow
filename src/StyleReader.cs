//
//  StyleReader.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Crow
{
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
