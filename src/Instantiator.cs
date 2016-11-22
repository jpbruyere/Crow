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
		Dictionary<string, Delegate> bindingDelegates = new Dictionary<string, Delegate>();//valuechanged del
		Dictionary<string, Delegate> bindingInitializer = new Dictionary<string, Delegate>();//initialize with actual values of binding origine

		#region IML parsing
		/// <summary>
		/// Parses IML and build a dynamic method that will be used to instanciate one or multiple occurence of the IML file or fragment
		/// </summary>
		void parseIML (XmlTextReader reader) {
			Context ctx = new Context (findRootType (reader));

			emitLoader (reader, ctx, ctx.RootType);

			emitBindingDelegates (ctx);

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
		void emitLoader (XmlTextReader reader, Context ctx, Type newType)
		{
			ctx.nodesStack.Push(new Node (newType, ctx.CurrentIndex));
			string tmpXml = reader.ReadOuterXml ();

			if (ctx.nodesStack.Peek().HasTemplate)
				emitTemplateLoad (ctx, tmpXml);

			emitGOLoad (ctx, tmpXml);

			emitCheckAndBindValueChanged (ctx);

			ctx.nodesStack.Pop ();
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
						ctx.CurrentIndex = -1;
						readChildren (reader, ctx);
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
			ctx.nodesStack.IncrementCurrentNodeIndex ();
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

						MemberInfo mi = ctx.CurrentNodeType.GetMember (reader.Name).FirstOrDefault ();
						if (mi == null)
							throw new Exception ("Member '" + reader.Name + "' not found in " + ctx.CurrentNodeType.Name);
						if (mi.MemberType == MemberTypes.Event) {
							//CompilerServices.emitBindingCreation (ctx.il, reader.Name, reader.Value);
							continue;
						}
						PropertyInfo pi = mi as PropertyInfo;
						if (pi == null)
							throw new Exception ("Member '" + reader.Name + "' not found in " + ctx.CurrentNodeType.Name);

						if (pi.Name == "Name"){
							if (!ctx.Names.ContainsKey(reader.Value))
								ctx.Names[reader.Value] = new List<NodeAddress>();							
							ctx.Names[reader.Value].Add(ctx.CurrentNodeAddress);
						}
						
						if (reader.Value.StartsWith ("{", StringComparison.OrdinalIgnoreCase))
							readPropertyBinding (ctx, reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));
						else
							CompilerServices.EmitSetValue (ctx.il, pi, reader.Value);

					}
					reader.MoveToElement ();
				}
				#endregion

				if (!reader.IsEmptyElement) {
					ctx.CurrentIndex = 0;
					readChildren (reader, ctx);
				}
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

					//push 2x current instance on stack for parenting and reseting loc0 to parent
					//loc_0 will be used for child
					ctx.il.Emit (OpCodes.Ldloc_0);
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

					emitLoader (reader, ctx, t);

					ctx.il.Emit (OpCodes.Ldloc_0);//load child on stack for parenting
					ctx.il.Emit (OpCodes.Callvirt, ctx.CurrentAddMethod);
					ctx.il.Emit (OpCodes.Stloc_0); //reset local to current go

					ctx.CurrentIndex++;

					break;
				}
				if (endTagReached)
					break;
			}
		}
		#endregion


		void emitCheckAndBindValueChanged(Context ctx){
			System.Reflection.Emit.Label labContinue = ctx.il.DefineLabel ();
			string strNA = ctx.CurrentNodeAddress.ToString ();
			//first, test if current node is in bindingDelegate dictionnary
			ctx.il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			ctx.il.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("bindingDelegates", BindingFlags.Instance | BindingFlags.NonPublic));
			ctx.il.Emit (OpCodes.Ldstr, strNA);//load binding id for current node
			ctx.il.Emit (OpCodes.Call, typeof(Dictionary<string,Delegate>).GetMethod ("ContainsKey"));

			ctx.il.Emit (OpCodes.Brfalse, labContinue);//if not present, do nothing

			ctx.il.Emit (OpCodes.Ldloc_0);//load current instance for event add
			//fetch delegate
			ctx.il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			ctx.il.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("bindingDelegates", BindingFlags.Instance | BindingFlags.NonPublic));
			ctx.il.Emit (OpCodes.Ldstr, strNA);//load binding id for current node
			ctx.il.Emit (OpCodes.Callvirt, typeof(Dictionary<string, Delegate>).GetMethod ("get_Item", new Type[] { typeof(string) }));

			//attach to valuechanged handler
			ctx.il.Emit(OpCodes.Callvirt, typeof(IValueChange).GetEvent("ValueChanged").AddMethod);

			//call initializer 
			ctx.il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			ctx.il.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("bindingInitializer", BindingFlags.Instance | BindingFlags.NonPublic));
			ctx.il.Emit (OpCodes.Ldstr, strNA);//load binding id for current node
			ctx.il.Emit (OpCodes.Call, typeof(Dictionary<string, Delegate>).GetMethod ("get_Item", new Type[] { typeof(string) }));
			ctx.il.Emit (OpCodes.Ldloc_0);//load current instance, passed as arg to invoke initializer
			ctx.il.Emit(OpCodes.Callvirt, typeof(Action<object>).GetMethod("Invoke"));

			ctx.il.MarkLabel (labContinue);
		}
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

			string[] bindingExp = expression.Split ('/');

			if (bindingExp.Length == 1) {
				//datasource binding
				processDataSourceBinding (ctx, sourceMember, bindingExp [0]);
				return;
			}

			NodeAddress currentNode = ctx.CurrentNodeAddress;
			int ptr = currentNode.Count - 1;

			//if exp start with '/' => Graphic tree parsing start at source
			if (string.IsNullOrEmpty (bindingExp [0])) {
				//TODO:
			} else if (bindingExp [0] == ".") { //search template root
				while (ptr > 0) {
					ptr--;
					if (typeof(TemplatedControl).IsAssignableFrom (currentNode [ptr].CrowType))
						break;
				}
			} else if (bindingExp [0] == "..") { //search starting at current node
				int levelUp = bindingExp.Length - 1;
				if (levelUp > ptr)
					throw new Exception ("Binding error: try to bind outside IML source");
				ptr -= levelUp;
			}
			Node[] targetNode = new Node[ptr+1];
			Array.Copy (currentNode.ToArray (), targetNode, ptr + 1);
			NodeAddress targetNA = new NodeAddress (targetNode);

			string [] bindTrg = bindingExp.Last().Split ('.');

			if (bindTrg.Length == 1)
				memberName = bindTrg [0];
			else if (bindTrg.Length == 2) {
				//named target
				//TODO:

				memberName = bindTrg [1];
				return;
			} else
				throw new Exception ("Syntax error in binding, expected 'go dot member'");

			Dictionary<string, List<MemberAddress>> nodeBindings = null;
			if (ctx.Bindings.ContainsKey (targetNA))
				nodeBindings = ctx.Bindings [targetNA];
			else {
				nodeBindings = new Dictionary<string, List<MemberAddress>> ();
				ctx.Bindings [targetNA] = nodeBindings;
			}

			if (!nodeBindings.ContainsKey (memberName))
				nodeBindings [memberName] = new List<MemberAddress> ();
			nodeBindings [memberName].Add (new MemberAddress (currentNode, sourceMember));

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
		/// <summary>
		/// create the valuechanged handler, the datasourcechanged handler and emit event handling
		/// </summary>
		void processDataSourceBinding(Context ctx, string sourceMember, string dataSourceMember){
			#region create valuechanged method
			DynamicMethod dm = new DynamicMethod ("dyn_DSvalueChanged",
				typeof (void),
				CompilerServices.argsBoundValueChange, true);

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
			PropertyInfo piSource = ctx.CurrentNodeType.GetProperty(sourceMember);
			Type sourceValueType = piSource.PropertyType;

			//il.Emit (OpCodes.Call, typeof(object).GetMethod("GetType"));
			//il.Emit (OpCodes.Call, typeof(Type).GetProperty("IsValueType").GetGetMethod());

			if (sourceValueType == typeof (string)) {
				il.Emit (OpCodes.Callvirt, CompilerServices.miObjToString);
			} else if (!sourceValueType.IsValueType)
				il.Emit (OpCodes.Castclass, sourceValueType);
			else if (sourceValueType != sourceValueType) {
				il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (sourceValueType));
			} else
				il.Emit (OpCodes.Unbox_Any, sourceValueType);

			il.Emit (OpCodes.Callvirt, piSource.GetSetMethod ());

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

			il.Emit (OpCodes.Nop);

			//load new datasource onto the stack for handler addition at the end
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);

			//first we have to create delegate from cached dynMethod bound to the GraphicObject currently instanced

			//Load cached delegate
			il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			il.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("dsValueChangedDynMeths", BindingFlags.Instance | BindingFlags.NonPublic));
			il.Emit(OpCodes.Ldc_I4, dmVC);//load index of dynmathod
			il.Emit(OpCodes.Callvirt, typeof(List<DynamicMethod>).GetMethod("get_Item", new Type[] { typeof(Int32) }));

			//load ds changed eventhandlertype
			il.Emit(OpCodes.Ldtoken, typeof(EventHandler<ValueChangeEventArgs>));
			il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new
				Type[1]{typeof(RuntimeTypeHandle)}));

			il.Emit (OpCodes.Ldarg_1);//load datasource change source
			il.Emit (OpCodes.Call, CompilerServices.miCreateBoundDelegate);//create bound delegate

			//add new delegate to datasource valuechanged event
			il.Emit(OpCodes.Callvirt, typeof(IValueChange).GetEvent("ValueChanged").AddMethod);//call add event			//il.Emit(OpCodes.Pop);
			il.Emit (OpCodes.Ret);

			//store dschange delegate in instatiator instance for access while instancing graphic object
			int delDSIndex = dataSourceChangedDelegates.Count;
			Delegate del = dm.CreateDelegate (CompilerServices.ehTypeDSChange, this);
			dataSourceChangedDelegates.Add(del);
			#endregion

			#region Emit datasourcechanged handler binding in the loader context
			ctx.il.Emit(OpCodes.Ldloc_0);//load ref to current graphic object
			ctx.il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			ctx.il.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("dataSourceChangedDelegates", BindingFlags.Instance | BindingFlags.NonPublic));
			ctx.il.Emit(OpCodes.Ldc_I4, delDSIndex);//load delegate index
			ctx.il.Emit(OpCodes.Callvirt, typeof(List<DynamicMethod>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
			ctx.il.Emit(OpCodes.Callvirt, typeof(GraphicObject).GetEvent("DataSourceChanged").AddMethod);//call add event
			#endregion
		}

		/// <summary>
		/// Create and store in the instanciator the ValueChanged delegates
		/// those delegates uses grtree functions to set destination value so they don't
		/// need to be bound to destination instance as in the ancient system.
		/// </summary>
		void emitBindingDelegates(Context ctx){
			foreach (KeyValuePair<NodeAddress,Dictionary<string, List<MemberAddress>>> bindings in ctx.Bindings ) {	

				Type origineNodeType = bindings.Key.NodeType;

				//init method, current instance passed as arg
				DynamicMethod dmInit = new DynamicMethod ("dyn_initBinding",
					typeof (void), new Type[] {typeof(object)}, true);
				ILGenerator ilInit = dmInit.GetILGenerator (256);
				ilInit.Emit (OpCodes.Nop);

				//value changed dyn method
				DynamicMethod dm = new DynamicMethod ("dyn_valueChanged",
					typeof (void), CompilerServices.argsValueChange, true);

				ILGenerator il = dm.GetILGenerator (256);

				System.Reflection.Emit.Label endMethod = il.DefineLabel ();

				il.DeclareLocal (typeof(object));

				il.Emit (OpCodes.Nop);
				//il.Emit (OpCodes.Ldarg_0);//load source instance of ValueChanged event

				int i = 0;
				foreach (KeyValuePair<string, List<MemberAddress>> bindingCase in bindings.Value ) {
					Type origineType = origineNodeType.GetProperty (bindingCase.Key).PropertyType;

					System.Reflection.Emit.Label nextTest = il.DefineLabel ();

					#region member name test
					//load source member name
					il.Emit (OpCodes.Ldarg_1);
					il.Emit (OpCodes.Ldfld, typeof(ValueChangeEventArgs).GetField ("MemberName"));

					il.Emit (OpCodes.Ldstr, bindingCase.Key);//load name to test
					il.Emit (OpCodes.Ldc_I4_4);//StringComparison.Ordinal
					il.Emit (OpCodes.Callvirt, CompilerServices.stringEquals);
					il.Emit (OpCodes.Brfalse, nextTest);//if not equal, jump to next case
					#endregion

					#region destination member affectations
					foreach (MemberAddress ma in bindingCase.Value) {
						//for initialisation dynmeth, load current instance
						ilInit.Emit(OpCodes.Ldarg_0);
						//first we have to load destination instance onto the stack, it is access
						//with graphic tree functions deducted from nodes topology
						il.Emit (OpCodes.Ldarg_0);//load source instance of ValueChanged event

						NodeAddress origine = bindings.Key;
						NodeAddress destination = ma.Address;

						emitGetInstance(il,origine,destination);
						emitGetInstance(ilInit,origine,destination);

						//init dynmeth: load actual value
						ilInit.Emit (OpCodes.Ldarg_0);
						ilInit.Emit (OpCodes.Callvirt, origineNodeType.GetProperty (bindingCase.Key).GetGetMethod());

						//load new value
						il.Emit (OpCodes.Ldarg_1);
						il.Emit (OpCodes.Ldfld, typeof (ValueChangeEventArgs).GetField ("NewValue"));

						emitConvert(ilInit,origineType,ma.Property.PropertyType);
						emitConvert(il,origineType, ma.Property.PropertyType);

						//set value
						ilInit.Emit (OpCodes.Callvirt, ma.Property.GetSetMethod());
						il.Emit (OpCodes.Callvirt, ma.Property.GetSetMethod());

						il.Emit (OpCodes.Br, endMethod);
						il.MarkLabel (nextTest);
					}
					#endregion

					i++;
				}
				//il.Emit (OpCodes.Pop);
				il.MarkLabel (endMethod);
				ilInit.Emit (OpCodes.Ret);
				il.Emit (OpCodes.Ret);

				bindingDelegates [bindings.Key.ToString()] = dm.CreateDelegate (typeof(EventHandler<ValueChangeEventArgs>));
				bindingInitializer [bindings.Key.ToString()] = dmInit.CreateDelegate (typeof(Action<object>));
			}
		}
		void emitGetInstance (ILGenerator il, NodeAddress orig, NodeAddress dest){
			if (orig.Count < dest.Count){
				for (int i = orig.Count-1; i < dest.Count-1; i++){
					if (typeof (Group).IsAssignableFrom (dest[i].CrowType)) {
						il.Emit (OpCodes.Ldfld, typeof(Group).GetField ("children", BindingFlags.Instance | BindingFlags.NonPublic));
						il.Emit(OpCodes.Ldc_I4, dest[i+1].Index);
						il.Emit (OpCodes.Callvirt, typeof(List<GraphicObject>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
						continue;
					}
					if (typeof(Container).IsAssignableFrom (dest[i].CrowType) || dest[i+1].Index < 0) {
						il.Emit (OpCodes.Ldfld, typeof(PrivateContainer).GetField ("child", BindingFlags.Instance | BindingFlags.NonPublic));
						continue;
					}
					if (typeof(TemplatedContainer).IsAssignableFrom (dest[i].CrowType)) {
						il.Emit (OpCodes.Callvirt, typeof(TemplatedContainer).GetProperty ("Content").GetGetMethod ());
						continue;
					}
					if (typeof(TemplatedGroup).IsAssignableFrom (dest[i].CrowType)) {
						il.Emit (OpCodes.Callvirt, typeof(TemplatedGroup).GetProperty ("Items").GetGetMethod ());
						il.Emit(OpCodes.Ldc_I4, dest[i+1].Index);
						il.Emit (OpCodes.Callvirt, typeof(List<GraphicObject>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
						continue;
					}
				}
				return;
			}

			for (int j = dest.Count; j < orig.Count; j++)
				il.Emit(OpCodes.Callvirt, typeof(ILayoutable).GetProperty("Parent").GetGetMethod());
		}
		void emitConvert(ILGenerator il, Type origType, Type destType){			
			if (destType == typeof (string))
				il.Emit (OpCodes.Callvirt, CompilerServices.miObjToString);
			else if (origType.IsValueType) {
				if (destType != origType)
					il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (destType));
				else
					il.Emit (OpCodes.Unbox_Any, destType);
			}else
				il.Emit (OpCodes.Castclass, destType);
		}
		void emitConvertWorking(ILGenerator il, Type sourceValueType){
			if (sourceValueType == typeof (string)) {
				il.Emit (OpCodes.Callvirt, CompilerServices.miObjToString);
			} else if (!sourceValueType.IsValueType)
				il.Emit (OpCodes.Castclass, sourceValueType);
			else if (sourceValueType != sourceValueType) {
				il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (sourceValueType));
			} else
				il.Emit (OpCodes.Unbox_Any, sourceValueType);			
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

