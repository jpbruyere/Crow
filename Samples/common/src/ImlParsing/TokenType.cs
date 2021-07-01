// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

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
		ElementOpen 			= 0x0403,// '<'
		EndElementOpen			= 0x0404,// '</'
		EmptyElementClosing		= 0x0405,// '/>'
		ClosingSign				= 0x0406,// '>'
		DTDObjectOpen			= 0x04A0,// '<!'
		Operator 				= 0x0800,
		EqualSign 				= 0x0801,
		Keyword 				= 0x1000,
		AttributeValue			= 0x2000,
		AttributeValueOpen		= 0x2001,
		AttributeValueClose		= 0x2002,
		Content,
	}
}