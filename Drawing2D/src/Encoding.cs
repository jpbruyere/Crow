﻿// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Drawing2D
{
	public static class Extensions
    {
        public static int ToUtf8 (this ReadOnlySpan<char> source, Span<byte> buff, int tabWidth = 4) {
            int c = 0;
            int encodedBytes = 0;
            int encodedChar = 0;
            while (c < source.Length) {
                if (source[c] < 0xD800) {
                    if (source[c] == '\t') {
                        int encTabWidth = tabWidth - encodedChar % tabWidth;
                        for (int i = 0; i < encTabWidth; i++) {
                            buff[encodedBytes++] = 0x20;
                            encodedChar++;
                        }
                        c++;
                        continue;
                    }

                    if (source[c] < 0x80) { //1 byte
                        buff[encodedBytes++] = (byte)source[c++];
                    }else if (source[c] < 0x7FF) { //2 bytes
                        buff[encodedBytes++] = (byte)((source[c] >> 6) + 0xC0);
                        buff[encodedBytes++] = (byte)((source[c] & 0x3F) + 0x80);
                        c++;
                    } else { //3 bytes
                        //ushort ch = (ushort)(source[c] - 0x10000u);
                        buff[encodedBytes++] = (byte)((source[c] >> 12) + 0xE0);
                        buff[encodedBytes++] = (byte)((source[c] >> 6) & 0x00BF);
                        buff[encodedBytes++] = (byte)((source[c] & 0x3F) + 0x80);
                        c++;
                    }

                    encodedChar++;
                    continue;
                }
                throw new NotImplementedException();
            }
			buff[encodedBytes] = 0;
            return encodedBytes;
        }


        /*unsafe static int Utf16toUtf8 (byte* cptr, byte* buff, int tabWidth) {

        }*/
    }
}
