//
//  Instantiator.cs
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
using System.Threading;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Crow.IML
{
	public delegate void InstanciatorInvoker(object instance, Interface iface);

	/// <summary>
	/// Instantiators
	/// </summary>
	public class Instantiator
	{
		public Type RootType;
		InstanciatorInvoker loader;

		#region CTOR
		public Instantiator (string path) : this (Interface.GetStreamFromPath(path)) {}

		public Instantiator (Stream stream)
		{
#if DEBUG_LOAD
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
#endif
			using (XmlTextReader itr = new XmlTextReader (stream)) {
				parseIML (itr);
			}
#if DEBUG_LOAD
			loadingTime.Stop ();
			Debug.WriteLine ("IML Instantiator creation '{2}' : {0} ticks, {1} ms",
				loadingTime.ElapsedTicks, loadingTime.ElapsedMilliseconds, imlPath);
#endif
		}
		public Instantiator (Type _root, InstanciatorInvoker _loader)
		{
			RootType = _root;
			loader = _loader;
		}
		public static Instantiator CreateFromImlFragment (string fragment)
		{
			try {
				using (Stream s = new MemoryStream (Encoding.UTF8.GetBytes (fragment))) {
					return new Instantiator (s);
				}
			} catch (Exception ex) {
				throw new Exception ("Error loading fragment:\n" + fragment + "\n", ex);
			}
		}
		#endregion

		/// <summary>
		/// Parses IML and build a dynamic method that will be used to instanciate one or multiple occurence of the IML file or fragment
		/// </summary>
		void parseIML (XmlTextReader reader) {
			Context ctx = new Context (findRootType (reader));

			ctx.CurrentNode = new Node (ctx.RootType);
			emitLoader (reader, ctx);

			reader.Read ();//close tag
		}
		/// <summary>
		/// read first node to set GraphicObject class for loading
		/// and let reader position on that node
		/// </summary>
		Type findRootType (XmlTextReader reader)
		{
			string root = "Object";
			while (reader.Read ()) {
				if (reader.NodeType == XmlNodeType.Element) {
					root = reader.Name;
					break;
				}
			}

			Type t = Type.GetType ("Crow." + root);
			if (t == null) {
				Assembly a = Assembly.GetEntryAssembly ();
				foreach (Type expT in a.GetExportedTypes ()) {
					if (expT.Name == root)
						t = expT;
				}
			}
			return t;
		}
		void emitLoader (XmlTextReader reader, Context ctx)
		{
			string tmpXml = reader.ReadOuterXml ();

			ctx.il.Emit (OpCodes.Ldloc_0);//save current go onto the stack if child has to be added

			if (ctx.CurrentNode.IsTemplate)
				emitTemplateLoad (ctx, tmpXml);

			emitGOLoad (ctx, tmpXml);

			ctx.il.Emit (OpCodes.Pop);//pop saved ref to current object
		}
		void emitTemplateLoad (Context ctx, string tmpXml) {
			//if its a template, first read template elements
			using (XmlTextReader reader = new XmlTextReader (tmpXml)) {
				List<string []> itemTemplateIds = new List<string []> ();
				bool inlineTemplate = false;

				string templatePath = reader.GetAttribute ("Template");

				reader.Read ();
				int depth = reader.Depth + 1;
				while (reader.Read ()) {
					if (!reader.IsStartElement () || reader.Depth > depth)
						continue;
					if (reader.Name == "Template") {
						inlineTemplate = true;
						reader.Read ();
						ctx.nodesStack.Push (ctx.CurrentNode);
						readChildren (reader, ctx);
						ctx.nodesStack.Pop ();
					} else if (reader.Name == "ItemTemplate") {
						string dataType = "default", datas = "", path = "";
						while (reader.MoveToNextAttribute ()) {
							if (reader.Name == "DataType")
								dataType = reader.Value;
							else if (reader.Name == "Data")
								datas = reader.Value;
							else if (reader.Name == "Path")
								path = reader.Value;
						}
						reader.MoveToElement ();

						string itemTmpID;

						if (string.IsNullOrEmpty (path)) {
							itemTmpID = Guid.NewGuid ().ToString ();
							Interface.Instantiators [itemTmpID] =
								new ItemTemplate (reader.ReadInnerXml (), dataType, datas);

						} else {
							if (!reader.IsEmptyElement)
								throw new Exception ("ItemTemplate with Path attribute may not include sub nodes");
							itemTmpID = path;
							Interface.Instantiators [itemTmpID] =
										 new ItemTemplate (Interface.GetStreamFromPath (itemTmpID), dataType, datas);
						}
						itemTemplateIds.Add (new string [] { dataType, itemTmpID, datas });
					}
				}

				if (!inlineTemplate) {//load from path or default template
					ctx.il.Emit (OpCodes.Ldloc_0);//Load  current templatedControl ref
					if (string.IsNullOrEmpty (templatePath)) {
						ctx.il.Emit (OpCodes.Ldnull);//default template loading
					} else {
						ctx.il.Emit (OpCodes.Ldarg_1);//load currentInterface
						ctx.il.Emit (OpCodes.Ldstr, templatePath); //Load template path string
						ctx.il.Emit (OpCodes.Callvirt,//call Interface.Load(path)
							CompilerServices.miIFaceLoad);
					}
					ctx.il.Emit (OpCodes.Callvirt, CompilerServices.miLoadTmp);//load template
				}
				//copy item templates (review this)
				foreach (string [] iTempId in itemTemplateIds) {
					ctx.il.Emit (OpCodes.Ldloc_0);//load TempControl ref
					ctx.il.Emit (OpCodes.Ldfld, CompilerServices.fldItemTemplates);//load ItemTemplates dic field
					ctx.il.Emit (OpCodes.Ldstr, iTempId [0]);//load key
					ctx.il.Emit (OpCodes.Ldstr, iTempId [1]);//load value
					ctx.il.Emit (OpCodes.Callvirt, CompilerServices.miGetITemp);
					ctx.il.Emit (OpCodes.Callvirt, CompilerServices.miAddITemp);

					if (!string.IsNullOrEmpty (iTempId [2])) {
						//expand delegate creation
						ctx.il.Emit (OpCodes.Ldloc_0);//load TempControl ref
						ctx.il.Emit (OpCodes.Ldfld, CompilerServices.fldItemTemplates);
						ctx.il.Emit (OpCodes.Ldstr, iTempId [0]);//load key
						ctx.il.Emit (OpCodes.Callvirt, CompilerServices.miGetITempFromDic);
						ctx.il.Emit (OpCodes.Ldloc_0);//load root of treeView
						ctx.il.Emit (OpCodes.Callvirt, CompilerServices.miCreateExpDel);
					}
				}
			}
			ctx.CurrentNode.Index++;
		}

		void emitGOLoad (Context ctx, string tmpXml) {
			using (XmlTextReader reader = new XmlTextReader (tmpXml)) {
				reader.Read ();

				#region Styling and default values loading
				if (reader.HasAttributes) {
					string style = reader.GetAttribute ("Style");
					if (!string.IsNullOrEmpty (style))
						CompilerServices.EmitSetValue (ctx.il, CompilerServices.piStyle, style);
				}
				ctx.il.Emit (OpCodes.Ldloc_0);
				ctx.il.Emit (OpCodes.Callvirt, CompilerServices.miLoadDefaultVals);
				#endregion

				#region Attributes reading
				if (reader.HasAttributes) {

					while (reader.MoveToNextAttribute ()) {
						if (reader.Name == "Style")
							continue;

						MemberInfo mi = ctx.CurrentNode.CrowType.GetMember (reader.Name).FirstOrDefault ();
						if (mi == null)
							throw new Exception ("Member '" + reader.Name + "' not found in " + ctx.CurrentNode.CrowType.Name);
						if (mi.MemberType == MemberTypes.Event) {
							CompilerServices.emitBindingCreation (ctx.il, reader.Name, reader.Value);
							continue;
						}
						PropertyInfo pi = mi as PropertyInfo;
						if (pi == null)
							throw new Exception ("Member '" + reader.Name + "' not found in " + ctx.CurrentNode.CrowType.Name);

						if (pi.Name == "Name")
							ctx.Names.Add (reader.Value, Node.AddressToString (ctx.nodesStack.ToArray ()));

						if (reader.Value.StartsWith ("{", StringComparison.OrdinalIgnoreCase)) {
							readPropertyBinding (reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));

							//CompilerServices.emitBindingCreation (reader.il, reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));
						} else
							CompilerServices.EmitSetValue (ctx.il, pi, reader.Value);

					}
					reader.MoveToElement ();
				}
				#endregion

				if (reader.IsEmptyElement) {
					ctx.il.Emit (OpCodes.Pop);//pop saved ref to current object
					return;
				}
				ctx.nodesStack.Push (ctx.CurrentNode);
				readChildren (reader, ctx);
				ctx.nodesStack.Pop ();
			}
		}
		/// <summary>
		/// Parse child node an generate corresponding msil
		/// </summary>
		void readChildren (XmlTextReader reader, Context ctx)
		{
			bool endTagReached = false;
			while (reader.Read ()) {
				switch (reader.NodeType) {
				case XmlNodeType.EndElement:
					endTagReached = true;
					break;
				case XmlNodeType.Element:
					//skip Templates
					if (reader.Name == "Template" ||
						reader.Name == "ItemTemplate") {
						reader.Skip ();
						continue;
					}

					//push current instance on stack for parenting
					//loc_0 will be used for child
					ctx.il.Emit (OpCodes.Ldloc_0);

					Type t = Type.GetType ("Crow." + reader.Name);
					if (t == null) {
						Assembly a = Assembly.GetEntryAssembly ();
						foreach (Type expT in a.GetExportedTypes ()) {
							if (expT.Name == reader.Name)
								t = expT;
						}
					}
					if (t == null)
						throw new Exception (reader.Name + " type not found");

					ctx.il.Emit (OpCodes.Newobj, t.GetConstructors () [0]);//TODO:search parameterless ctor
					ctx.il.Emit (OpCodes.Stloc_0);//child is now loc_0
					CompilerServices.emitSetCurInterface (ctx.il);

					ctx.CurrentNode = new Node (t);
					emitLoader (reader, ctx);

					ctx.il.Emit (OpCodes.Ldloc_0);//load child on stack for parenting
					ctx.il.Emit (OpCodes.Callvirt, ctx.nodesStack.Peek ().AddMethod);
					ctx.il.Emit (OpCodes.Stloc_0); //reset local to current go
					ctx.il.Emit (OpCodes.Ldloc_0);//save current go onto the stack if child has to be added

					ctx.nodesStack.Peek ().Index++;

					break;
				}
				if (endTagReached)
					break;
			}
		}


		public GraphicObject CreateInstance(Interface iface){
			GraphicObject tmp = (GraphicObject)Activator.CreateInstance(RootType);
			loader (tmp, iface);
			return tmp;
		}
		//public string GetImlSourcesCode(){
		//	try {
		//		using (StreamReader sr = new StreamReader (imlPath))
		//			return sr.ReadToEnd();
		//	} catch (Exception ex) {
		//		throw new Exception ("Error getting sources for <" + imlPath + ">:", ex);
		//	}
		//}
	}
}

