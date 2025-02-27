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
		ISvgHandle hSVG;

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
			DbgLogger.AddEventWithMsg(DbgEvtType.Ressources, $"SVGPicture.load:{Path}");
			if (iFace.sharedPictures.ContainsKey (Path)) {
				sharedPicture sp = iFace.sharedPictures [Path];
				hSVG = (ISvgHandle)sp.Data;
				Dimensions = sp.Dims;
				return;
			}
			using (Stream stream = iFace.GetStreamFromPath (Path))
				hSVG = iFace.Backend.LoadSvg (stream);
			Dimensions = hSVG.Dimensions;
			iFace.sharedPictures [Path] = new sharedPicture (hSVG, Dimensions);
		}
		public override void LoadFromStream (Interface iFace, Stream stream) {
			DbgLogger.AddEventWithMsg(DbgEvtType.Ressources, $"SVGPicture.LoadFromStream:{Path}");
			hSVG = iFace.Backend.LoadSvg (stream);
			Dimensions = hSVG.Dimensions;
			iFace.sharedPictures [Path] = new sharedPicture (hSVG, Dimensions);			
		}
		public void LoadSvgFragment (Interface iface, string fragment) {
			DbgLogger.AddEventWithMsg(DbgEvtType.Ressources, $"SVGPicture.LoadSvgFragment:{fragment}");
			hSVG = iface.Backend.LoadSvg (fragment);
			Dimensions = hSVG.Dimensions;
		}

		#region implemented abstract members of Fill
		public override bool IsLoaded => hSVG != null;
		public override void SetAsSource (Interface iFace, IContext ctx, Rectangle bounds = default(Rectangle))
		{
			if (!IsLoaded)
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
			using (ISurface tmp = iFace.Backend.CreateSurface (bounds.Width, bounds.Height)) {
				using (IContext gr = iFace.Backend.CreateContext (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);

					hSVG.Render (gr);
				}
				ctx.SetSource (tmp);
			}
		}
		#endregion


        protected override void Render(Interface iFace, IContext gr, string subPart = "")
        {
			if (string.IsNullOrEmpty (subPart))
				hSVG.Render (gr);
			else {
				string[] parts = subPart.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in parts)
					hSVG.Render (gr, "#" + subPart);
			}
            
        }
    }
}

