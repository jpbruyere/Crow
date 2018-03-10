//
//  Texture.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
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
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;

namespace Crow
{
    public class Texture
    {
        public string Map;
        public int texRef;
		public int Width;
		public int Height;
		        
		public Texture(string _mapPath, bool flipY = true)
        {
			using (Stream s = Interface.StaticGetStreamFromPath (_mapPath)) {

				try {
					Map = _mapPath;

					Bitmap bitmap = new Bitmap (s);

					if (flipY)
						bitmap.RotateFlip (RotateFlipType.RotateNoneFlipY);

					BitmapData data = bitmap.LockBits (new System.Drawing.Rectangle (0, 0, bitmap.Width, bitmap.Height),
						ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

					createTexture (data.Scan0, data.Width, data.Height);

					bitmap.UnlockBits (data);

					GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
					GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

					GL.GenerateMipmap (GenerateMipmapTarget.Texture2D);

				} catch (Exception ex) {
					Debug.WriteLine ("Error loading texture: " + Map + ":" + ex.Message);
				}
			}
		}

		public Texture(int width, int height)
		{
			createTexture (IntPtr.Zero, width, height);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
		}

		void createTexture(IntPtr data, int width, int height)
		{
			Width = width;
			Height = height;

			GL.GenTextures(1, out texRef);
			GL.BindTexture(TextureTarget.Texture2D, texRef);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);			
		}       
			
        public static implicit operator int(Texture t)
        { 
            return t == null ? 0: t.texRef; 
        }
    }

}
