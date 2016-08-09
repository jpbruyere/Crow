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

namespace Crow
{
	public class IMLReader : XmlTextReader
	{
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
			readRootType();
			InitEmitter();
			BuildInstanciator(RootType);
			Read();//close tag
		}
		public IMLReader (ILGenerator ilGen, string xmlFragment)
			: base(xmlFragment, XmlNodeType.Element,null){
			il = ilGen;
		}
		#endregion

		/// <summary>
		/// Finalize instatiator MSIL and return LoaderInvoker delegate
		/// </summary>
		public Instantiator GetInstanciator(){
			il.Emit(OpCodes.Ret);

			return new Instantiator (RootType,
				(Interface.LoaderInvoker)dm.CreateDelegate (typeof(Interface.LoaderInvoker)));
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
		void BuildInstanciator(Type crowType){
			string tmpXml = ReadOuterXml ();

			il.Emit (OpCodes.Ldloc_0);//save current go onto the stack if child has to be added

			if (typeof(TemplatedControl).IsAssignableFrom (crowType)) {
				//if its a template, first read template elements
				using (IMLReader reader = new IMLReader (il, tmpXml)) {

					string template = reader.GetAttribute ("Template");

					bool inlineTemplate = false;
					if (string.IsNullOrEmpty (template)) {
						reader.Read ();

						while (reader.Read ()) {
							if (!reader.IsStartElement ())
								continue;
							if (reader.Name == "Template") {
								inlineTemplate = true;
								reader.Read ();

								readChildren (reader, crowType);
								continue;
							}else if (reader.Name == "ItemTemplate") {
								reader.Skip ();
								//								string dataType = "default", datas = "", itemTmp;
								//								while (reader.MoveToNextAttribute ()) {
								//									if (reader.Name == "DataType")
								//										dataType = reader.Value;
								//									else if (reader.Name == "Data")
								//										datas = reader.Value;
								//								}
								//
								//								reader.Read();
								//								itemTmp = .ReadInnerXml ();
								//
								//								if (ItemTemplates == null)
								//									ItemTemplates = new Dictionary<string, ItemTemplate> ();
								//								//TODO:check encoding
								//								ItemTemplates[dataType] = new ItemTemplate (Encoding.UTF8.GetBytes(itemTmp));
								//								if (!string.IsNullOrEmpty (datas))
								//									ItemTemplates [dataType].CreateExpandDelegate(this, dataType, datas);

								continue;
							}
						}
						if (!inlineTemplate) {
							DefaultTemplate dt = (DefaultTemplate)crowType.GetCustomAttributes (typeof(DefaultTemplate), true).FirstOrDefault();
							template = dt.Path;
						}
					}
					if (!inlineTemplate) {
						reader.il.Emit (OpCodes.Ldloc_0);//Load  this templateControl ref

						reader.il.Emit (OpCodes.Ldstr, template); //Load template path string
						reader.il.Emit (OpCodes.Callvirt,//call Interface.Load(path)
							typeof(Interface).GetMethod ("Load", BindingFlags.Static | BindingFlags.Public));
					}
					reader.il.Emit (OpCodes.Callvirt,//add child
						crowType.GetMethod ("loadTemplate", BindingFlags.Instance | BindingFlags.NonPublic));
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
				if (reader.HasAttributes) {
					reader.il.Emit (OpCodes.Ldloc_0);
					reader.il.Emit (OpCodes.Callvirt, typeof(GraphicObject).GetMethod ("loadDefaultValues"));

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

					reader.il.Emit(OpCodes.Newobj, t.GetConstructors () [0]);
					reader.il.Emit (OpCodes.Stloc_0);//child is now loc_0

					reader.BuildInstanciator(t);

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

