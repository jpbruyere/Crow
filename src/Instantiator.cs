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
using Crow.IML;

namespace Crow
{
	public delegate void InstanciatorInvoker(object instance, Interface iface);

	/// <summary>
	/// Instantiator
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

		public GraphicObject CreateInstance(Interface iface){
			GraphicObject tmp = (GraphicObject)Activator.CreateInstance(RootType);
			loader (tmp, iface);
			return tmp;
		}

		List<DynamicMethod> dsValueChangedDynMeths = new List<DynamicMethod>();
		List<Delegate> dataSourceChangedDelegates = new List<Delegate>();

		#region IML parsing
		/// <summary>
		/// Parses IML and build a dynamic method that will be used to instanciate one or multiple occurence of the IML file or fragment
		/// </summary>
		void parseIML (XmlTextReader reader) {
			Context ctx = new Context (findRootType (reader));

			ctx.CurrentNode = new Node (ctx.RootType);
			emitLoader (reader, ctx);

			ctx.il.Emit(OpCodes.Ret);

			reader.Read ();//close tag
			RootType = ctx.RootType;
			loader = (InstanciatorInvoker)ctx.dm.CreateDelegate (typeof (InstanciatorInvoker), this);
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
			using (XmlTextReader reader = new XmlTextReader (tmpXml, XmlNodeType.Element, null)) {
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
						ctx.il.Emit (OpCodes.Ldarg_2);//load currentInterface
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
			using (XmlTextReader reader = new XmlTextReader (tmpXml, XmlNodeType.Element, null)) {
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

						//if (pi.Name == "Name")
						//	ctx.Names.Add (reader.Value, Node.AddressToString (ctx.nodesStack.ToArray ()));

						if (reader.Value.StartsWith ("{", StringComparison.OrdinalIgnoreCase)) {
							

							readPropertyBinding (ctx, reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));

							//CompilerServices.emitBindingCreation (reader.il, reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));
						} else
							CompilerServices.EmitSetValue (ctx.il, pi, reader.Value);

					}
					reader.MoveToElement ();
				}
				#endregion

				if (reader.IsEmptyElement) {
					//ctx.il.Emit (OpCodes.Pop);//pop saved ref to current object
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
		#endregion

		/// <summary>
		/// Tries the find target.
		/// </summary>
		/// <returns>The member address in the graphic tree, null if on dataSource, </returns>
		/// <param name="expression">Expression.</param>
		void readPropertyBinding (Context ctx, string sourceMember, string expression)
		{
			MemberAddress targetMember;
			NodeAddress target;

			string memberName = null;
			bool twoWay = false;

			//if binding exp = '{}' => binding is done on datasource
			if (string.IsNullOrEmpty (expression))
				return;			

			if (expression.StartsWith ("²")) {
				expression = expression.Substring (1);
				twoWay = true;
			}

			string [] bindingExp = expression.Split ('/');

			if (bindingExp.Length == 1) {
				//datasource binding
				processDataSourceBinding(ctx, sourceMember, bindingExp [0]);

			}
//			else {
//				int ptr = 0;
//
//				//if exp start with '/' => Graphic tree parsing start at source
//				if (string.IsNullOrEmpty (bindingExp [0]))
//					ptr++;
//				else if (bindingExp[0] == "."){ //search template root
//					do {
//						target = new NodeAddress(ctx.
//						if (tmpTarget == null)
//							return false;
//						if (tmpTarget is Interface)
//							throw new Exception ("Not in Templated Control");
//					} while (!(tmpTarget is TemplatedControl));
//					ptr++;
//				}
//				while (ptr < bindingExp.Length - 1) {
//					if (tmpTarget == null) {
//						#if DEBUG_BINDING
//						Debug.WriteLine ("\tTarget not found => " + this.ToString());
//						#endif
//						return false;
//					}
//					if (bindingExp [ptr] == "..")
//						tmpTarget = tmpTarget.LogicalParent;
//					else if (bindingExp [ptr] == ".") {
//						if (ptr > 0)
//							throw new Exception ("Syntax error in binding, './' may only appear in first position");
//						tmpTarget = Source.Instance as ILayoutable;
//					} else
//						tmpTarget = (tmpTarget as GraphicObject).FindByName (bindingExp [ptr]);
//					ptr++;
//				}
//
//				if (tmpTarget == null) {
//					#if DEBUG_BINDING
//					Debug.WriteLine ("\tBinding Target not found => " + this.ToString());
//					#endif
//					return false;
//				}
//
//				string [] bindTrg = bindingExp [ptr].Split ('.');
//
//				if (bindTrg.Length == 1)
//					memberName = bindTrg [0];
//				else if (bindTrg.Length == 2) {
//					tmpTarget = (tmpTarget as GraphicObject).FindByName (bindTrg [0]);
//					memberName = bindTrg [1];
//				} else
//					throw new Exception ("Syntax error in binding, expected 'go dot member'");
//
//				if (tmpTarget == null) {
//					#if DEBUG_BINDING
//					Debug.WriteLine ("\tBinding Target not found => " + this.ToString());
//					#endif
//					return false;
//				}
//
//				Target = new MemberReference (tmpTarget);
//			}
//
//			if (Target.TryFindMember (memberName)) {
//				if (TwoWayBinding) {
//					//					IBindable source = Target.Instance as IBindable;
//					//					if (source == null)
//					//						throw new Exception (Source.Instance + " does not implement IBindable for 2 way bindings");
//					//					source.Bindings.Add (new Binding (Target, Source));
//				}
//			}
			#if DEBUG_BINDING
			else
			Debug.WriteLine ("Property less binding: " + Target + expression);
			#endif
		}

		void processDataSourceBinding(Context ctx, string sourceMember, string dataSourceMember){
			#region create valuechanged method
			DynamicMethod dm = new DynamicMethod ("dyn_valueChanged",
				typeof (void),
				CompilerServices.argsValueChange, true);

			ILGenerator il = dm.GetILGenerator (256);

			System.Reflection.Emit.Label endMethod = il.DefineLabel ();

			il.Emit (OpCodes.Nop);

			//load value changed member name onto the stack
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldfld, CompilerServices.fiMbName);

			//test if it's the expected one
			il.Emit (OpCodes.Ldstr, dataSourceMember);
			il.Emit (OpCodes.Ldc_I4_4);//StringComparison.Ordinal
			il.Emit (OpCodes.Callvirt, CompilerServices.stringEquals);
			il.Emit (OpCodes.Brfalse, endMethod);
			//set destination member with valueChanged new value
			//load destination ref
			il.Emit (OpCodes.Ldarg_0);
			//load new value onto the stack
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldfld, CompilerServices.fiNewValue);

			//by default, source value type is deducted from target member type to allow
			//memberless binding, if targetMember exists, it will be used to determine target
			//value type for conversion
			PropertyInfo piSource = ctx.CurrentNode.CrowType.GetProperty(sourceMember);
			Type sourceValueType = piSource.PropertyType;

			if (sourceValueType == typeof (string)) {
				il.Emit (OpCodes.Callvirt, CompilerServices.miObjToString);
			} else if (!sourceValueType.IsValueType)
				il.Emit (OpCodes.Castclass, sourceValueType);
			else if (sourceValueType != sourceValueType) {
				il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (sourceValueType));
			} else
				il.Emit (OpCodes.Unbox_Any, sourceValueType);

			il.Emit (OpCodes.Callvirt, piSource.GetSetMethod ());
			//il.Emit (OpCodes.Pop);
			//il.Emit (OpCodes.Pop);

			il.MarkLabel (endMethod);

			il.Emit (OpCodes.Ret);

			//vc dyn meth is stored in a cached list, it will be bound to datasource only
			//when datasource of source graphic object changed
			int dmVC = dsValueChangedDynMeths.Count;
			//Delegate tmp = dm.CreateDelegate(typeof(EventHandler<ValueChangeEventArgs>), this);
			dsValueChangedDynMeths.Add (dm);
			#endregion

			#region emit dataSourceChanged event handler
			//now we create the datasource changed method that will init the destination member with 
			//the actual value of the origin member of the datasource and then will bind the value changed 
			//dyn methode.
			//dm is bound to the instanciator instance to have access to cached dyn meth and delegates
			dm = new DynamicMethod ("dyn_dschanged",
				typeof (void),
				CompilerServices.argsDSChange, true);

			il = dm.GetILGenerator (256);
			//il.DeclareLocal (typeof(Object[]));

			il.Emit (OpCodes.Nop);

//			//load new datasource onto the stack for handler addition at the end
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);

			#region Load cached delegate
			il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			il.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("dsValueChangedDynMeths", BindingFlags.Instance | BindingFlags.NonPublic));
			il.Emit(OpCodes.Ldc_I4, dmVC);
			il.Emit(OpCodes.Callvirt, typeof(List<DynamicMethod>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
			#endregion

			//load ds changed eventhandlertype
			il.Emit(OpCodes.Ldtoken, typeof(EventHandler<ValueChangeEventArgs>));
			il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new
				Type[1]{typeof(RuntimeTypeHandle)}));
			//typeof (GraphicObject).GetEvent ("DataSourceChanged").EventHandlerType;
			//il.Emit(OpCodes.Ldfld, typeof(CompilerServices).GetField("ehTypeDSChange", BindingFlags.Static | BindingFlags.NonPublic););

			//create object[] for delegate creation parameters
//			il.Emit (OpCodes.Ldc_I4_1);
//			il.Emit (OpCodes.Newarr, typeof(object));
//			il.Emit (OpCodes.Stloc_0);//save array ref
//			il.Emit (OpCodes.Ldloc_0);//load array ref onto the stack
//			il.Emit (OpCodes.Ldc_I4_0);//0 is the index of the dm in the array

			//load datasource change source
			il.Emit (OpCodes.Ldarg_1);
			//il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);

			//create bound delegate
			il.Emit (OpCodes.Call, CompilerServices.miCreateBoundDelegate);
			//set delegete as index 0 in the object array
//			il.Emit(OpCodes.Ldelem_I4);

			//add new delegate to datasource valuechanged event
			il.Emit(OpCodes.Callvirt, typeof(IValueChange).GetEvent("ValueChanged").AddMethod);//call add event			//il.Emit(OpCodes.Pop);
			//il.Emit(OpCodes.Pop);
			//il.Emit(OpCodes.Pop);
			//il.Emit(OpCodes.Pop);
			il.Emit (OpCodes.Ret);


			#endregion

			int delDSIndex = dataSourceChangedDelegates.Count;
			Delegate del = dm.CreateDelegate (CompilerServices.ehTypeDSChange, this);
			dataSourceChangedDelegates.Add(del);

			#region Emit datasourcechanged handler binding in the loader context
			ctx.il.Emit(OpCodes.Ldloc_0);//load ref to current graphic object
			ctx.il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			ctx.il.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("dataSourceChangedDelegates", BindingFlags.Instance | BindingFlags.NonPublic));
			ctx.il.Emit(OpCodes.Ldc_I4, delDSIndex);//load delegate index
			ctx.il.Emit(OpCodes.Callvirt, typeof(List<DynamicMethod>).GetMethod("get_Item", new Type[] { typeof(Int32) }));

			ctx.il.Emit(OpCodes.Callvirt, typeof(GraphicObject).GetEvent("DataSourceChanged").AddMethod);//call add event
			#endregion
			//miValueChangeAdd.Invoke (grouped [0].Target.Instance, new object [] { del });
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

