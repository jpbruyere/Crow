//
//  Token.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
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

namespace Crow.Coding
{
	public struct Token
	{
		public BufferParser.TokenType Type;
		public string Content;
		public Point Start;
		public Point End;

		public string PrintableContent {
			get { return string.IsNullOrEmpty(Content) ? "" : Content.Replace("\t", new String(' ', Interface.TabSize)); }
		}

//		public Token (TokenType tokType, string content = ""){
//			Type = tokType;
//			Content = content;
//		}

		public bool IsEmpty { get { return string.IsNullOrEmpty(Content); }}

		public static bool operator == (Token t, System.Enum tt){
			return Convert.ToInt32(t.Type) == Convert.ToInt32(tt);
		}
		public static bool operator != (Token t, System.Enum tt){
			return Convert.ToInt32(t.Type) != Convert.ToInt32(tt);
		}
		public static bool operator == (System.Enum tt, Token t){
			return Convert.ToInt32(t.Type) == Convert.ToInt32(tt);
		}
		public static bool operator != (System.Enum tt, Token t){
			return Convert.ToInt32(t.Type) != Convert.ToInt32(tt);
		}

		public static Token operator +(Token t, char c){
			t.Content += c;
			return t;
		}
		public static Token operator +(Token t, string s){
			t.Content += s;
			return t;
		}
		public override string ToString ()
		{
			return string.Format ("[Tok{2}->{3}:{0}: {1}]", Type,Content,Start,End);
		}
	}
}

