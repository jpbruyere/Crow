// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Drawing2D;
using OpenGL;
using static OpenGL.Gl;

namespace Crow.CairoBackend
{
	public abstract class GLDevice : CairoDevice
	{
		protected GLDevice (IntPtr handle, bool owner = true) : base (handle, owner) {}
		public void SetThreadAware (bool value) {
			NativeMethods.cairo_gl_device_set_thread_aware (handle, value ? 1 : 0);
		}
		public virtual ISurface CreateSurface(int width, int height) {
			uint tex = GenTexture ();
			BindTexture (TextureTarget.Texture2d, tex);
			TexImage2D (TextureTarget.Texture2d, 0, InternalFormat.Rgb, width, height,
				0, PixelFormat.Rgb, PixelType.UnsignedByte, 0);
			TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, NEAREST);
			TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, NEAREST);
			return new GLSurface (this, Content.ColorAlpha, tex, width, height);
		}

	}
}

