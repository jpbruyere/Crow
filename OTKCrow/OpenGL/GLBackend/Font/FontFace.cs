using System;
using System.Collections.Generic;
using System.IO;
using SharpFont;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL;

namespace Crow.GLBackend
{
	public class FontFace
	{
		public static int testTexture;

		static List<FontFace> fonts = new List<FontFace>();
		public Dictionary<uint,Dictionary<uint, glGlyph>> glyphesCache = new Dictionary<uint, Dictionary<uint, glGlyph>>();

		#region CTOR
		public FontFace ()
		{
		}
		#endregion

		string _name = "droid";
		string _family;
		string _fontPath;
		public FontExtents originalFontExtents;

		FontStyle _style = FontStyle.Normal;
		FontFlag _flags = FontFlag.None;

		public string Name {
			get { return _name; }
			set { _name = value; }
		}
		public string Family {
			get {return _family;}
			set {_family = value;}
		}
		public string FontPath {
			get { return _fontPath; }
			set { _fontPath = value; }
		}
			
		public FontStyle Style {
			get { return _style; }
			set { _style = value; }
		}
		public FontFlag Flags {
			get { return _flags; }
			set { _flags = value; }
		}

		public FontSlant Slant {
			get{ 
				if ((Style & FontStyle.Italic) == FontStyle.Italic)
					return FontSlant.Italic;
				if ((Style & FontStyle.Oblique) == FontStyle.Oblique)
					return FontSlant.Oblique;
				return FontSlant.Normal;
			}
		}
		public FontWeight Wheight {
			get{ return (Style & FontStyle.Bold) == FontStyle.Bold ? FontWeight.Bold : FontWeight.Normal; }
		}

		public static FontFace SearchFont(Font font)
		{
			FontFace[] tmp = fonts.Where (f =>
				(string.Compare(f.Name,font.Name,true)==0 && f.Style == font.Style)).ToArray();

			tmp = tmp.Where (f => (f.Flags & font.Flags) == font.Flags).OrderBy(f=>f.Flags).ToArray();

			return tmp.FirstOrDefault();
		}

		public static void BuildFontsList(string _path)
		{
			DirectoryInfo dir = new DirectoryInfo (_path);
			try
			{
				using (Library lib = new Library())
				{
					foreach (FileInfo f in dir.GetFiles("*.ttf",SearchOption.AllDirectories)) {
						try {

							Face face = new Face(lib,f.FullName, 0);
							FontFace font = new FontFace();

							//TODO:test if font is scalable, if not=>continue
							font.Family = face.FamilyName;
							font.FontPath = f.FullName;
											
							//check if no particularities hidde in family name
							font.Name = "";
							string[] styles = face.FamilyName.Split(' ');
							for (int i = 0; i < styles.Length; i++) {
								FontFlag fl;
								FontStyle fs;
								if (Enum.TryParse<FontFlag> (styles [i], true, out fl))
									font.Flags |= fl;
								else if (Enum.TryParse<FontStyle> (styles [i], true, out fs))
									font.Style |= fs;
								else//add only unknown word to create shortest possible name
									font.Name += styles[i] + " ";
							}
							font.Name = font.Name.Trim();

							styles = face.StyleName.Split(' ');
							for (int i = 0; i < styles.Length; i++) {
								switch (styles[i].ToLowerInvariant()) {
								case "regular":
								case "normal":
									break;
								case "bold":
									font.Style |= FontStyle.Bold;
									break;
								case "italic":
									font.Style |= FontStyle.Italic;
									break;
								case "oblique":
									font.Style |= FontStyle.Oblique;
									break;
								default:
									FontFlag fl;
									if (Enum.TryParse<FontFlag>(styles[i], true, out fl))
										font.Flags |= fl;
									break;
								}

							}								
							fonts.Add(font);
							face.Dispose();
						}
						catch (FreeTypeException ee)
						{ Console.Write(ee.Error.ToString() + ": " + f.FullName); }
					}
				}
			}
			catch (FreeTypeException e)
			{ Console.Write(e.Error.ToString()); }
		}

		const int FONT_TEX_SIZE = 1024;

		public void buildGlyphesTextures(uint activeSize, uint startChar, uint endChar){
			try
			{
				using (Library lib = new Library())
				{
					Face face = new Face(lib,_fontPath, 0);

					face.SetPixelSizes(0,activeSize);

					//face.SetCharSize(0, 32 * 64, 0, 96);

					originalFontExtents = new FontExtents();
					originalFontExtents.Ascent = face.Size.Metrics.Ascender >> 6;
					originalFontExtents.Descent = face.Size.Metrics.Descender >> 6;
					originalFontExtents.Height = face.Size.Metrics.Height >> 6;
					originalFontExtents.MaxXAdvance = face.Size.Metrics.MaxAdvance >> 6;
					originalFontExtents.MaxYAdvance = face.MaxAdvanceHeight >> 6;

					if (!glyphesCache.ContainsKey(activeSize))
						glyphesCache [activeSize] = new Dictionary<uint, glGlyph>();

					buildTexture(ref face,activeSize,startChar,endChar);

					face.Dispose();
				}
			}
			catch (FreeTypeException e)
			{ Console.Write(e.Error.ToString()); }
		}
			
		void setTextureData(int tex, byte[] _buffer)
		{
			GL.BindTexture(TextureTarget.Texture2D, tex);
			GL.TexImage2D(TextureTarget.Texture2D,0,
				PixelInternalFormat.R8, FONT_TEX_SIZE, FONT_TEX_SIZE,0,
				OpenTK.Graphics.OpenGL.PixelFormat.Red, PixelType.UnsignedByte,_buffer);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.BindTexture(TextureTarget.Texture2D, 0);

			byte[] pixels = new byte[FONT_TEX_SIZE * FONT_TEX_SIZE * 4];

			for (int x = 0; x < FONT_TEX_SIZE; x++)
			{
				for (int y = 0; y < FONT_TEX_SIZE; y++)
				{
					byte v = _buffer[x + y * FONT_TEX_SIZE];

					pixels[(x + y * FONT_TEX_SIZE) * 4] = v;
					pixels[(x + y * FONT_TEX_SIZE) * 4 + 1] = v;
					pixels[(x + y * FONT_TEX_SIZE) * 4 + 2] = v;
					pixels[(x + y * FONT_TEX_SIZE) * 4 + 3] = 255;
				}
			}
			System.Drawing.Bitmap bmp;
			unsafe
			{
				bmp = new System.Drawing.Bitmap(FONT_TEX_SIZE, FONT_TEX_SIZE, System.Drawing.Imaging.PixelFormat.Format32bppArgb);//, ptr);
				System.Drawing.Imaging.BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, FONT_TEX_SIZE, FONT_TEX_SIZE),
					System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, FONT_TEX_SIZE * FONT_TEX_SIZE * 4);

				bmp.UnlockBits(data);
			}

			bmp.Save(@"/home/jp/fonttex.png");


		}


		void buildTexture(ref Face face,uint activeSize, uint startChar, uint endChar){
			int texPage = GL.GenTexture ();
			byte[] buffer = new byte[FONT_TEX_SIZE*FONT_TEX_SIZE];

			int penX = 0,
				penY = 0;

			int maxBmpHeight = 0;

			for(uint c=startChar;c<endChar;c++)
			{
				uint glyphIndex = face.GetCharIndex((uint)c);
				if (glyphIndex==0)
					continue;
				//face.LoadChar((uint)c, LoadFlags.Render, LoadTarget.Normal);
				face.LoadGlyph(glyphIndex, LoadFlags.Render, LoadTarget.Normal);
				testTexture = texPage;

				GlyphSlot slot = face.Glyph;
				FTBitmap bmp = slot.Bitmap;

				if (penX+bmp.Width > FONT_TEX_SIZE){
					penX = 0;
					penY += maxBmpHeight;
					maxBmpHeight = 0;
				}

				if (penY+bmp.Rows > FONT_TEX_SIZE)
				{
					//glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
					setTextureData (texPage, buffer);
					texPage = GL.GenTexture ();
					buffer = new byte[FONT_TEX_SIZE*FONT_TEX_SIZE];
					penX = 0;
					penY = 0;
					maxBmpHeight = 0;
				}

				Rectangle tmp = new Rectangle (
					penX,
					penY,
					bmp.Width, bmp.Rows);

				for(int y=0; y<bmp.Rows; y++)
				{
					for(int x=0; x<bmp.Width; x++)
						buffer[ penX + x + (penY + y)* FONT_TEX_SIZE ] =
							bmp.BufferData[x + y * bmp.Width];
				}
				glyphesCache [activeSize][c] = new glGlyph() 
				{
					texId = texPage,
					texX = (float)penX / FONT_TEX_SIZE,
					texY = (float)penY / FONT_TEX_SIZE,
					texWidth = (float)(bmp.Width) / FONT_TEX_SIZE,
					texHeight = (float)(bmp.Rows)  / FONT_TEX_SIZE,

//					texCoord = new Rectangle(
//						penX / FONT_TEX_SIZE,
//						penY / FONT_TEX_SIZE,
//						(bmp.Width) / FONT_TEX_SIZE,
//						(bmp.Rows)  / FONT_TEX_SIZE),
					dims = new Size(bmp.Width , bmp.Rows),
					bmpLeft = slot.BitmapLeft, bmpTop = slot.BitmapTop, advanceX = slot.Advance.X>>6,advanceY = slot.Advance.Y>>6
				};

				if (bmp.Rows>maxBmpHeight)
					maxBmpHeight=bmp.Rows;

				penX += bmp.Width;
			}

			if (penX>0 || penY>0)
				setTextureData(texPage, buffer);
		}

		public override string ToString()
		{
			string tmp = Name;
			if (Style != FontStyle.Normal)
				tmp += "," + Style.ToString ();
			if (Flags != FontFlag.None)
				tmp += "," + Flags.ToString ();
			return tmp;
		}
	}
}

