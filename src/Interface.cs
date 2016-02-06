//
//  Interface.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Reflection.Emit;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace Crow
{
	public static class Interface
	{
		/// <summary> Used to prevent spurious loading of templates </summary>
		internal static bool XmlSerializerInit = false;
		/// <summary> keep ressource path for debug msg </summary>
		internal static string CurrentGOMLPath = "";

		public static int TabSize = 4;
		public static string LineBreak = "\r\n";
		public static bool FocusOnHover = false;
		/// <summary> Time to wait in millisecond before starting repeat loop</summary>
		public static int DeviceRepeatDelay = 600;
		/// <summary> Time interval in millisecond between device event repeat</summary>
		public static int DeviceRepeatInterval = 100;
		public static bool ReplaceTabsWithSpace = false;
		/// <summary> Allow rendering of interface in development environment </summary>
		public static bool DesignerMode = false;
		/// <summary> Threshold to catch borders for sizing </summary>
		public static int BorderThreshold = 5;

		public static LayoutingQueue LayoutingQueue = new LayoutingQueue ();
		public static Queue<GraphicObject> RegisteredGOForLayouting = new Queue<GraphicObject>();

		#region Load/Save
		//internal static List<DynAttribute> Bindings;


		public static void Save<T> (string file, T graphicObject)
		{            
			XmlSerializerNamespaces xn = new XmlSerializerNamespaces ();
			xn.Add ("", "");
			XmlSerializer xs = new XmlSerializer (typeof(T));

			xs = new XmlSerializer (typeof(T));
			using (Stream s = new FileStream (file, FileMode.Create)) {
				xs.Serialize (s, graphicObject, xn);
			}
		}

		public static Stream GetStreamFromPath (string path)
		{
			Stream stream = null;

			if (path.StartsWith ("#")) {
				string resId = path.Substring (1);
				//try/catch added to prevent nunit error
				try {
					stream = System.Reflection.Assembly.GetEntryAssembly ().GetManifestResourceStream (resId);
				} catch{}
				if (stream == null)//try to find ressource in Crow assembly				
					stream = System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceStream (resId);
				if (stream == null)
					throw new Exception ("Resource not found: " + path);
			} else {
				if (!File.Exists (path))
					throw new FileNotFoundException ("File not found: ", path);
				stream = new FileStream (path, FileMode.Open, FileAccess.Read);
			}
			return stream;
		}

		/// <summary>
		/// Pre-read first node to set GraphicObject class for loading
		/// and reset stream position to 0
		/// </summary>
		public static Type GetTopContainerOfGOMLStream (Stream stream)
		{
			string root = "Object";
			stream.Seek (0, SeekOrigin.Begin);
			using (XmlReader reader = XmlReader.Create (stream)) {
				while (reader.Read ()) {
					// first element is the root element
					if (reader.NodeType == XmlNodeType.Element) {
						root = reader.Name;
						break;
					}
				}
			}

			Type t = Type.GetType ("Crow." + root);

			stream.Seek (0, SeekOrigin.Begin);
			return t;
		}


		public static GraphicObject Load (string path, object hostClass = null)
		{
			CurrentGOMLPath = path;
			using (Stream stream = GetStreamFromPath (path)) {
				return Load(stream, GetTopContainerOfGOMLStream(stream), hostClass);
			}
		}



		public static GraphicObject Load (Stream stream, Type type, object hostClass = null)
		{
			#if DEBUG_LOAD_TIME
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif

			GraphicObject result;


			XmlSerializerNamespaces xn = new XmlSerializerNamespaces ();
			xn.Add ("", "");

			XmlSerializerInit = true;
			XmlSerializer xs = new XmlSerializer (type);
			XmlSerializerInit = false;

			result = (GraphicObject)xs.Deserialize (stream);
			//result.DataSource = hostClass;

			#if DEBUG_LOAD_TIME
			FileStream fs = stream as FileStream;
			if (fs!=null)
				CurrentGOMLPath = fs.Name;
			loadingTime.Stop ();
			Debug.WriteLine ("GOML Loading ({2}->{3}): {0} ticks, {1} ms",
				loadingTime.ElapsedTicks,
				loadingTime.ElapsedMilliseconds,
			CurrentGOMLPath, result.ToString());
			#endif

			return result;
		}


		#endregion
	}
}

