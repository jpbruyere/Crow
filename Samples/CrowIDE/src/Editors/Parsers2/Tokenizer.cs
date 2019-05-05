//
// Tokenizer.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using Crow.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Crow.Coding2
{
	public static class TokenType {
		public const int Undefine = 0;
		public const int WhiteSpace = 0;
	}
	public class Token {
		public int ptr;
		public int length;
	}

	public class Tokenizer
	{
		#region Regular Expression for validity checks
		public Regex rxValidChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		public Regex rxNameStartChar = new Regex(@"_|\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}");
		public Regex rxNameChar = new Regex(@"\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}|\p{Mn}|\p{Mc}|\p{Nd}|\p{Pc}|\p{Cf}");
		public Regex rxNewLineChar = new Regex(@"\u000D|\u000A|\u0085|\u2028|\u2029");
		public Regex rxWhiteSpaceChar = new Regex(@"\p{Zs}|\u0009|\u000B|\u000C");
		public Regex rxDecimal = new Regex(@"[0-9]+");
		public Regex rxHexadecimal = new Regex(@"[0-9a-fA-F]+");
		#endregion

		public List<Token> Tokens;

		public Tokenizer (TextBuffer buffer)
		{
		}


		public void Tokenize () {
			
		}

	}
}

