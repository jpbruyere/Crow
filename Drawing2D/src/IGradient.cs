// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

namespace Drawing2D
{
	public interface IGradient : IPattern
	{
		void AddColorStop (double offset, Color c);
		void AddColorStop(float offset, float r, float g, float b, float a = 1f);
	}
}