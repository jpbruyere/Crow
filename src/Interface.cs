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

namespace go
{
	public static class Interface
	{
		public static int TabSize = 4;
		public static string LineBreak = "\r\n";
		public static bool ReplaceTabsWithSpace = false;
		/// <summary> Allow rendering of interface in development environment </summary>
		public static bool DesignerMode = false;
		/// <summary> Threshold to catch borders for sizing </summary>
		public static int BorderThreshold = 5;

		/// <summary>
		/// Graphic objects References use in dynamic delegates for binding
		/// </summary>
		public static List<object> References = new List<object>();

		public static LayoutingQueue LayoutingQueue = new LayoutingQueue();

		#region Load/Save

		internal static Stack<List<DynAttribute>> GOMLResolutionStack = new Stack<List<DynAttribute>>();
		internal static List<DynAttribute> GOMLResolver
		{
			get { return GOMLResolutionStack.Peek ();}
		}
		//internal static List<DynAttribute> Bindings;


		public static void Save<T>(string file, T graphicObject)
		{            
			XmlSerializerNamespaces xn = new XmlSerializerNamespaces();
			xn.Add("", "");
			XmlSerializer xs = new XmlSerializer(typeof(T));

			xs = new XmlSerializer(typeof(T));
			using (Stream s = new FileStream(file, FileMode.Create))
			{
				xs.Serialize(s, graphicObject, xn);
			}
		}

		public static Stream GetStreamFromPath(string path)
		{
			Stream stream = null;

			if (path.StartsWith ("#")) {
				string resId = path.Substring (1);
				stream = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(resId);
				if (stream == null)//try to find ressource in golib assembly				
					stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resId);
				if (stream == null)
					throw new Exception ("Resource not found: " + path);
			} else {
				if (!File.Exists (path))
					throw new FileNotFoundException ("File not found: ", path);
				stream = new FileStream (path, FileMode.Open, FileAccess.Read);
			}
			return stream;
		}

		public static GraphicObject Load(string path, object hostClass = null)
		{
			string root = "Object";

			using (Stream stream = GetStreamFromPath (path)) {

				#region Pre-read first node to set GraphicObject class for loading
				using (XmlReader reader = XmlReader.Create (stream)) {
					while (reader.Read()) {
						// first element is the root element
						if (reader.NodeType == XmlNodeType.Element) {
							root = reader.Name;
							break;
						}
					}
				}

				Type t = Type.GetType("go." + root);
				//var go = Activator.CreateInstance(t);
				stream.Seek(0,SeekOrigin.Begin);
				#endregion

				return Load(stream, t, hostClass);
			}
		}
		static GraphicObject Load(Stream stream, Type type, object hostClass = null)
		{
			GraphicObject result;
			GOMLResolutionStack.Push(new List<DynAttribute>());

			XmlSerializerNamespaces xn = new XmlSerializerNamespaces();
			xn.Add("", "");
			XmlSerializer xs = new XmlSerializer(type);            

			result = (GraphicObject)xs.Deserialize(stream);
			result.DataSource = hostClass;

			if (hostClass == null) {
				GOMLResolutionStack.Pop ();
				return result;
			}

			resolveGOML (hostClass);

//			while (Bindings.Count > 0) {
//				DynAttribute binding = Bindings [0];
//				Bindings.RemoveAt (0);
//				CompilerServices.ResolveBinding (binding, hostClass);
//			}

//			foreach (DynAttribute binding in Bindings) {
//				//				Type tSource = binding.Source.GetType ();
//				//				if (!tSource.GetInterfaces ().Any (i => i.Name == "IValueChange")){
//				//					Debug.WriteLine ("Binding source does not implement IValueChange.");
//				//					continue;
//				//				}
//				//MemberInfo mi = binding.Source.GetType ().GetMember (binding.MemberName);
//				CompilerServices.CreateBinding (binding, hostClass);
//			}
//			Bindings.Clear ();


			return result;
		}

		static void resolveGOML(object hostClass)
		{
			foreach (DynAttribute es in GOMLResolver)
			{
				if (string.IsNullOrEmpty(es.Value))
					continue;

				Type dstType = es.Source.GetType ();
				MemberInfo miTarget = dstType.GetMember (es.MemberName).FirstOrDefault();

				if (miTarget == null) {
					Debug.WriteLine ("'{0}' Member not found in '{1}' type.", es.MemberName, dstType.ToString ());
					continue;
				}
				
				if (miTarget.MemberType == MemberTypes.Event) {
					if (es.Value.StartsWith ("{")) {
						CompilerServices.CompileEventSource (es);
					} else {					
						MethodInfo mi = hostClass.GetType ().GetMethod (es.Value, BindingFlags.NonPublic | BindingFlags.Public
						                | BindingFlags.Instance);

						object effectiveHostClass = hostClass;
						if (mi == null) {
							//TODO: hack to have it work, hostClass and dataSource should be clearly separated
							mi = OpenTKGameWindow.currentWindow.GetType ().GetMethod (es.Value, BindingFlags.NonPublic | BindingFlags.Public
								| BindingFlags.Instance);
							if (mi == null) {
								Debug.WriteLine ("Handler Method not found: " + es.Value);
								continue;
							}
							effectiveHostClass = OpenTKGameWindow.currentWindow;
						}

						EventInfo ei = es.Source.GetType ().GetEvent (es.MemberName);
						MethodInfo addHandler = ei.GetAddMethod ();
						Delegate del = Delegate.CreateDelegate (ei.EventHandlerType, effectiveHostClass, mi);


						addHandler.Invoke(es.Source, new object[] {del});

					}
				} else {
					CompilerServices.ResolveBinding (es, hostClass);
				}
			}
			GOMLResolutionStack.Pop();			
		}
		#endregion
	}
}

