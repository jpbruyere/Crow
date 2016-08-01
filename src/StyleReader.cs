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


		public StyleReader (Assembly assembly, string resId)
			: base(assembly.GetManifestResourceStream (resId))
		{
			string styleKey = resId.Substring (0, resId.Length - 6);
			string token = "";
			List<string> targetsClasses = new List<string> ();
			string currentProperty = "";

			int curlyBracketCount = 0;

			while (!EndOfStream) {
				char c = (Char)Read ();

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
							throw new Exception ("Unexpected token '='");
						else if (targetsClasses.Count == 1) {
							if (!string.IsNullOrEmpty (token))
								throw new Exception ("Unexpected token '='");
							currentProperty = targetsClasses [0];
							targetsClasses [0] = styleKey;
						}else{
							if (string.IsNullOrEmpty (token))
								throw new Exception ("Unexpected token '='");
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
							throw new Exception ("Unexpected token '" + c + "'");
						targetsClasses = new List<string> ();
						currentProperty = "";
						state = readerState.classNames;
					} else
						token += c;
					break;
				case readerState.expression:
					if (curlyBracketCount == 0) {
						if (c == '{'){
							if (string.IsNullOrEmpty(token))
								throw new Exception ("Unexpected token '{'");
							curlyBracketCount++;
							token = "{";
						}else if (c == '}')
							throw new Exception ("Unexpected token '{'");
						else if (c == ';') {
							if (!string.IsNullOrEmpty (token)) {
								string expression = token.Trim ();

								foreach (string tc in targetsClasses) {
									if (!Interface.Styling.ContainsKey (tc))
										Interface.Styling [tc] = new Dictionary<string, object> ();
									else if (Interface.Styling [tc].ContainsKey (currentProperty))
										continue;
									Interface.Styling [tc] [currentProperty] = expression;
								}
								token = "";
							}
							state = readerState.propertyName;
						} else
							token += c;
					} else {
						if (c == '{')
							curlyBracketCount++;
						else if (c == '}')
							curlyBracketCount--;
						token += c;
					}
					break;
				}
			}

			if (curlyBracketCount > 0)
				throw new Exception ("Unexpected end of file");
		}
	}
}
