// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;

using System.Collections.Generic;

using Drawing2D;
using System.Globalization;
using Crow.DebugLogger;

namespace Crow
{
	/// <summary>
	/// store data and dimensions for resource sharing
	/// </summary>
	internal class sharedPicture {
		//TODO: restructure this whith clever conceptual classes
		public object Data;
		public Size Dims;
		public sharedPicture (object _data, Size _dims){
			Data = _data;
			Dims = _dims;
		}
	}
	/// <summary>
	/// virtual class for loading and drawing picture in the interface
	///
	/// Every loaded resources are stored in a dictonary with their path as key and shared
	/// among interface elements
	/// </summary>
	public abstract class Picture : Fill
	{
		/// <summary>
		/// path of the picture
		/// </summary>
		public string Path;
		/// <summary>
		/// unscaled dimensions fetched on loading
		/// </summary>
		public Size Dimensions { get; protected set; }
		/// <summary>
		/// if true and image has to be scalled, it will be scaled in both direction
		/// equaly
		/// </summary>
		public bool KeepProportions = false;
		/// <summary>
		/// allow or not the picture to be scalled on request by the painter
		/// </summary>
		public bool Scaled = true;

		#region CTOR
		/// <summary>
		/// Initializes a new instance of Picture.
		/// </summary>
		public Picture ()
		{
		}
		/// <summary>
		/// Initializes a new instance of Picture by loading the image pointed by the path argument
		/// </summary>
		/// <param name="path">image path, may be embedded</param>
		public Picture (string path)
		{
			Path = path;
		}
		#endregion

		void init(Interface iFace, ref Rectangle rect, out float widthRatio, out float heightRatio) {
			if (!IsLoaded)
				load (iFace);

			widthRatio = 1f;
			heightRatio = 1f;

			if (Scaled) {
				widthRatio = (float)rect.Width / Dimensions.Width;
				heightRatio = (float)rect.Height / Dimensions.Height;
				if (KeepProportions) {
					if (widthRatio < heightRatio)
						heightRatio = widthRatio;
					else
						widthRatio = heightRatio;
				}
			}
		}

		/// <summary>
		/// paint the picture in the rectangle given in arguments according
		/// to the Scale and keepProportion parameters.
		/// </summary>
		/// <param name="gr">drawing Backend context</param>
		/// <param name="bounds">bounds of the target surface to paint</param>
		/// <param name="subPart">limit rendering to this coma separated list of svg part identified with their svg 'id' attribute.</param>
		public void Paint (Interface iFace, IContext gr, Rectangle bounds, string subPart = "")
		{
			DbgLogger.AddEventWithMsg(DbgEvtType.Ressources, $"{Path}[Picture.Paint:]");

			init(iFace, ref bounds, out float widthRatio, out float heightRatio);

			gr.SaveTransformations ();

			gr.Translate (bounds.Left,bounds.Top);
			gr.Scale (widthRatio, heightRatio);
			gr.Translate (((float)bounds.Width/widthRatio - Dimensions.Width)/2f, ((float)bounds.Height/heightRatio - Dimensions.Height)/2f);

			Render(iFace, gr, subPart);

			gr.RestoreTransformations ();
		}
		public override void SetAsSource (Interface iFace, IContext ctx, Rectangle bounds = default(Rectangle))
		{
			DbgLogger.AddEventWithMsg(DbgEvtType.Ressources, $"{Path} [Picture.SetAsSource]");

			init(iFace, ref bounds, out float widthRatio, out float heightRatio);

			using (ISurface tmp = iFace.Backend.CreateSurface (bounds.Width, bounds.Height)) {
				using (IContext gr = iFace.Backend.CreateContext (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);

					Render(iFace, gr);
				}
				ctx.SetSource (tmp);
			}
		}		
		//final rendering on a scaled and configured context
		protected abstract void Render(Interface iFace, IContext gr, string subPart = "");
		public abstract bool IsLoaded { get; }
		public abstract void load (Interface iface);
		public abstract void LoadFromStream (Interface iface, Stream stream);
		#region Operators
		public static implicit operator Picture(string path) => Parse (path) as Picture;
		public static implicit operator string(Picture _pic) => _pic == null ? null : _pic.Path;
		#endregion

		public static new Picture Parse(string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;

			Picture _pic = null;

			DbgLogger.AddEventWithMsg(DbgEvtType.Ressources, $"Picture.parse:{path}");

			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture))
				_pic = new SvgPicture (path);
			else
				_pic = new BmpPicture (path);

			return _pic;
		}
		public override string ToString ()
		{
			return Path;
		}
	}
}

