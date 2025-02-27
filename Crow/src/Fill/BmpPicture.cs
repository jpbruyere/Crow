// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Runtime.InteropServices;


using Drawing2D;

namespace Crow
{

	/// <summary>
	/// Derived from FILL for loading and drawing bitmaps in the interface
	/// </summary>
	public class BmpPicture : Picture {
		byte[] image = null;

		#region CTOR
		/// <summary>
		/// Initializes a new instance of BmpPicture.
		/// </summary>
		public BmpPicture () { }
		/// <summary>
		/// Initializes a new instance of BmpPicture by loading the image pointed by the path argument
		/// </summary>
		/// <param name="path">image path, may be embedded</param>
		public BmpPicture (string path) : base (path) { }
		#endregion
		/// <summary>
		/// load the image for rendering from the path given as argument
		/// </summary>
		public override void load (Interface iFace) {
			DbgLogger.AddEventWithMsg(DbgEvtType.Ressources, $"BMPPicture.load:{Path}");
			if (iFace.sharedPictures.ContainsKey (Path)) {
				sharedPicture sp = iFace.sharedPictures[Path];
				image = (byte[])sp.Data;
				Dimensions = sp.Dims;
				return;
			}
			using (Stream stream = iFace.GetStreamFromPath (Path)) {
				image = iFace.Backend.LoadBitmap (stream, out Size dimensions);
				Dimensions = dimensions;
				iFace.sharedPictures[Path] = new sharedPicture (image, Dimensions);
			}
		}
		public override void LoadFromStream (Interface iFace, Stream stream) {
			DbgLogger.AddEventWithMsg(DbgEvtType.Ressources, $"BMPPicture.LoadFromStream:{Path}");
			image = iFace.Backend.LoadBitmap (stream, out Size dimensions);
			Dimensions = dimensions;
			iFace.sharedPictures[Path] = new sharedPicture (image, Dimensions);
		}

		#region implemented abstract members of Fill
		public override bool IsLoaded => image != null;
		#endregion

        protected override void Render(Interface iFace, IContext gr, string subPart = "")
        {
			using (ISurface imgSurf = iFace.Backend.CreateSurface (image, Dimensions.Width, Dimensions.Height)) {
				gr.SetSource (imgSurf, 0,0);
				gr.Paint ();
			}
		}		
	}
}

