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
		public static bool DesignerMode = false;

		/// <summary>
		/// Graphic objects References use in dynamic delegates for binding
		/// </summary>
		public static List<object> References = new List<object>();

		public static LayoutingQueue LayoutingQueue = new LayoutingQueue();

		#region Load/Save

		internal static List<DynAttribute> EventsToResolve;
		internal static List<DynAttribute> Bindings;

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
		public static GraphicObject Load(string path)
		{
			string root = "Object";
			using (Stream s = new FileStream (path, FileMode.Open)) {
				using (XmlReader reader = XmlReader.Create (s)) {
					while (reader.Read()) {
						// first element is the root element
						if (reader.NodeType == XmlNodeType.Element) {
							root = reader.Name;
							break;
						}
					}
				}
			}
			Type t = Type.GetType("go." + root);
			//var go = Activator.CreateInstance(t);
			return Load(path, t);
		}
		public static void Load<T>(string file, out T result, object ClassContainingHandlers = null)
		{
			EventsToResolve = new List<DynAttribute>();
			Bindings = new List<DynAttribute> ();

			XmlSerializerNamespaces xn = new XmlSerializerNamespaces();
			xn.Add("", "");
			XmlSerializer xs = new XmlSerializer(typeof(T));            

			using (Stream s = new FileStream(file, FileMode.Open))
			{
				result = (T)xs.Deserialize(s);
			}

			if (ClassContainingHandlers == null)
				return;

			foreach (DynAttribute es in EventsToResolve)
			{
				if (string.IsNullOrEmpty(es.Value))
					continue;

				if (es.Value.StartsWith ("{")) {
					CompilerServices.CompileEventSource (es);
				} else {					
					MethodInfo mi = ClassContainingHandlers.GetType ().GetMethod (es.Value, BindingFlags.NonPublic | BindingFlags.Public
						| BindingFlags.Instance);

					if (mi == null) {
						Debug.WriteLine ("Handler Method not found: " + es.Value);
						continue;
					}

					FieldInfo fi = CompilerServices.getEventHandlerField (es.Source.GetType (), es.MemberName);
					Delegate del = Delegate.CreateDelegate(fi.FieldType, ClassContainingHandlers, mi);
					fi.SetValue(es.Source, del);
				}
			}

			foreach (DynAttribute binding in Bindings) {
//				Type tSource = binding.Source.GetType ();
//				if (!tSource.GetInterfaces ().Any (i => i.Name == "IValueChange")){
//					Debug.WriteLine ("Binding source does not implement IValueChange.");
//					continue;
//				}
				//MemberInfo mi = binding.Source.GetType ().GetMember (binding.MemberName);
				CompilerServices.CreateBinding (binding, ClassContainingHandlers);
			}
		}
		public static GraphicObject Load(string file, Type type, object ClassContainingHandlers = null)
		{
			GraphicObject result;
			EventsToResolve = new List<DynAttribute>();

			XmlSerializerNamespaces xn = new XmlSerializerNamespaces();
			xn.Add("", "");
			XmlSerializer xs = new XmlSerializer(type);            

			using (Stream s = new FileStream(file, FileMode.Open))
			{
				result = (GraphicObject)xs.Deserialize(s);
			}

			if (ClassContainingHandlers == null)
				return result;

			foreach (DynAttribute es in EventsToResolve)
			{
				if (string.IsNullOrEmpty(es.Value))
					continue;

				if (es.Value.StartsWith ("{")) {
					CompilerServices.CompileEventSource (es);
				} else {					
					MethodInfo mi = ClassContainingHandlers.GetType ().GetMethod (es.Value, BindingFlags.NonPublic | BindingFlags.Public
						| BindingFlags.Instance);

					if (mi == null) {
						Debug.WriteLine ("Handler Method not found: " + es.Value);
						continue;
					}

					FieldInfo fi = CompilerServices.getEventHandlerField (es.Source.GetType (), es.MemberName);
					Delegate del = Delegate.CreateDelegate(fi.FieldType, ClassContainingHandlers, mi);
					fi.SetValue(es.Source, del);
				}
			}
			return result;
		}

		#endregion
	}
}

