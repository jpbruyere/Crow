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
		InstanciatorInvoker loader = null;
		DynamicMethod dm = null;

		public ILGenerator il = null;
		public Type RootType = null;

		/// <summary>
		/// Finalize instatiator MSIL and return LoaderInvoker delegate
		/// </summary>
		public InstanciatorInvoker GetLoader(){
			if (loader != null)
				return loader;

			il.Emit(OpCodes.Ret);
			loader = (InstanciatorInvoker)dm.CreateDelegate (typeof(InstanciatorInvoker));
			return loader;
		}

		#region CTOR
		public IMLReader (string path)
			: this(Interface.GetStreamFromPath (path)){
		}
		public IMLReader (Stream stream)
			: base(stream)
		{
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
		/// Inits il generator, RootType must have been read first
		/// </summary>
		void InitEmitter(){

			dm = new DynamicMethod("dyn_instantiator",
				MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
				CallingConventions.Standard,
				typeof(void),new Type[] {typeof(object), typeof(Interface)}, RootType, true);

			il = dm.GetILGenerator(256);
			il.DeclareLocal(typeof(GraphicObject));
			il.Emit(OpCodes.Nop);
			//set local GraphicObject to root object passed as 1st argument
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Stloc_0);
			CompilerServices.emitSetCurInterface (il);
		}
		void emitLoader(Type crowType){
			string tmpXml = ReadOuterXml ();

			il.Emit (OpCodes.Ldloc_0);//save current go onto the stack if child has to be added

			#region Template and ItemTemplates loading
			if (typeof(TemplatedControl).IsAssignableFrom (crowType)) {
				//if its a template, first read template elements
				using (IMLReader reader = new IMLReader (il, tmpXml)) {

					string templatePath = reader.GetAttribute ("Template");
					//string itemTemplatePath = reader.GetAttribute ("ItemTemplate");

					List<string[]> itemTemplateIds = new List<string[]> ();
					bool inlineTemplate = false;
					reader.Read ();
					int depth = reader.Depth + 1;
					while (reader.Read ()) {
						if (!reader.IsStartElement () || reader.Depth > depth)
							continue;
						if (reader.Name == "Template") {
							inlineTemplate = true;
							reader.Read ();

							readChildren (reader, crowType, true);
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
								using (IMLReader iTmp = new IMLReader (null, reader.ReadInnerXml ())) {
									itemTmpID = Guid.NewGuid ().ToString ();
									Interface.Instantiators [itemTmpID] =
										new ItemTemplate (iTmp.RootType, iTmp.GetLoader (), dataType, datas);
								}
							}else{
								if (!reader.IsEmptyElement)
									throw new Exception ("ItemTemplate with Path attribute may not include sub nodes");
								itemTmpID = path;
								Interface.Instantiators [itemTmpID] =
									new ItemTemplate (itemTmpID, dataType, datas);
							}
							itemTemplateIds.Add (new string[] { dataType, itemTmpID, datas });
						}
					}

					if (!inlineTemplate) {
						reader.il.Emit (OpCodes.Ldloc_0);//Load  this templateControl ref
						if (string.IsNullOrEmpty (templatePath)) {
							reader.il.Emit (OpCodes.Ldnull);//default template loading
						}else{
							reader.il.Emit (OpCodes.Ldarg_1);//load currentInterface
							reader.il.Emit (OpCodes.Ldstr, templatePath); //Load template path string
							reader.il.Emit (OpCodes.Callvirt,//call Interface.Load(path)
								CompilerServices.miIFaceLoad);
						}
						reader.il.Emit (OpCodes.Callvirt,//load template
							crowType.GetMethod ("loadTemplate", BindingFlags.Instance | BindingFlags.NonPublic));
					}
					foreach (string[] iTempId in itemTemplateIds) {
						reader.il.Emit (OpCodes.Ldloc_0);//load TempControl ref
						reader.il.Emit (OpCodes.Ldfld, CompilerServices.fldItemTemplates);//load ItemTemplates dic field
						reader.il.Emit (OpCodes.Ldstr, iTempId[0]);//load key
						reader.il.Emit (OpCodes.Ldstr, iTempId[1]);//load value
						reader.il.Emit (OpCodes.Callvirt, CompilerServices.miGetITemp);
						reader.il.Emit (OpCodes.Callvirt, CompilerServices.miAddITemp);

						if (!string.IsNullOrEmpty (iTempId [2])) {
							//expand delegate creation
							reader.il.Emit (OpCodes.Ldloc_0);//load TempControl ref
							reader.il.Emit (OpCodes.Ldfld, CompilerServices.fldItemTemplates);
							reader.il.Emit (OpCodes.Ldstr, iTempId [0]);//load key
							reader.il.Emit (OpCodes.Callvirt, CompilerServices.miGetITempFromDic);
							reader.il.Emit (OpCodes.Ldloc_0);//load root of treeView
							reader.il.Emit (OpCodes.Callvirt, CompilerServices.miCreateExpDel);
						}
					}
				}
			}
			#endregion

			using (IMLReader reader = new IMLReader(il,tmpXml)){
				reader.Read ();

				#region Styling and default values loading
				if (reader.HasAttributes) {
					string style = reader.GetAttribute ("Style");
					if (!string.IsNullOrEmpty (style)) {
						PropertyInfo pi = crowType.GetProperty ("Style");
						CompilerServices.EmitSetValue (reader.il, pi, style);
					}
				}
				reader.il.Emit (OpCodes.Ldloc_0);
				reader.il.Emit (OpCodes.Callvirt, typeof(GraphicObject).GetMethod ("loadDefaultValues"));
				#endregion

				#region Attributes reading
				if (reader.HasAttributes) {

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
				#endregion

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
		void readChildren(IMLReader reader, Type crowType, bool templateLoading = false){
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
						else if (typeof(TemplatedContainer).IsAssignableFrom (crowType) && !templateLoading)
							miAddChild = typeof(TemplatedContainer).GetProperty ("Content").GetSetMethod ();
						else if (typeof(TemplatedGroup).IsAssignableFrom (crowType) && !templateLoading)
							miAddChild = typeof(TemplatedGroup).GetMethod ("AddItem",
								BindingFlags.Instance | BindingFlags.Public);
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

					reader.il.Emit (OpCodes.Newobj, t.GetConstructors () [0]);//TODO:search parameterless ctor
					reader.il.Emit (OpCodes.Stloc_0);//child is now loc_0
					CompilerServices.emitSetCurInterface (il);

					reader.emitLoader (t);

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

