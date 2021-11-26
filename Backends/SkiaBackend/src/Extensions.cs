// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


using Drawing2D;
using SkiaSharp;

namespace Crow.SkiaBackend
{
	internal static class Extensions {
		internal static SKColor ToSkiaColor (this Color c)
			=> new Color (c.A, c.R, c.G, c.B).Value;
	}
}