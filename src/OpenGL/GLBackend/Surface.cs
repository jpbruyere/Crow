using System;
using OpenTK.Graphics.OpenGL;

namespace go.GLBackend
{
	[Serializable]
	public enum Format
	{
		Argb32,
		Rgb24,
		A8,
		A1,
		Rgb16565,
		ARGB32,
		RGB24
	}
	[Serializable]
	public enum LineJoin
	{
		Miter,
		Round,
		Bevel
	}
	[Serializable]
	public enum LineCap
	{
		Butt,
		Round,
		Square
	}
	public class Surface : IDisposable
	{
		public static int samples;

		public int 	texId,
					width,
					height;
		Format format;

		public PixelFormat PixelFormat;
		public PixelInternalFormat InternalFormat;

		public Surface (int _width, int _height)
		{
			format = Format.Argb32;
			width = _width;
			height = _height;

			createTexture ();
		}

		public Surface (Format _format,int _width, int _height)
		{
			format = _format;
			width = _width;
			height = _height;

			InternalFormat = PixelInternalFormat.Rgba;
			PixelFormat = PixelFormat.Bgra;

			createTexture ();
		}

		public Surface CreateSimilar()
		{
			return new Surface (format, width, height);
		}
			
		void createTexture()
		{
			texId = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, texId);
			GL.TexImage2D(TextureTarget.Texture2D,0,
				PixelInternalFormat.Rgba, width, height,0,PixelFormat.Bgra,PixelType.UnsignedByte,IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}


		public void Save(string fileName)
		{
			GL.BindTexture(TextureTarget.Texture2D, texId);
			System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			System.Drawing.Imaging.BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
				System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			bmp.UnlockBits(data);
			bmp.RotateFlip (System.Drawing.RotateFlipType.RotateNoneFlipY);
			bmp.Save(fileName);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (GL.IsTexture (texId))
				GL.DeleteTexture (texId);
		}
		#endregion
	}
}

