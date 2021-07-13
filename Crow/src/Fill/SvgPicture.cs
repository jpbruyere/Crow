// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif

namespace Crow
{
	/// <summary>
	/// Derived from FILL for loading and drawing SVG images in the interface
	/// </summary>
	public class SvgPicture : Picture
	{
		Rsvg.Handle hSVG;

		#region CTOR
		/// <summary>
		/// Initializes a new instance of SvgPicture.
		/// </summary>
		public SvgPicture () {}
		/// <summary>
		/// Initializes a new instance of SvgPicture by loading the SVG file pointed by the path argument
		/// </summary>
		/// <param name="path">image path, may be embedded</param>
		public SvgPicture (string path) : base(path) {}
		#endregion

		bool load (Interface iFace)
		{
			if (string.IsNullOrEmpty(Path))
				return false;
			if (iFace.sharedPictures.ContainsKey (Path)) {
				sharedPicture sp = iFace.sharedPictures [Path];
				hSVG = (Rsvg.Handle)sp.Data;
				Dimensions = sp.Dims;
				return true;
			}
			using (Stream stream = iFace.GetStreamFromPath (Path))
				load (stream);
			iFace.sharedPictures [Path] = new sharedPicture (hSVG, Dimensions);
			return true;
		}
		void load (Stream stream) {
			using (BinaryReader sr = new BinaryReader (stream)) {
				hSVG = new Rsvg.Handle (sr.ReadBytes ((int)stream.Length));
				Dimensions = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
			}
		}
		internal static sharedPicture CreateSharedPicture (Stream stream) {
			SvgPicture pic = new SvgPicture ();
			pic.load (stream);
			return new sharedPicture (pic.hSVG, pic.Dimensions);
		}

		public void LoadSvgFragment (string fragment) {			
			hSVG = new Rsvg.Handle (System.Text.Encoding.Unicode.GetBytes(fragment));
			Dimensions = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
		}

		#region implemented abstract members of Fill

		public override void SetAsSource (Interface iFace, Context ctx, Rectangle bounds = default(Rectangle))
		{
			if (hSVG == null)
				if (!load (iFace))
					return;

			float widthRatio = 1f;
			float heightRatio = 1f;

			if (Scaled){
				widthRatio = (float)bounds.Width / Dimensions.Width;
				heightRatio = (float)bounds.Height / Dimensions.Height;
			}

			if (KeepProportions) {
				if (widthRatio < heightRatio)
					heightRatio = widthRatio;
				else
					widthRatio = heightRatio;
			}

			/*using (Surface tmp = new ImageSurface (Format.Argb32, bounds.Width, bounds.Height)) {
				using (Context gr = new Context (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);

					hSVG.RenderCairo (gr);
				}
				ctx.SetSource (tmp);
			}*/	
		}
		#endregion

		/// <summary>
		/// paint the image in the rectangle given in arguments according
		/// to the Scale and keepProportion parameters.
		/// </summary>
		/// <param name="gr">drawing Backend context</param>
		/// <param name="rect">bounds of the target surface to paint</param>
		/// <param name="subPart">limit rendering to this coma separated list of svg part identified with their svg 'id' attribute.</param>
		public override void Paint (Interface iFace, Context gr, Rectangle rect, string subPart = "")
		{
			if (hSVG == null)
				if (!load (iFace))
					return;

			float widthRatio = 1f;
			float heightRatio = 1f;

			if (Scaled) {
				widthRatio = (float)rect.Width / Dimensions.Width;
				heightRatio = (float)rect.Height / Dimensions.Height;
			}
			if (KeepProportions) {
				if (widthRatio < heightRatio)
					heightRatio = widthRatio;
				else
					widthRatio = heightRatio;
			}
				
			gr.Save ();

			gr.Translate (rect.Left,rect.Top);
			gr.Scale (widthRatio, heightRatio);
			gr.Translate (((float)rect.Width/widthRatio - Dimensions.Width)/2f, ((float)rect.Height/heightRatio - Dimensions.Height)/2f);

			if (string.IsNullOrEmpty (subPart))
				hSVG.RenderCairo (gr);
			else {
				string[] parts = subPart.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in parts)                
					hSVG.RenderCairoSub (gr, "#" + subPart);
			}
			
			gr.Restore ();			
		}
	}
}

