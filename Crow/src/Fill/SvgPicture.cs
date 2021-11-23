// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;


using Drawing2D;

namespace Crow
{
	/// <summary>
	/// Derived from FILL for loading and drawing SVG images in the interface
	/// </summary>
	public class SvgPicture : Picture
	{
		SvgHandle hSVG;

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

		public override void load (Interface iFace)
		{
			if (iFace.sharedPictures.ContainsKey (Path)) {
				sharedPicture sp = iFace.sharedPictures [Path];
				hSVG = (SvgHandle)sp.Data;
				Dimensions = sp.Dims;
				return;
			}
			using (Stream stream = iFace.GetStreamFromPath (Path))
				load (iFace, stream);
			iFace.sharedPictures [Path] = new sharedPicture (hSVG, Dimensions);
		}
		void load (Interface iface, Stream stream) {
			using (BinaryReader sr = new BinaryReader (stream)) {
#if VKVG
				hSVG = new SvgHandle (iface.vkvgDevice, sr.ReadBytes ((int)stream.Length));
#else
				hSVG = new SvgHandle (sr.ReadBytes ((int)stream.Length));
#endif
				Dimensions = hSVG.Dimensions;
			}
		}
		internal static sharedPicture CreateSharedPicture (Interface iface, Stream stream) {
			SvgPicture pic = new SvgPicture ();
			pic.load (iface, stream);
			return new sharedPicture (pic.hSVG, pic.Dimensions);
		}

		public void LoadSvgFragment (Interface iface, string fragment) {			
#if VKVG
			hSVG = new SvgHandle (iface.vkvgDevice, System.Text.Encoding.Unicode.GetBytes(fragment));
#else
			hSVG = new SvgHandle (System.Text.Encoding.Unicode.GetBytes(fragment));
#endif
			Dimensions = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
		}

		#region implemented abstract members of Fill
		public override bool IsLoaded => hSVG != null;
		public override void SetAsSource (Interface iFace, IContext ctx, Rectangle bounds = default(Rectangle))
		{
			if (hSVG == null)
				load (iFace);

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

#if VKVG
			using (Surface tmp = new Surface (iFace.vkvgDevice, bounds.Width, bounds.Height)) {
#else
			using (Surface tmp = new ImageSurface (Format.Argb32, bounds.Width, bounds.Height)) {
#endif
				using (IContext gr = new Context (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);

					hSVG.Render (gr);
				}
				ctx.SetSource (tmp);
			}
		}
		#endregion

		/// <summary>
		/// paint the image in the rectangle given in arguments according
		/// to the Scale and keepProportion parameters.
		/// </summary>
		/// <param name="gr">drawing Backend context</param>
		/// <param name="rect">bounds of the target surface to paint</param>
		/// <param name="subPart">limit rendering to this coma separated list of svg part identified with their svg 'id' attribute.</param>
		public override void Paint (Interface iFace, IContext gr, Rectangle rect, string subPart = "")
		{
			if (hSVG == null)
				load (iFace);

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
				hSVG.Render (gr);
			else {
				string[] parts = subPart.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in parts)                
					hSVG.Render (gr, "#" + subPart);
			}
			
			gr.Restore ();
		}
	}
}

