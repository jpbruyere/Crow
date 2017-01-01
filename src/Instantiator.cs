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
		#region Dynamic Method ID generation
		static long curId = 0;
		internal static long NewId {
			get { return curId++; }
		}
		#endregion

		public Type RootType;
		InstanciatorInvoker loader;

		internal string sourcePath;

		#region CTOR
		public Instantiator (string path) : this (Interface.GetStreamFromPath(path)) {
			sourcePath = path;
		}
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
		List<Delegate> cachedDelegates = new List<Delegate>();
		List<int> templateCachedDelegateIndices = new List<int>();//store indices of template delegate to be handled by root parentChanged event
		Delegate templateBinding;

		#region IML parsing
		/// <summary>
		/// Parses IML and build a dynamic method that will be used to instanciate one or multiple occurence of the IML file or fragment
		/// </summary>
		void parseIML (XmlTextReader reader) {
			Context ctx = new Context (findRootType (reader));

			ctx.nodesStack.Push (new Node (ctx.RootType));
			emitLoader (reader, ctx);
			ctx.nodesStack.Pop ();

			foreach (int idx in templateCachedDelegateIndices)
				ctx.emitCachedDelegateHandlerAddition(idx, typeof(GraphicObject).GetEvent("LogicalParentChanged"));

			ctx.ResolveNamedTargets ();

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
		void emitLoader (XmlTextReader reader, Context ctx)
		{
			string tmpXml = reader.ReadOuterXml ();

			if (ctx.nodesStack.Peek().HasTemplate)
				emitTemplateLoad (ctx, tmpXml);

			emitGOLoad (ctx, tmpXml);

			//emitCheckAndBindValueChanged (ctx);
		}
		void emitTemplateLoad (Context ctx, string tmpXml) {
			//if its a template, first read template elements
			using (XmlTextReader reader = new XmlTextReader (tmpXml, XmlNodeType.Element, null)) {
				List<string[]> itemTemplateIds = new List<string[]> ();
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
						readChildren (reader, ctx, -1);
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
								new ItemTemplate (new MemoryStream (Encoding.UTF8.GetBytes (reader.ReadInnerXml ())), dataType, datas);

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
							foreach (string exp in CompilerServices.splitOnSemiColumnOutsideAccolades(reader.Value)) {
								string trimed = exp.Trim();
								if (trimed.StartsWith ("{", StringComparison.OrdinalIgnoreCase))
									compileAndStoreDynHandler (ctx, mi as EventInfo, trimed.Substring (1, trimed.Length - 2));
								else
									emitHandlerBinding (ctx, mi as EventInfo, trimed);
							}

							continue;
						}
						PropertyInfo pi = mi as PropertyInfo;
						if (pi == null)
							throw new Exception ("Member '" + reader.Name + "' is not a property in " + ctx.CurrentNodeType.Name);

						if (pi.Name == "Name")
							ctx.StoreCurrentName (reader.Value);

						if (reader.Value.StartsWith ("{", StringComparison.OrdinalIgnoreCase))
							readPropertyBinding (ctx, reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));
						else
							CompilerServices.EmitSetValue (ctx.il, pi, reader.Value);

					}
					reader.MoveToElement ();
				}
				#endregion

				readChildren (reader, ctx);

				ctx.nodesStack.ResetCurrentNodeIndex ();
			}
		}

		/// <summary>
		/// Parse child node an generate corresponding msil
		/// </summary>
		void readChildren (XmlTextReader reader, Context ctx, int startingIdx = 0)
		{
			bool endTagReached = false;
			int nodeIdx = startingIdx;
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

					ctx.nodesStack.Push (new Node (t, nodeIdx));
					emitLoader (reader, ctx);
					ctx.nodesStack.Pop ();

					ctx.il.Emit (OpCodes.Ldloc_0);//load child on stack for parenting
					ctx.il.Emit (OpCodes.Callvirt, ctx.nodesStack.Peek().GetAddMethod(nodeIdx));
					ctx.il.Emit (OpCodes.Stloc_0); //reset local to current go

					nodeIdx++;

					break;
				}
				if (endTagReached)
					break;
			}
		}
		#endregion

		void readPropertyBinding (Context ctx, string sourceMember, string expression)
		{
			NodeAddress sourceNA = ctx.CurrentNodeAddress;
			BindingDefinition bindingDef = splitBindingExp (sourceNA, sourceMember, expression);

			if (bindingDef.IsDataSourceBinding)//bind on data source
				emitDataSourceBindings (ctx, bindingDef);
			else
				ctx.StorePropertyBinding (bindingDef);
		}

		/// <summary>
		/// Splits the binding expression
		/// </summary>
		/// <returns><c>true</c>, if it's a two way binding, <c>false</c> otherwise.</returns>
		/// <param name="sourceNA">Source Node address</param>
		/// <param name="expression">Expression.</param>
		/// <param name="targetNA">Target Node Address</param>
		/// <param name="targetMember">Target member name</param>
		/// <param name="targetName">Target node name</param>
		BindingDefinition splitBindingExp(NodeAddress sourceNA, string sourceMember, string expression){
			BindingDefinition bindingDef = new BindingDefinition(sourceNA, sourceMember);
			if (string.IsNullOrEmpty (expression)) {
				return bindingDef;
			} else {
				if (expression.StartsWith ("²")) {
					bindingDef.TwoWay = true;
					expression = expression.Substring (1);
				}
				string[] bindingExp = expression.Split ('/');

				if (bindingExp.Length > 1)
					bindingDef.TargetNA = CompilerServices.getNodeAdressFromBindingExp (sourceNA, bindingExp);

				string [] bindTrg = bindingExp.Last().Split ('.');

				if (bindTrg.Length == 1)
					bindingDef.TargetMember = bindTrg [0];
				else if (bindTrg.Length == 2) {
					//named target
					bindingDef.TargetName = bindTrg[0];
					bindingDef.TargetMember = bindTrg [1];
				} else
					throw new Exception ("Syntax error in binding, expected 'go dot member'");
			}

			return bindingDef;
		}

		#region Emit Helper
		void dataSourceChangedEmitHelper(object dscSource, object dataSource, int dynMethIdx){
			if (dataSource is IValueChange)
				(dataSource as IValueChange).ValueChanged +=
					(EventHandler<ValueChangeEventArgs>)dsValueChangedDynMeths [dynMethIdx].CreateDelegate (typeof(EventHandler<ValueChangeEventArgs>), dscSource);
		}
		#endregion

		#region Event Bindings
		/// <summary>
		/// Compile events expression in IML attributes, and store the result in the instanciator
		/// Those handlers will be bound when instatiing
		/// </summary>
		void compileAndStoreDynHandler (Context ctx, EventInfo sourceEvent, string expression)
		{
			//store event handler dynamic method in instanciator
			int dmIdx = cachedDelegates.Count;
			cachedDelegates.Add (CompilerServices.compileDynEventHandler (sourceEvent, expression, ctx.CurrentNodeAddress));
			ctx.emitCachedDelegateHandlerAddition(dmIdx, sourceEvent);
		}
		/// <summary> Emits handler method bindings </summary>
		void emitHandlerBinding (Context ctx, EventInfo sourceEvent, string expression){
			NodeAddress currentNode = ctx.CurrentNodeAddress;
			BindingDefinition bindingDef = splitBindingExp (currentNode, sourceEvent.Name, expression);

			if (bindingDef.IsTemplateBinding | bindingDef.IsDataSourceBinding) {
				//we need to bind datasource method to source event
				DynamicMethod dm = new DynamicMethod ("dyn_dschangedForHandler" + NewId,
					                   typeof(void),
					                   CompilerServices.argsBoundDSChange, true);

				ILGenerator il = dm.GetILGenerator (256);
				System.Reflection.Emit.Label cancel = il.DefineLabel ();

				il.DeclareLocal (typeof(MethodInfo));//used to cancel binding if method doesn't exist

				il.Emit (OpCodes.Nop);

				emitRemoveOldDataSourceHandler (il, sourceEvent.Name, bindingDef.TargetMember, false);

				//fetch method in datasource and test if it exist
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Brfalse, cancel);//cancel if new datasource is null
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Ldstr, bindingDef.TargetMember);//load handler method name
				il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod ("getMethodInfoWithReflexion", BindingFlags.Static | BindingFlags.Public));
				il.Emit (OpCodes.Stloc_0);//save MethodInfo
				il.Emit (OpCodes.Ldloc_0);//push mi for test if null

				il.Emit (OpCodes.Brfalse, cancel);

				il.Emit (OpCodes.Ldarg_1);//load datasource change source where the event is as 1st arg of handler.add
				if (bindingDef.IsTemplateBinding)//fetch source instance with address
					CompilerServices.emitGetInstance (il, bindingDef.SourceNA);

				//load handlerType of sourceEvent to create delegate (1st arg)
				il.Emit (OpCodes.Ldtoken, sourceEvent.EventHandlerType);
				il.Emit (OpCodes.Call, CompilerServices.miGetTypeFromHandle);
				il.Emit (OpCodes.Ldarg_2);//load new datasource where the method is defined
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Ldloc_0);//load methodInfo (3rd arg)

				il.Emit (OpCodes.Callvirt, typeof(Delegate).GetMethod ("CreateDelegate",
					new Type[] { typeof(Type), typeof(object), typeof(MethodInfo) }));//create bound delegate
				il.Emit (OpCodes.Callvirt, sourceEvent.AddMethod);//call add event

				System.Reflection.Emit.Label finish = il.DefineLabel ();
				il.Emit (OpCodes.Br, finish);
				il.MarkLabel (cancel);
				il.EmitWriteLine (string.Format ("Handler method '{0}' for '{1}' not found in new dataSource ", bindingDef.TargetMember, sourceEvent.Name));
				il.MarkLabel (finish);
				il.Emit (OpCodes.Ret);

				//store dschange delegate in instatiator instance for access while instancing graphic object
				int delDSIndex = cachedDelegates.Count;
				cachedDelegates.Add (dm.CreateDelegate (CompilerServices.ehTypeDSChange, this));

				if (bindingDef.IsDataSourceBinding)
					ctx.emitCachedDelegateHandlerAddition (delDSIndex, typeof(GraphicObject).GetEvent ("DataSourceChanged"));
				else //template handler binding, will be added to root parentChanged
					templateCachedDelegateIndices.Add (delDSIndex);
			} else {//normal in tree handler binding, store until tree is complete (end of parse)
				ctx.UnresolvedTargets.Add (new EventBinding (
					bindingDef.SourceNA, sourceEvent,
					bindingDef.TargetNA, bindingDef.TargetMember, bindingDef.TargetName));
			}
		}
		#endregion

		#region Property Bindings
		/// <summary>
		/// Create and store in the instanciator the ValueChanged delegates
		/// those delegates uses grtree functions to set destination value so they don't
		/// need to be bound to destination instance as in the ancient system.
		/// </summary>
		void emitBindingDelegates(Context ctx){
			foreach (KeyValuePair<NodeAddress,Dictionary<string, List<MemberAddress>>> bindings in ctx.Bindings ) {
				if (bindings.Key.Count == 0)//template binding
					emitTemplateBindings (ctx, bindings.Value);
				else
					emitPropertyBindings (ctx,  bindings.Key, bindings.Value);
			}
		}
		void emitPropertyBindings(Context ctx, NodeAddress origine, Dictionary<string, List<MemberAddress>> bindings){
			Type origineNodeType = origine.NodeType;

			//value changed dyn method
			DynamicMethod dm = new DynamicMethod ("dyn_valueChanged" + NewId,
				typeof (void), CompilerServices.argsValueChange, true);
			ILGenerator il = dm.GetILGenerator (256);

			System.Reflection.Emit.Label endMethod = il.DefineLabel ();

			il.DeclareLocal (typeof(object));

			il.Emit (OpCodes.Nop);

			int i = 0;
			foreach (KeyValuePair<string, List<MemberAddress>> bindingCase in bindings ) {

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
				PropertyInfo piOrig = origineNodeType.GetProperty (bindingCase.Key);
				Type origineType = null;
				if (piOrig != null)
					origineType = piOrig.PropertyType;
				foreach (MemberAddress ma in bindingCase.Value) {
					//first we have to load destination instance onto the stack, it is access
					//with graphic tree functions deducted from nodes topology
					il.Emit (OpCodes.Ldarg_0);//load source instance of ValueChanged event

					NodeAddress destination = ma.Address;

					if (destination.Count == 0){//template reverse binding
						//fetch destination instance (which is the template root)
						for (int j = 0; j < origine.Count ; j++)
							il.Emit(OpCodes.Callvirt, typeof(GraphicObject).GetProperty("LogicalParent").GetGetMethod());
					}else
						CompilerServices.emitGetInstance (il, origine, destination);

					if (origineType != null && destination.Count > 0){//else, prop less binding or reverse template bind, no init requiered
						//for initialisation dynmeth, push destination instance loc_0 is root node in ctx
						ctx.il.Emit(OpCodes.Ldloc_0);
						CompilerServices.emitGetInstance (ctx.il, destination);

						//init dynmeth: load actual value from origine
						ctx.il.Emit (OpCodes.Ldloc_0);
						CompilerServices.emitGetInstance (ctx.il, origine);
						ctx.il.Emit (OpCodes.Callvirt, origineNodeType.GetProperty (bindingCase.Key).GetGetMethod());
					}
					//load new value
					il.Emit (OpCodes.Ldarg_1);
					il.Emit (OpCodes.Ldfld, typeof (ValueChangeEventArgs).GetField ("NewValue"));

					if (origineType == null)//property less binding, no init
						CompilerServices.emitConvert (il, ma.Property.PropertyType);
					else if (destination.Count > 0) {
						if (origineType.IsValueType)
							ctx.il.Emit(OpCodes.Box, origineType);

						CompilerServices.emitConvert (ctx.il, origineType, ma.Property.PropertyType);
						CompilerServices.emitConvert (il, origineType, ma.Property.PropertyType);

						ctx.il.Emit (OpCodes.Callvirt, ma.Property.GetSetMethod());//set init value
					} else {// reverse templateBinding
						il.Emit (OpCodes.Ldstr, ma.memberName);//arg 3 of setValueWithReflexion
						il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("setValueWithReflexion", BindingFlags.Static | BindingFlags.Public));
						continue;
					}
					il.Emit (OpCodes.Callvirt, ma.Property.GetSetMethod());//set value on value changes
				}
				#endregion
				il.Emit (OpCodes.Br, endMethod);
				il.MarkLabel (nextTest);

				i++;
			}

			il.MarkLabel (endMethod);
			il.Emit (OpCodes.Ret);

			//store and emit Add in ctx
			int dmIdx = cachedDelegates.Count;
			cachedDelegates.Add (dm.CreateDelegate (typeof(EventHandler<ValueChangeEventArgs>)));
			ctx.emitCachedDelegateHandlerAddition (dmIdx, typeof(IValueChange).GetEvent ("ValueChanged"), origine);
		}
		void emitTemplateBindings(Context ctx, Dictionary<string, List<MemberAddress>> bindings){
			//value changed dyn method
			DynamicMethod dm = new DynamicMethod ("dyn_tmpValueChanged",
				typeof (void), CompilerServices.argsValueChange, true);
			ILGenerator il = dm.GetILGenerator (256);

			//create parentchanged dyn meth in parallel to have only one loop over bindings
			DynamicMethod dmPC = new DynamicMethod ("dyn_InitAndLogicalParentChanged",
				typeof (void),
				CompilerServices.argsBoundDSChange, true);
			ILGenerator ilPC = dmPC.GetILGenerator (256);

			il.Emit (OpCodes.Nop);
			ilPC.Emit (OpCodes.Nop);

			System.Reflection.Emit.Label endMethod = il.DefineLabel ();

			il.DeclareLocal (typeof(object));
			ilPC.DeclareLocal (typeof(object));//used for checking propery less bindings
			ilPC.DeclareLocal (typeof(MemberInfo));//used for checking propery less bindings

			System.Reflection.Emit.Label cancel = ilPC.DefineLabel ();

			#region Unregister previous parent event handler
			//unregister previous parent handler if not null
			ilPC.Emit (OpCodes.Ldarg_2);//load old parent
			ilPC.Emit (OpCodes.Ldfld, typeof (DataSourceChangeEventArgs).GetField ("OldDataSource"));
			ilPC.Emit (OpCodes.Brfalse, cancel);//old parent is null

			ilPC.Emit (OpCodes.Ldarg_2);//load old parent
			ilPC.Emit (OpCodes.Ldfld, typeof (DataSourceChangeEventArgs).GetField ("OldDataSource"));
			//Load cached delegate
			ilPC.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			ilPC.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("templateBinding", BindingFlags.Instance | BindingFlags.NonPublic));

			//add template bindings dynValueChanged delegate to new parent event
			ilPC.Emit(OpCodes.Callvirt, typeof(IValueChange).GetEvent("ValueChanged").RemoveMethod);//call remove event
			#endregion

			ilPC.MarkLabel(cancel);

			#region check if new parent is null
			cancel = ilPC.DefineLabel ();
			ilPC.Emit (OpCodes.Ldarg_2);//load datasource change arg
			ilPC.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
			ilPC.Emit (OpCodes.Brfalse, cancel);//new ds is null
			#endregion

			int i = 0;
			foreach (KeyValuePair<string, List<MemberAddress>> bindingCase in bindings ) {

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
					if (ma.Address.Count == 0){
						Debug.WriteLine("\t\tBUG: reverse template binding in normal template binding");
						continue;//template binding
					}
					//first we try to get memberInfo of new parent, if it doesn't exist, it's a propery less binding
					ilPC.Emit (OpCodes.Ldarg_2);//load new parent onto the stack for handler addition
					ilPC.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
					ilPC.Emit (OpCodes.Stloc_0);//save new parent
					//get parent type
					ilPC.Emit (OpCodes.Ldloc_0);//push parent instance
					ilPC.Emit (OpCodes.Ldstr, bindingCase.Key);//load member name
					ilPC.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("getMemberInfoWithReflexion", BindingFlags.Static | BindingFlags.Public));
					ilPC.Emit (OpCodes.Stloc_1);//save memberInfo
					ilPC.Emit (OpCodes.Ldloc_1);//push mi for test if null
					System.Reflection.Emit.Label propLessReturn = ilPC.DefineLabel ();
					ilPC.Emit (OpCodes.Brfalse, propLessReturn);


					//first we have to load destination instance onto the stack, it is access
					//with graphic tree functions deducted from nodes topology
					il.Emit (OpCodes.Ldarg_0);//load source instance of ValueChanged event
					CompilerServices.emitGetChild (il, typeof(TemplatedControl), -1);
					CompilerServices.emitGetInstance (il, ma.Address);

					ilPC.Emit (OpCodes.Ldarg_2);//load destination instance to set actual value of member
					ilPC.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
					CompilerServices.emitGetChild (ilPC, typeof(TemplatedControl), -1);
					CompilerServices.emitGetInstance (ilPC, ma.Address);

					//load new value
					il.Emit (OpCodes.Ldarg_1);
					il.Emit (OpCodes.Ldfld, typeof (ValueChangeEventArgs).GetField ("NewValue"));

					//for the parent changed dyn meth we need to fetch actual value for initialisation thrue reflexion
					ilPC.Emit (OpCodes.Ldloc_0);//push root instance of instanciator as parentChanged source
					ilPC.Emit (OpCodes.Ldloc_1);//push mi for value fetching
					ilPC.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("getValueWithReflexion", BindingFlags.Static | BindingFlags.Public));

					CompilerServices.emitConvert (il, ma.Property.PropertyType);

					//					//box ValueType
					//					ilPC.Emit (OpCodes.Ldloc_1);//push mi to check if it's a valuetype
					//					ilPC.Emit (OpCodes.Call, typeof(PropertyInfo).GetProperty("PropertyType").GetGetMethod());
					//					ilPC.Emit (OpCodes.Call, typeof(Type).GetProperty("IsValueType").GetGetMethod());
					//					System.Reflection.Emit.Label noBoxingRequired = ilPC.DefineLabel ();
					//					ilPC.Emit (OpCodes.Brfalse, noBoxingRequired);

					CompilerServices.emitConvert (ilPC, ma.Property.PropertyType);

					il.Emit (OpCodes.Callvirt, ma.Property.GetSetMethod());
					ilPC.Emit (OpCodes.Callvirt, ma.Property.GetSetMethod());

					ilPC.MarkLabel(propLessReturn);
				}
				#endregion
				il.Emit (OpCodes.Br, endMethod);
				il.MarkLabel (nextTest);

				i++;
			}
			//il.Emit (OpCodes.Pop);
			il.MarkLabel (endMethod);
			il.Emit (OpCodes.Ret);

			//store template bindings in instanciator
			templateBinding = dm.CreateDelegate (typeof(EventHandler<ValueChangeEventArgs>));

			#region emit LogicalParentChanged method

			//load new parent onto the stack for handler addition
			ilPC.Emit (OpCodes.Ldarg_2);
			ilPC.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);

			//Load cached delegate
			ilPC.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			ilPC.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("templateBinding", BindingFlags.Instance | BindingFlags.NonPublic));

			//add template bindings dynValueChanged delegate to new parent event
			ilPC.Emit(OpCodes.Callvirt, typeof(IValueChange).GetEvent("ValueChanged").AddMethod);//call add event

			ilPC.MarkLabel (cancel);
			ilPC.Emit (OpCodes.Ret);

			//store dschange delegate in instatiator instance for access while instancing graphic object
			int delDSIndex = cachedDelegates.Count;
			cachedDelegates.Add(dmPC.CreateDelegate (CompilerServices.ehTypeDSChange, this));
			#endregion

			ctx.emitCachedDelegateHandlerAddition(delDSIndex, typeof(GraphicObject).GetEvent("LogicalParentChanged"));
		}
		/// <summary>
		/// create the valuechanged handler, the datasourcechanged handler and emit event handling
		/// </summary>
		void emitDataSourceBindings(Context ctx, BindingDefinition bindingDef){
			DynamicMethod dm = null;
			ILGenerator il = null;
			int dmVC = 0;
			PropertyInfo piSource = ctx.CurrentNodeType.GetProperty(bindingDef.SourceMember);
			//if no dataSource member name is provided, valuechange is not handle and datasource change
			//will be used as origine value
			string delName = "dyn_DSvalueChanged" + NewId;
			if (!string.IsNullOrEmpty(bindingDef.TargetMember)){
				#region create valuechanged method
				dm = new DynamicMethod (delName,
					typeof (void),
					CompilerServices.argsBoundValueChange, true);

				il = dm.GetILGenerator (256);

				System.Reflection.Emit.Label endMethod = il.DefineLabel ();

				il.DeclareLocal (typeof(object));

				il.Emit (OpCodes.Nop);

				//load value changed member name onto the stack
				il.Emit (OpCodes.Ldarg_2);
				il.Emit (OpCodes.Ldfld, CompilerServices.fiMbName);

				//test if it's the expected one
				il.Emit (OpCodes.Ldstr, bindingDef.TargetMember);
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

				CompilerServices.emitConvert (il, piSource.PropertyType);

				il.Emit (OpCodes.Callvirt, piSource.GetSetMethod ());

				il.MarkLabel (endMethod);
				il.Emit (OpCodes.Ret);

				//vc dyn meth is stored in a cached list, it will be bound to datasource only
				//when datasource of source graphic object changed
				dmVC = dsValueChangedDynMeths.Count;
				dsValueChangedDynMeths.Add (dm);
				#endregion
			}

			#region emit dataSourceChanged event handler
			//now we create the datasource changed method that will init the destination member with
			//the actual value of the origin member of the datasource and then will bind the value changed
			//dyn methode.
			//dm is bound to the instanciator instance to have access to cached dyn meth and delegates
			dm = new DynamicMethod ("dyn_dschanged",
				typeof (void),
				CompilerServices.argsBoundDSChange, true);

			il = dm.GetILGenerator (256);

			il.DeclareLocal (typeof(object));//used for checking propery less bindings
			il.DeclareLocal (typeof(MemberInfo));//used for checking propery less bindings
			System.Reflection.Emit.Label cancel = il.DefineLabel ();
			System.Reflection.Emit.Label cancelInit = il.DefineLabel ();

			il.Emit (OpCodes.Nop);

			emitRemoveOldDataSourceHandler(il, "ValueChanged", delName);

			if (!string.IsNullOrEmpty(bindingDef.TargetMember)){
				if (bindingDef.TwoWay){
					System.Reflection.Emit.Label cancelRemove = il.DefineLabel ();
					//remove handler if not null
					il.Emit (OpCodes.Ldarg_2);//load old parent
					il.Emit (OpCodes.Ldfld, typeof (DataSourceChangeEventArgs).GetField ("OldDataSource"));
					il.Emit (OpCodes.Brfalse, cancelRemove);//old parent is null

					//remove handler
					il.Emit (OpCodes.Ldarg_2);//1st arg load old datasource
					il.Emit (OpCodes.Ldfld, typeof (DataSourceChangeEventArgs).GetField ("OldDataSource"));
					il.Emit (OpCodes.Ldstr, "ValueChanged");//2nd arg event name
					il.Emit (OpCodes.Ldarg_1);//3d arg: instance bound to delegate (the source)
					il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("RemoveEventHandlerByTarget", BindingFlags.Static | BindingFlags.Public));
					il.MarkLabel(cancelRemove);
				}
				il.Emit (OpCodes.Ldarg_2);//load datasource change arg
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Brfalse, cancel);//new ds is null
			}

			#region fetch initial Value
			if (!string.IsNullOrEmpty(bindingDef.TargetMember)){
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Ldstr, bindingDef.TargetMember);//load member name
				il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("getMemberInfoWithReflexion", BindingFlags.Static | BindingFlags.Public));
				il.Emit (OpCodes.Stloc_1);//save memberInfo
				il.Emit (OpCodes.Ldloc_1);//push mi for test if null
				il.Emit (OpCodes.Brfalse, cancelInit);//propertyLessBinding
			}

			il.Emit (OpCodes.Ldarg_1);//load source of dataSourceChanged which is the dest instance
			il.Emit (OpCodes.Ldarg_2);//load new datasource
			il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
			if (!string.IsNullOrEmpty(bindingDef.TargetMember)){
				il.Emit (OpCodes.Ldloc_1);//push mi for value fetching
				il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("getValueWithReflexion", BindingFlags.Static | BindingFlags.Public));
			}
			CompilerServices.emitConvert (il, piSource.PropertyType);
			il.Emit (OpCodes.Callvirt, piSource.GetSetMethod ());
			#endregion

			if (!string.IsNullOrEmpty(bindingDef.TargetMember)){
				il.MarkLabel(cancelInit);
				//check if new dataSource implement IValueChange
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Isinst, typeof(IValueChange));
				il.Emit (OpCodes.Brfalse, cancel);

				il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
				il.Emit (OpCodes.Ldarg_1);//load datasource change source
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit(OpCodes.Ldc_I4, dmVC);//load index of dynmathod
				il.Emit (OpCodes.Call, typeof(Instantiator).GetMethod("dataSourceChangedEmitHelper", BindingFlags.Instance | BindingFlags.NonPublic));

				if (bindingDef.TwoWay){
					il.Emit (OpCodes.Ldarg_1);//arg1: dataSourceChange source, the origine of the binding
					il.Emit (OpCodes.Ldstr, bindingDef.SourceMember);//arg2: orig member
					il.Emit (OpCodes.Ldarg_2);//arg3: new datasource
					il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
					il.Emit (OpCodes.Ldstr, bindingDef.TargetMember);//arg4: dest member
					il.Emit (OpCodes.Call, typeof(Instantiator).GetMethod("dataSourceReverseBinding", BindingFlags.Static | BindingFlags.NonPublic));
				}

				il.MarkLabel (cancel);
			}
			il.Emit (OpCodes.Ret);

			//store dschange delegate in instatiator instance for access while instancing graphic object
			int delDSIndex = cachedDelegates.Count;
			cachedDelegates.Add(dm.CreateDelegate (CompilerServices.ehTypeDSChange, this));
			#endregion

			ctx.emitCachedDelegateHandlerAddition(delDSIndex, typeof(GraphicObject).GetEvent("DataSourceChanged"));
		}
		/// <summary>
		/// Two way binding for datasource, graphicObj=>dataSource link, datasource value has priority
		/// and will be set as init for source property (in emitDataSourceBindings func)
		/// </summary>
		/// <param name="orig">Graphic object instance, source of binding</param>
		/// <param name="origMember">Origin member name</param>
		/// <param name="dest">datasource instance, target of the binding</param>
		/// <param name="destMember">Destination member name</param>
		static void dataSourceReverseBinding(IValueChange orig, string origMember, object dest, string destMember){
			Type tOrig = orig.GetType ();
			Type tDest = dest.GetType ();
			PropertyInfo piOrig = tOrig.GetProperty (origMember);
			PropertyInfo piDest = tDest.GetProperty (destMember);

			#region ValueChanged emit
			DynamicMethod dm = new DynamicMethod ("dyn_valueChanged" + NewId,
				typeof (void), CompilerServices.argsBoundValueChange, true);
			ILGenerator il = dm.GetILGenerator (256);

			System.Reflection.Emit.Label endMethod = il.DefineLabel ();

			il.DeclareLocal (typeof(object));
			il.Emit (OpCodes.Nop);

			//load value changed member name onto the stack
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldfld, CompilerServices.fiMbName);

			//test if it's the expected one
			il.Emit (OpCodes.Ldstr, origMember);
			il.Emit (OpCodes.Ldc_I4_4);//StringComparison.Ordinal
			il.Emit (OpCodes.Callvirt, CompilerServices.stringEquals);
			il.Emit (OpCodes.Brfalse, endMethod);
			//set destination member with valueChanged new value
			//load destination ref
			il.Emit (OpCodes.Ldarg_0);
			//load new value onto the stack
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldfld, CompilerServices.fiNewValue);

			CompilerServices.emitConvert (il, piOrig.PropertyType, piDest.PropertyType);

			il.Emit (OpCodes.Callvirt, piDest.GetSetMethod ());

			il.MarkLabel (endMethod);
			il.Emit (OpCodes.Ret);
			#endregion

			orig.ValueChanged += (EventHandler<ValueChangeEventArgs>)dm.CreateDelegate (typeof(EventHandler<ValueChangeEventArgs>), dest);
		}
		#endregion

		/// <summary> Emits remove old data source event handler./summary>
		void emitRemoveOldDataSourceHandler(ILGenerator il, string eventName, string delegateName, bool DSSide = true){
			System.Reflection.Emit.Label cancel = il.DefineLabel ();

			il.Emit (OpCodes.Ldarg_2);//load old parent
			il.Emit (OpCodes.Ldfld, typeof (DataSourceChangeEventArgs).GetField ("OldDataSource"));
			il.Emit (OpCodes.Brfalse, cancel);//old parent is null

			//remove handler
			if (DSSide){//event is defined in the dataSource instance
				il.Emit (OpCodes.Ldarg_2);//1st arg load old datasource
				il.Emit (OpCodes.Ldfld, typeof (DataSourceChangeEventArgs).GetField ("OldDataSource"));
			}else//the event is in the source
				il.Emit (OpCodes.Ldarg_1);//1st arg load old datasource
			il.Emit (OpCodes.Ldstr, eventName);//2nd arg event name
			il.Emit (OpCodes.Ldstr, delegateName);//3d arg: delegate name
			il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("RemoveEventHandlerByName", BindingFlags.Static | BindingFlags.Public));
			il.MarkLabel(cancel);
		}

	}
}

