// Copyright (c) 2018-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Drawing2D
{
	public interface IDevice: IDisposable
	{
		//IntPtr Handle => handle;

		void GetDpy (out int hdpy, out int vdpy);
		void SetDpy (int hdpy, int vdpy);
		ISurface CreateSurface (int width, int height);
	}
}

