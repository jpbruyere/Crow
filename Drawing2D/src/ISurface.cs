// Copyright (c) 2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Drawing2D
{
	public interface ISurface: IDisposable
	{
		IntPtr Handle { get; }
		int Width { get; }
		int Height { get; }

		void Flush ();

		/*void WriteToPng (string path);
		void WriteTo (IntPtr bitmap);
		void Clear ();
		ISurface CreateSimilar (int width, int height);*/
		void Resize (int width, int height);
	}
}

