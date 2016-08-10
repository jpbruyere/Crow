//
//  IMLReader.cs
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
using System.Xml;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;

namespace Crow
{
	public class IMLReader : XmlTextReader
	{
		Interface.LoaderInvoker loader = null;

		public string ImlPath;
		public Stream ImlStream;
		public Type RootType = null;

		DynamicMethod dm = null;
		public ILGenerator il = null;

		#region CTOR
		public IMLReader (string path)
			: this(Interface.GetStreamFromPath (path)){
			ImlPath = path;
		}
		public IMLReader (Stream stream)
			: base(stream)
		{
			ImlStream = stream;
			createInstantiator ();
		}
		/// <summary>
		/// Used to parse xmlFrament with same code generator linked
		/// If ilGen=null, a new Code Generator will be created.
		/// </summary>
		public IMLReader (ILGenerator ilGen, string xmlFragment)
			: base(xmlFragment, XmlNodeType.Element,null){
			il = ilGen;

			if (il != null)
				return;

			createInstantiator();
		}
		#endregion

		void createInstantiator(){
			readRootType();
			InitEmitter();
			emitLoader(RootType);
			Read();//close tag
		}
		/// <summary>
		/// Finalize instatiator MSIL and return LoaderInvoker delegate
		/// </summary>
		public Interface.LoaderInvoker GetLoader(){
			if (loader != null)
				return loader;

			il.Emit(OpCodes.Ret);
			loader = (Interface.LoaderInvoker)dm.CreateDelegate (typeof(Interface.LoaderInvoker));
			return loader;
		}
		/// <summary>
		/// Inits il generator, RootType must have been read first
		/// </summary>
		void InitEmitter(){

			dm = new DynamicMethod("dyn_instantiator",
				MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
				CallingConventions.Standard,
				typeof(void),new Type[] {typeof(object)}, RootType, true);

			il = dm.GetILGenerator(256);
			il.DeclareLocal(typeof(GraphicObject));
			il.Emit(OpCodes.Nop);
			//set local GraphicObject to root object passed as 1st argument
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Stloc_0);
		}
		void emitLoader(Type crowType){
			string tmpXml = ReadOuterXml ();

			il.Emit (OpCodes.Ldloc_0);//save current go onto the stack if child has to be added

			if (typeof(TemplatedControl).IsAssignableFrom (crowType)) {
				//if its a template, first read template elements
				using (IMLReader reader = new IMLReader (il, tmpXml)) {

					string templatePath = reader.GetAttribute ("Template");
					//string itemTemplatePath = reader.GetAttribute ("ItemTemplate");

					bool inlineTemplate = false;
					reader.Read ();

					while (reader.Read ()) {
						if (!reader.IsStartElement ())
							continue;
						if (reader.Name == "Template") {
							inlineTemplate = true;
							reader.Read ();

							readChildren (reader, crowType);
							continue;
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
							using (IMLReader iTmp = new IMLReader (null, reader.ReadInnerXml ())) {
								string uid = Guid.NewGuid ().ToString ();
								Interface.Instantiators [uid] =
								new ItemTemplate (iTmp.RootType, iTmp.GetLoader ());
								reader.il.Emit (OpCodes.Ldloc_0);//load TempControl ref
								reader.il.Emit (OpCodes.Ldfld,//load ItemTemplates dic field
									typeof(TemplatedControl).GetField("ItemTemplates"));
								reader.il.Emit (OpCodes.Ldstr, dataType);//load key
								reader.il.Emit (OpCodes.Ldstr, uid);//load value
								reader.il.Emit (OpCodes.Callvirt,
									typeof(Interface).GetMethod ("GetItemTemplate"));
								reader.il.Emit (OpCodes.Callvirt,
									typeof(Dictionary<string, ItemTemplate>).GetMethod ("set_Item",
										new Type[] { typeof(string), typeof(ItemTemplate) }));
							}
//							if (!string.IsNullOrEmpty (datas))
//								ItemTemplates [dataType].CreateExpandDelegate(this, dataType, datas);

							continue;
						}
					}

					if (!inlineTemplate) {
						if (string.IsNullOrEmpty (templatePath)) {
							DefaultTemplate dt = (DefaultTemplate)crowType.GetCustomAttributes (typeof(DefaultTemplate), true).FirstOrDefault ();
							if (dt!=null)
								templatePath = dt.Path;
						}

						if (!string.IsNullOrEmpty (templatePath)) {
							reader.il.Emit (OpCodes.Ldloc_0);//Load  this templateControl ref

							reader.il.Emit (OpCodes.Ldstr, templatePath); //Load template path string
							reader.il.Emit (OpCodes.Callvirt,//call Interface.Load(path)
								typeof(Interface).GetMethod ("Load", BindingFlags.Static | BindingFlags.Public));
							reader.il.Emit (OpCodes.Callvirt,//add child
								crowType.GetMethod ("loadTemplate", BindingFlags.Instance | BindingFlags.NonPublic));
						}
					}
				}
			}

			using (IMLReader reader = new IMLReader(il,tmpXml)){
				reader.Read ();

				if (reader.HasAttributes) {
					string style = reader.GetAttribute ("Style");
					if (!string.IsNullOrEmpty (style)) {
						PropertyInfo pi = crowType.GetProperty ("Style");
						CompilerServices.EmitSetValue (reader.il, pi, style);
					}
				}
				reader.il.Emit (OpCodes.Ldloc_0);
				reader.il.Emit (OpCodes.Callvirt, typeof(GraphicObject).GetMethod ("loadDefaultValues"));

				if (reader.HasAttributes) {

					MethodInfo miAddBinding = typeof(GraphicObject).GetMethod ("BindMember");

					while (reader.MoveToNextAttribute ()) {
						if (reader.Name == "Style")
							continue;

						MemberInfo mi = crowType.GetMember (reader.Name).FirstOrDefault();
						if (mi == null)
							throw new Exception ("Member '" + reader.Name + "' not found in " + crowType.Name);
						if (mi.MemberType == MemberTypes.Event) {
							CompilerServices.emitBindingCreation (reader.il, reader.Name, reader.Value);
							continue;
						}
						PropertyInfo pi = mi as PropertyInfo;
						if (pi == null)
							throw new Exception ("Member '" + reader.Name + "' not found in " + crowType.Name);

						if (reader.Value.StartsWith ("{")) {
							CompilerServices.emitBindingCreation (reader.il, reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));
						}else
							CompilerServices.EmitSetValue (reader.il, pi, reader.Value);

					}
					reader.MoveToElement ();
				}

				if (reader.IsEmptyElement) {
					reader.il.Emit (OpCodes.Pop);//pop saved ref to current object
					return;
				}

				readChildren (reader, crowType);
			}
			il.Emit (OpCodes.Pop);//pop saved ref to current object
		}
		/// <summary>
		/// Parse child node an generate corresponding msil
		/// </summary>
		void readChildren(IMLReader reader, Type crowType){
			MethodInfo miAddChild = null;
			bool endTagReached = false;
			while (reader.Read()){
				switch (reader.NodeType) {
				case XmlNodeType.EndElement:
					endTagReached = true;
					break;
				case XmlNodeType.Element:
					//Templates
					if (reader.Name == "Template" ||
						reader.Name == "ItemTemplate") {
						reader.Skip ();
						continue;
					}

					if (miAddChild == null) {
						if (typeof(Group).IsAssignableFrom (crowType))
							miAddChild = typeof(Group).GetMethod ("AddChild");
						else if (typeof(Container).IsAssignableFrom (crowType))
							miAddChild = typeof(Container).GetMethod ("SetChild");
						else if (typeof(TemplatedContainer).IsAssignableFrom (crowType))
							miAddChild = typeof(TemplatedContainer).GetProperty("Content").GetSetMethod();
						else if (typeof(TemplatedControl).IsAssignableFrom (crowType))
							miAddChild = typeof(TemplatedControl).GetMethod ("loadTemplate",
								BindingFlags.Instance | BindingFlags.NonPublic);						
						else if (typeof(PrivateContainer).IsAssignableFrom (crowType))
							miAddChild = typeof(PrivateContainer).GetMethod ("SetChild",
								BindingFlags.Instance | BindingFlags.NonPublic);
					}

					//push current instance on stack for parenting
					//loc_0 will be used for child
					reader.il.Emit (OpCodes.Ldloc_0);

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

					reader.il.Emit(OpCodes.Newobj, t.GetConstructors () [0]);//TODO:search parameterless ctor
					reader.il.Emit (OpCodes.Stloc_0);//child is now loc_0

					reader.emitLoader(t);

					reader.il.Emit (OpCodes.Ldloc_0);//load child on stack for parenting
					reader.il.Emit (OpCodes.Callvirt, miAddChild);
					reader.il.Emit (OpCodes.Stloc_0); //reset local to current go
					reader.il.Emit (OpCodes.Ldloc_0);//save current go onto the stack if child has to be added
					break;
				}
				if (endTagReached)
					break;
			}
		}
		/// <summary>
		/// read first node to set GraphicObject class for loading
		/// and let reader position on that node
		/// </summary>
		Type readRootType ()
		{
			string root = "Object";
			while (Read()) {
				if (NodeType == XmlNodeType.Element) {
					root = this.Name;
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
			RootType = t;
			return t;
		}
	}
}

