//
// Instantiator.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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

namespace Crow.IML
{
	public class InstantiatorException : Exception {
		public string Path;
		public InstantiatorException (string path, Exception innerException)
			: base ("ITor error:" + path, innerException){
			Path = path;
		}
	}
	public delegate object InstanciatorInvoker(Interface iface);

	/// <summary>
	/// Reflexion being very slow, the settings of the starting values for widgets are set by a dynamic method.
	/// This method is created on the first instacing and is recalled for further widget instancing.
	/// 
	/// It includes:
	/// 	- XML values setting
	/// 	- Default values (appearing as attribute in C#)  loading
	/// 	- Styling
	/// 
	/// Instantiators are shared amongs interfaces. Their are stored with their path as key, and inlined template
	/// and itemtemplate are stored with a generated uuid
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
		protected Interface iface;

		internal string sourcePath;

		public static int NextInstantiatorID = 0;
		public int currentInstantiatorID = 0;
		int currentDesignID = 0;
		internal string NextDesignID { get { return string.Format ("{0}_{1}",currentInstantiatorID, currentDesignID++); }}

		#region CTOR
		/// <summary>
		/// Initializes a new instance of the Instantiator class.
		/// </summary>
		public Instantiator (Interface _iface, string path) : this (_iface, _iface.GetStreamFromPath(path), path) {
			
		}
		/// <summary>
		/// Initializes a new instance of the Instantiator class.
		/// </summary>
		public Instantiator (Interface _iface, Stream stream, string srcPath = null)
		{
			currentInstantiatorID = NextInstantiatorID++;
			iface = _iface;
			sourcePath = srcPath;
			#if DEBUG_LOAD
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif
			try {
				using (XmlReader itr = XmlReader.Create (stream)) {
					parseIML (itr);
				}
				stream.Dispose ();
			} catch (Exception ex) {
				throw new InstantiatorException(sourcePath, ex);
			} finally {
				#if DEBUG_LOAD
				loadingTime.Stop ();
				Debug.WriteLine ("IML Instantiator creation '{2}' : {0} ticks, {1} ms",
				loadingTime.ElapsedTicks, loadingTime.ElapsedMilliseconds, sourcePath);
				#endif
			}
		}
		/// <summary>
		/// Initializes a new instance of the Instantiator class with an already openned xml reader
		/// positionned on the start tag inside the itemTemplate
		/// </summary>
		public Instantiator (Interface _iface, XmlReader itr){
			currentInstantiatorID = NextInstantiatorID++;
			iface = _iface;
			parseIML (itr);
		}
		//TODO:check if still used
		public Instantiator (Interface _iface, Type _root, InstanciatorInvoker _loader)
		{
			currentInstantiatorID = NextInstantiatorID++;
			iface = _iface;
			RootType = _root;
			loader = _loader;
		}
		/// <summary>
		/// Create a new instantiator from IML fragment provided directely as a string
		/// </summary>
		/// <returns>A new instantiator</returns>
		/// <param name="fragment">IML string</param>
		public static Instantiator CreateFromImlFragment (Interface _iface, string fragment)
		{
			using (Stream s = new MemoryStream (Encoding.UTF8.GetBytes (fragment))) {
				return new Instantiator (_iface, s);
			}
		}
		#endregion

		/// <summary>
		/// Creates a new instance of the GraphicObject compiled in the instantiator
		/// and bind it the an interface
		/// </summary>
		/// <returns>The new graphic object instance</returns>
		/// <param name="iface">The interface to bind to</param>
		public GraphicObject CreateInstance(){
			return loader (iface) as GraphicObject;
		}
		/// <summary>
		/// Creates a new instance of T compiled in the instantiator
		/// and bind it the an interface
		/// </summary>
		/// <returns>The new T instance</returns>
		/// <param name="iface">The interface to bind to</param>
		public T CreateInstance<T>(){
			return (T)loader (iface);
		}
		List<DynamicMethod> dsValueChangedDynMeths = new List<DynamicMethod>();
		List<Delegate> cachedDelegates = new List<Delegate>();
		/// <summary>
		/// store indices of template delegate to be handled by root parentChanged event
		/// </summary>
		List<int> templateCachedDelegateIndices = new List<int>();
		/// <summary>
		/// Store template bindings in the instantiator
		/// </summary>
		Delegate templateBinding;

		#region IML parsing
		/// <summary>
		/// Parses IML and build a dynamic method that will be used to instantiate one or multiple occurences of the IML file or fragment
		/// </summary>
		void parseIML (XmlReader reader) {
			IMLContext ctx = new IMLContext (findRootType (reader));

			ctx.nodesStack.Push (new Node (ctx.RootType));
			emitLoader (reader, ctx);
			ctx.nodesStack.Pop ();

			foreach (int idx in templateCachedDelegateIndices)
				ctx.emitCachedDelegateHandlerAddition(idx, CompilerServices.eiLogicalParentChanged);

			ctx.ResolveNamedTargets ();

			emitBindingDelegates (ctx);

			ctx.il.Emit (OpCodes.Ldloc_0);//load root obj to return
			ctx.il.Emit(OpCodes.Ret);

			reader.Read ();//close tag
			RootType = ctx.RootType;
			loader = (InstanciatorInvoker)ctx.dm.CreateDelegate (typeof (InstanciatorInvoker), this);
		}
		/// <summary>
		/// read first node to set GraphicObject class for loading
		/// and let reader position on that node
		/// </summary>
		Type findRootType (XmlReader reader)
		{
			string root = "Object";
			while (reader.NodeType != XmlNodeType.Element)
				reader.Read ();
			root = reader.Name;
			Type t = tryGetGOType (root);
			if (t == null)
				throw new Exception ("IML parsing error: undefined root type (" + root + ")");
			return t;
		}
		/// <summary>
		/// main parsing entry point
		/// </summary>
		void emitLoader (XmlReader reader, IMLContext ctx)
		{
			int curLine = ctx.curLine;

			#if DESIGN_MODE
			IXmlLineInfo li = (IXmlLineInfo)reader;
			ctx.curLine += li.LineNumber - 1;
			#endif

			string tmpXml = reader.ReadOuterXml ();

			if (ctx.nodesStack.Peek().HasTemplate)
				emitTemplateLoad (ctx, tmpXml);

			emitGOLoad (ctx, tmpXml);

			ctx.curLine = curLine;
			//emitCheckAndBindValueChanged (ctx);
		}
		/// <summary>
		/// Parses the item template tag.
		/// </summary>
		/// <returns>the string triplet dataType, itemTmpID read as attribute of this tag</returns>
		/// <param name="reader">current xml text reader</param>
		/// <param name="itemTemplatePath">file containing the templates if its a dedicated one</param>
		string[] parseItemTemplateTag (XmlReader reader, string itemTemplatePath = "") {
			string dataType = "default", datas = "", path = "", dataTest = "TypeOf";
			while (reader.MoveToNextAttribute ()) {
				if (reader.Name == "DataType")
					dataType = reader.Value;
				else if (reader.Name == "Data")
					datas = reader.Value;
				else if (reader.Name == "Path")
					path = reader.Value;
				else if (reader.Name == "DataTest")
					dataTest = reader.Value;
			}
			reader.MoveToElement ();

			string itemTmpID = itemTemplatePath;

			if (string.IsNullOrEmpty (path)) {
				itemTmpID += Guid.NewGuid ().ToString ();
				iface.Instantiators [itemTmpID] =
					new ItemTemplate (iface, new MemoryStream (Encoding.UTF8.GetBytes (reader.ReadInnerXml ())), dataTest, dataType, datas);

			} else {
				if (!reader.IsEmptyElement)
					throw new Exception ("ItemTemplate with Path attribute set may not include sub nodes");
				itemTmpID += path+dataType+datas;
				if (!iface.Instantiators.ContainsKey (itemTmpID))
					iface.Instantiators [itemTmpID] =
						new ItemTemplate (iface, path, dataTest, dataType, datas);
			}
			return new string [] { dataType, itemTmpID, datas, dataTest };
		}
		/// <summary>
		/// process template and item template definition prior to
		/// other attributes or childs processing
		/// </summary>
		/// <param name="ctx">Loading Context</param>
		/// <param name="tmpXml">xml fragment</param>
		void emitTemplateLoad (IMLContext ctx, string tmpXml) {
			//if its a template, first read template elements
			using (XmlTextReader reader = new XmlTextReader (tmpXml, XmlNodeType.Element, null)) {
				List<string[]> itemTemplateIds = new List<string[]> ();
				bool inlineTemplate = false;

				reader.Read ();
				string templatePath = reader.GetAttribute ("Template");
				string itemTemplatePath = reader.GetAttribute ("ItemTemplate");

				int depth = reader.Depth + 1;
				while (reader.Read ()) {
					if (!reader.IsStartElement () || reader.Depth > depth)
						continue;
					if (reader.Name == "Template") {
						inlineTemplate = true;
						#if DESIGN_MODE
						ctx.il.Emit (OpCodes.Ldloc_0);
						ctx.il.Emit (OpCodes.Ldc_I4_1);
						ctx.il.Emit (OpCodes.Stfld, typeof(TemplatedControl).GetField("design_inlineTemplate"));
						#endif
						reader.Read ();
						readChildren (reader, ctx, -1);
					} else if (reader.Name == "ItemTemplate")
						itemTemplateIds.Add (parseItemTemplateTag (reader));					
				}

				if (!inlineTemplate) {//load from path or default template
					ctx.il.Emit (OpCodes.Ldloc_0);//Load  current templatedControl ref
					ctx.il.Emit (OpCodes.Ldarg_1);//load currentInterface
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
				if (itemTemplateIds.Count == 0) {
					//try to load ItemTemplate(s) from ItemTemplate attribute of TemplatedGroup
					if (!string.IsNullOrEmpty (itemTemplatePath)) {
						//check if it is already loaded in cache as a single itemTemplate instantiator
						if (iface.Instantiators.ContainsKey (itemTemplatePath)) {
							itemTemplateIds.Add (new string [] { "default", itemTemplatePath, "" });
						} else {
							using (Stream stream = iface.GetStreamFromPath (itemTemplatePath)) {
								//itemtemplate files may have multiple root nodes
								XmlReaderSettings itrSettings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
								using (XmlReader itr = XmlReader.Create (stream, itrSettings)) {									
									while (itr.Read ()) {
										if (!itr.IsStartElement ())
											continue;
										if (itr.NodeType == XmlNodeType.Element) {
											if (itr.Name != "ItemTemplate") {
												//the file contains a single template to use as default
												iface.Instantiators [itemTemplatePath] =
													new ItemTemplate (iface, itr);
												itemTemplateIds.Add (new string [] { "default", itemTemplatePath, "", "TypeOf" });
												break;//we should be at the end of the file
											}
											itemTemplateIds.Add (parseItemTemplateTag (itr, itemTemplatePath));
										}
									}
								}
							}
						}
					}
				}
				if (!ctx.nodesStack.Peek ().IsTemplatedGroup)
					return;
				//add the default item template if no default is defined
				if (!itemTemplateIds.Any(ids=>ids[0] == "default"))
					itemTemplateIds.Add (new string [] { "default", "#Crow.DefaultItem.template", "", "TypeOf"});
				//get item templates 
				foreach (string [] iTempId in itemTemplateIds) {
					ctx.il.Emit (OpCodes.Ldloc_0);//load TempControl ref
					ctx.il.Emit (OpCodes.Ldfld, CompilerServices.fldItemTemplates);//load ItemTemplates dic field

					//prepare argument to add itemTemplate to templated group dic of ItemTemplates
					ctx.il.Emit (OpCodes.Ldstr, iTempId [0]);//load key
					//load itemTemplate
					ctx.il.Emit (OpCodes.Ldarg_1);//load currentInterface
					ctx.il.Emit (OpCodes.Ldstr, iTempId [1]);//load path
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

		#if DESIGN_MODE
		void emitSetDesignAttribute (IMLContext ctx, string name, string value){
			//store member value in iml
			ctx.il.Emit (OpCodes.Ldloc_0);
			ctx.il.Emit (OpCodes.Ldfld, typeof(GraphicObject).GetField("design_iml_values"));
			ctx.il.Emit (OpCodes.Ldstr, name);
			if (string.IsNullOrEmpty (value))
				ctx.il.Emit (OpCodes.Ldnull);
			else
				ctx.il.Emit (OpCodes.Ldstr, value);
			ctx.il.Emit (OpCodes.Call, CompilerServices.miDicStrStrAdd);
		}
		#endif

		/// <summary>
		/// process styling, attributes and children loading.
		/// </summary>
		/// <param name="ctx">parsing context</param>
		/// <param name="tmpXml">xml fragment</param>
		void emitGOLoad (IMLContext ctx, string tmpXml) {
			using (XmlTextReader reader = new XmlTextReader (tmpXml, XmlNodeType.Element, null)) {
				reader.Read ();

				ctx.il.Emit (OpCodes.Ldloc_0);
				ctx.il.Emit (OpCodes.Ldstr, this.NextDesignID);
				ctx.il.Emit (OpCodes.Stfld, typeof(GraphicObject).GetField("design_id"));

				#if DESIGN_MODE
				IXmlLineInfo li = (IXmlLineInfo)reader;
				ctx.il.Emit (OpCodes.Ldloc_0);
				ctx.il.Emit (OpCodes.Ldc_I4, ctx.curLine + li.LineNumber);
				ctx.il.Emit (OpCodes.Stfld, typeof(GraphicObject).GetField("design_line"));
				ctx.il.Emit (OpCodes.Ldloc_0);
				ctx.il.Emit (OpCodes.Ldc_I4, li.LinePosition);
				ctx.il.Emit (OpCodes.Stfld, typeof(GraphicObject).GetField("design_column"));
				if (!string.IsNullOrEmpty (sourcePath)) {
					ctx.il.Emit (OpCodes.Ldloc_0);
					ctx.il.Emit (OpCodes.Ldstr, sourcePath);
					ctx.il.Emit (OpCodes.Stfld, typeof(GraphicObject).GetField("design_imlPath"));
				}
				#endif

				#region Styling and default values loading
				//first check for Style attribute then trigger default value loading
				if (reader.HasAttributes) {
					string style = reader.GetAttribute ("Style");
					if (!string.IsNullOrEmpty (style)){
						CompilerServices.EmitSetValue (ctx.il, CompilerServices.piStyle, style);
						#if DESIGN_MODE
						emitSetDesignAttribute (ctx, "Style", style);
						#endif
					}
				}
				ctx.il.Emit (OpCodes.Ldloc_0);
				ctx.il.Emit (OpCodes.Ldarg_1);//load currentInterface
				ctx.il.Emit (OpCodes.Callvirt, CompilerServices.miLoadDefaultVals);
				#endregion

				#region Attributes reading
				if (reader.HasAttributes) {

					while (reader.MoveToNextAttribute ()) {
						if (reader.Name == "Style")
							continue;

						#if DESIGN_MODE
						emitSetDesignAttribute (ctx, reader.Name, reader.Value);
						#endif

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
		void readChildren (XmlTextReader reader, IMLContext ctx, int startingIdx = 0)
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

					Type t = tryGetGOType (reader.Name);
					if (t == null)
						throw new Exception (reader.Name + " type not found");
					ConstructorInfo ci = t.GetConstructor (
						                     BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,  
						                     null, Type.EmptyTypes, null);
					if (ci == null)
						throw new Exception ("No default parameterless constructor found in " + t.Name);					
					ctx.il.Emit (OpCodes.Newobj, ci);
					ctx.il.Emit (OpCodes.Stloc_0);//child is now loc_0
					//CompilerServices.emitSetCurInterface (ctx.il);

					ctx.nodesStack.Push (new Node (t, nodeIdx));
					emitLoader (reader, ctx);
					ctx.nodesStack.Pop ();

					MethodInfo miAdd = ctx.nodesStack.Peek ().GetAddMethod (nodeIdx);
					if (miAdd == CompilerServices.miLoadTmp)
						ctx.il.Emit (OpCodes.Ldarg_1);//push cur iface if tmp load
					ctx.il.Emit (OpCodes.Ldloc_0);//load child on stack for parenting
					ctx.il.Emit (OpCodes.Callvirt, miAdd);
					ctx.il.Emit (OpCodes.Stloc_0); //reset local to current go

					nodeIdx++;

					break;
				}
				if (endTagReached)
					break;
			}
		}
		#endregion

		void readPropertyBinding (IMLContext ctx, string sourceMember, string expression)
		{
			NodeAddress sourceNA = ctx.CurrentNodeAddress;
			BindingDefinition bindingDef = sourceNA.GetBindingDef (sourceMember, expression);

			#if DEBUG_BINDING
			Debug.WriteLine("Property Binding: " + bindingDef.ToString());
			#endif

			if (bindingDef.IsDataSourceBinding)//bind on data source
				emitDataSourceBindings (ctx, bindingDef);
			else
				ctx.StorePropertyBinding (bindingDef);
		}

		#region Emit Helper
		void dataSourceChangedEmitHelper(object dscSource, object dataSource, int dynMethIdx){
			if (dataSource is IValueChange)
				(dataSource as IValueChange).ValueChanged +=
					(EventHandler<ValueChangeEventArgs>)dsValueChangedDynMeths [dynMethIdx].CreateDelegate (typeof(EventHandler<ValueChangeEventArgs>), dscSource);
		}
		/// <summary> Emits remove old data source event handler./summary>
		void emitRemoveOldDataSourceHandler(ILGenerator il, string eventName, string delegateName, bool DSSide = true){
			System.Reflection.Emit.Label cancel = il.DefineLabel ();

			il.Emit (OpCodes.Ldarg_2);//load old parent
			il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCOldDS);
			il.Emit (OpCodes.Brfalse, cancel);//old parent is null

			//remove handler
			if (DSSide){//event is defined in the dataSource instance
				il.Emit (OpCodes.Ldarg_2);//1st arg load old datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCOldDS);
			}else//the event is in the source
				il.Emit (OpCodes.Ldarg_1);//1st arg load old datasource
			il.Emit (OpCodes.Ldstr, eventName);//2nd arg event name
			il.Emit (OpCodes.Ldstr, delegateName);//3d arg: delegate name
			il.Emit (OpCodes.Call, CompilerServices.miRemEvtHdlByName);
			il.MarkLabel(cancel);
		}
		#endregion

		#region Event Bindings
		/// <summary>
		/// Compile events expression in IML attributes, and store the result in the instanciator
		/// Those handlers will be bound when instatiing
		/// </summary>
		void compileAndStoreDynHandler (IMLContext ctx, EventInfo sourceEvent, string expression)
		{
			//store event handler dynamic method in instanciator
			int dmIdx = cachedDelegates.Count;
			cachedDelegates.Add (CompilerServices.compileDynEventHandler (sourceEvent, expression, ctx.CurrentNodeAddress));
			ctx.emitCachedDelegateHandlerAddition(dmIdx, sourceEvent);
		}
		/// <summary> Emits handler method bindings </summary>
		void emitHandlerBinding (IMLContext ctx, EventInfo sourceEvent, string expression){
			NodeAddress currentNode = ctx.CurrentNodeAddress;
			BindingDefinition bindingDef = currentNode.GetBindingDef (sourceEvent.Name, expression);

			#if DEBUG_BINDING
			Debug.WriteLine("Event Binding: " + bindingDef.ToString());
			#endif

			if (bindingDef.IsTemplateBinding | bindingDef.IsDataSourceBinding) {
				//we need to bind datasource method to source event
				DynamicMethod dm = new DynamicMethod ("dyn_dsORtmpChangedForHandler" + NewId,
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
				il.Emit (OpCodes.Call, CompilerServices.miGetMethInfoWithRefx);
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

				il.Emit (OpCodes.Callvirt, CompilerServices.miCreateBoundDel);
				il.Emit (OpCodes.Callvirt, sourceEvent.AddMethod);//call add event

				System.Reflection.Emit.Label finish = il.DefineLabel ();
				il.Emit (OpCodes.Br, finish);
				il.MarkLabel (cancel);
				#if DEBUG_BINDING
				il.EmitWriteLine (string.Format ("Handler method '{0}' for '{1}' NOT FOUND in new dataSource", bindingDef.TargetMember, sourceEvent.Name));
				#endif
				il.MarkLabel (finish);
				#if DEBUG_BINDING
				il.EmitWriteLine (string.Format ("Handler method '{0}' for '{1}' FOUND in new dataSource", bindingDef.TargetMember, sourceEvent.Name));
				#endif
				il.Emit (OpCodes.Ret);

				//store dschange delegate in instatiator instance for access while instancing graphic object
				int delDSIndex = cachedDelegates.Count;
				cachedDelegates.Add (dm.CreateDelegate (CompilerServices.ehTypeDSChange, this));

				if (bindingDef.IsDataSourceBinding)
					ctx.emitCachedDelegateHandlerAddition (delDSIndex, CompilerServices.eiDSChange);
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
		void emitBindingDelegates(IMLContext ctx){
			foreach (KeyValuePair<NodeAddress,Dictionary<string, List<MemberAddress>>> bindings in ctx.Bindings ) {
				if (bindings.Key.Count == 0)//template binding
					emitTemplateBindings (ctx, bindings.Value);
				else
					emitPropertyBindings (ctx,  bindings.Key, bindings.Value);
			}
		}
		void emitPropertyBindings(IMLContext ctx, NodeAddress origine, Dictionary<string, List<MemberAddress>> bindings){
			Type origineNodeType = origine.NodeType;

			//value changed dyn method
			DynamicMethod dm = new DynamicMethod ("dyn_valueChanged" + NewId,
				typeof (void), CompilerServices.argsValueChange, true);
			ILGenerator il = dm.GetILGenerator (256);

			System.Reflection.Emit.Label endMethod = il.DefineLabel ();

			il.DeclareLocal (CompilerServices.TObject);

			il.Emit (OpCodes.Nop);

			int i = 0;
			foreach (KeyValuePair<string, List<MemberAddress>> bindingCase in bindings ) {

				System.Reflection.Emit.Label nextTest = il.DefineLabel ();

				#region member name test
				//load source member name
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Ldfld, CompilerServices.fiVCMbName);

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
							il.Emit(OpCodes.Callvirt, CompilerServices.miGetLogicalParent);
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
					il.Emit (OpCodes.Ldfld, CompilerServices.fiVCNewValue);

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
						il.Emit (OpCodes.Call, CompilerServices.miSetValWithRefx);
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
			ctx.emitCachedDelegateHandlerAddition (dmIdx, CompilerServices.eiValueChange, origine);

			#if DEBUG_BINDING
			Debug.WriteLine("\tCrow property binding: " + dm.Name);
			#endif

		}
		void emitTemplateBindings(IMLContext ctx, Dictionary<string, List<MemberAddress>> bindings){
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

			il.DeclareLocal (CompilerServices.TObject);
			ilPC.DeclareLocal (CompilerServices.TObject);//used for checking propery less bindings
			ilPC.DeclareLocal (typeof(MemberInfo));//used for checking propery less bindings

			System.Reflection.Emit.Label cancel = ilPC.DefineLabel ();

			#region Unregister previous parent event handler
			//unregister previous parent handler if not null
			ilPC.Emit (OpCodes.Ldarg_2);//load old parent
			ilPC.Emit (OpCodes.Ldfld, CompilerServices.fiDSCOldDS);
			ilPC.Emit (OpCodes.Brfalse, cancel);//old parent is null

			ilPC.Emit (OpCodes.Ldarg_2);//load old parent
			ilPC.Emit (OpCodes.Ldfld, CompilerServices.fiDSCOldDS);
			//Load cached delegate
			ilPC.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			ilPC.Emit(OpCodes.Ldfld, CompilerServices.fiTemplateBinding);

			//add template bindings dynValueChanged delegate to new parent event
			ilPC.Emit(OpCodes.Callvirt, CompilerServices.eiValueChange.RemoveMethod);//call remove event
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
				il.Emit (OpCodes.Ldfld, CompilerServices.fiVCMbName);

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
					ilPC.Emit (OpCodes.Call, CompilerServices.miGetMembIinfoWithRefx);
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
					il.Emit (OpCodes.Ldfld, CompilerServices.fiVCNewValue);

					//for the parent changed dyn meth we need to fetch actual value for initialisation thrue reflexion
					ilPC.Emit (OpCodes.Ldloc_0);//push root instance of instanciator as parentChanged source
					ilPC.Emit (OpCodes.Ldloc_1);//push mi for value fetching
					ilPC.Emit (OpCodes.Call, CompilerServices.miGetValWithRefx);

					CompilerServices.emitConvert (il, ma.Property.PropertyType);
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
			ilPC.Emit(OpCodes.Ldfld, CompilerServices.fiTemplateBinding);

			//add template bindings dynValueChanged delegate to new parent event
			ilPC.Emit(OpCodes.Callvirt, CompilerServices.eiValueChange.AddMethod);//call add event

			ilPC.MarkLabel (cancel);
			ilPC.Emit (OpCodes.Ret);

			//store dschange delegate in instatiator instance for access while instancing graphic object
			int delDSIndex = cachedDelegates.Count;
			cachedDelegates.Add(dmPC.CreateDelegate (CompilerServices.ehTypeDSChange, this));
			#endregion

			ctx.emitCachedDelegateHandlerAddition(delDSIndex, CompilerServices.eiLogicalParentChanged);
		}
		/// <summary>
		/// create the valuechanged handler, the datasourcechanged handler and emit event handling
		/// </summary>
		void emitDataSourceBindings(IMLContext ctx, BindingDefinition bindingDef){
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

				il.DeclareLocal (CompilerServices.TObject);

				il.Emit (OpCodes.Nop);

				//load value changed member name onto the stack
				il.Emit (OpCodes.Ldarg_2);
				il.Emit (OpCodes.Ldfld, CompilerServices.fiVCMbName);

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
				il.Emit (OpCodes.Ldfld, CompilerServices.fiVCNewValue);

				//by default, source value type is deducted from target member type to allow
				//memberless binding, if targetMember exists, it will be used to determine target
				//value type for conversion

				CompilerServices.emitConvert (il, piSource.PropertyType);

				if (!piSource.CanWrite)
					throw new Exception ("Source member of bindind is read only:" + piSource.ToString());

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
			dm = new DynamicMethod ("dyn_dschanged" + NewId,
				typeof (void),
				CompilerServices.argsBoundDSChange, true);

			il = dm.GetILGenerator (256);

			il.DeclareLocal (CompilerServices.TObject);//used for checking propery less bindings
			il.DeclareLocal (typeof(MemberInfo));//used for checking propery less bindings
			System.Reflection.Emit.Label cancel = il.DefineLabel ();
			System.Reflection.Emit.Label newDSIsNull = il.DefineLabel ();
			System.Reflection.Emit.Label cancelInit = il.DefineLabel ();

			il.Emit (OpCodes.Nop);

			emitRemoveOldDataSourceHandler(il, "ValueChanged", delName, true);

			if (!string.IsNullOrEmpty(bindingDef.TargetMember)){
				if (bindingDef.TwoWay){
//					System.Reflection.Emit.Label cancelRemove = il.DefineLabel ();
//					//remove handler if not null
//					il.Emit (OpCodes.Ldarg_2);//load old parent
//					il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCOldDS);
//					il.Emit (OpCodes.Brfalse, cancelRemove);//old parent is null

					//remove handler
					emitRemoveOldDataSourceHandler(il, "ValueChanged", delName, false);

//					il.Emit (OpCodes.Ldarg_1);//3d arg: instance bound to delegate (the source)
//					il.Emit (OpCodes.Ldstr, "ValueChanged");//2nd arg event name
//					il.Emit (OpCodes.Ldarg_2);//1st arg load old datasource
//					il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCOldDS);
//					il.Emit (OpCodes.Call, CompilerServices.miRemEvtHdlByTarget);
//					il.MarkLabel(cancelRemove);
				}
				il.Emit (OpCodes.Ldarg_2);//load datasource change arg
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Brfalse, newDSIsNull);//new ds is null
			}

			#region fetch initial Value
			if (!string.IsNullOrEmpty(bindingDef.TargetMember)){
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Ldstr, bindingDef.TargetMember);//load member name
				il.Emit (OpCodes.Call, CompilerServices.miGetMembIinfoWithRefx);
				il.Emit (OpCodes.Stloc_1);//save memberInfo
				il.Emit (OpCodes.Ldloc_1);//push mi for test if null
				il.Emit (OpCodes.Brfalse, cancelInit);//propertyLessBinding
			}

			il.Emit (OpCodes.Ldarg_1);//load source of dataSourceChanged which is the dest instance
			il.Emit (OpCodes.Ldarg_2);//load new datasource
			il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
			if (!string.IsNullOrEmpty(bindingDef.TargetMember)){
				il.Emit (OpCodes.Ldloc_1);//push mi for value fetching
				il.Emit (OpCodes.Call, CompilerServices.miGetValWithRefx);
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
				il.Emit (OpCodes.Call, CompilerServices.miDSChangeEmitHelper);

				il.MarkLabel (cancel);

				if (bindingDef.TwoWay){
					il.Emit (OpCodes.Ldarg_1);//arg1: dataSourceChange source, the origine of the binding
					il.Emit (OpCodes.Ldstr, bindingDef.SourceMember);//arg2: orig member
					il.Emit (OpCodes.Ldarg_2);//arg3: new datasource
					il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
					il.Emit (OpCodes.Ldstr, bindingDef.TargetMember);//arg4: dest member
					il.Emit (OpCodes.Call, CompilerServices.miDSReverseBinding);
				}

			}
			il.MarkLabel (newDSIsNull);
			il.Emit (OpCodes.Ret);

			//store dschange delegate in instatiator instance for access while instancing graphic object
			int delDSIndex = cachedDelegates.Count;
			cachedDelegates.Add(dm.CreateDelegate (CompilerServices.ehTypeDSChange, this));
			#endregion

			ctx.emitCachedDelegateHandlerAddition(delDSIndex, CompilerServices.eiDSChange);

			#if DEBUG_BINDING
			Debug.WriteLine("\tDataSource ValueChanged: " + delName);
			Debug.WriteLine("\tDataSource Changed: " + dm.Name);
			#endif
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

			if (piDest == null) {
				Debug.WriteLine ("Member '{0}' not found in new DataSource '{1}' of '{2}'", destMember, dest, orig);
				return;
			}
			#if DEBUG_BINDING
			Debug.WriteLine ("DS Reverse binding: Member '{0}' found in new DS '{1}' of '{2}'", destMember, dest, orig);
			#endif

			#region ValueChanged emit
			DynamicMethod dm = new DynamicMethod ("dyn_valueChanged" + NewId,
				typeof (void), CompilerServices.argsBoundValueChange, true);
			ILGenerator il = dm.GetILGenerator (256);

			System.Reflection.Emit.Label endMethod = il.DefineLabel ();

			il.DeclareLocal (CompilerServices.TObject);
			il.Emit (OpCodes.Nop);

			//load value changed member name onto the stack
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldfld, CompilerServices.fiVCMbName);

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
			il.Emit (OpCodes.Ldfld, CompilerServices.fiVCNewValue);

			CompilerServices.emitConvert (il, piOrig.PropertyType, piDest.PropertyType);

			il.Emit (OpCodes.Callvirt, piDest.GetSetMethod ());

			il.MarkLabel (endMethod);
			il.Emit (OpCodes.Ret);
			#endregion

			orig.ValueChanged += (EventHandler<ValueChangeEventArgs>)dm.CreateDelegate (typeof(EventHandler<ValueChangeEventArgs>), dest);
		}
		#endregion

		/// <summary>
		/// search for graphic object type in crow assembly, if not found,
		/// search for type independently of namespace in all the loaded assemblies
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <returns>the corresponding type object</returns>
		/// <param name="typeName">graphic object type name without its namespace</param>
		Type tryGetGOType (string typeName){
			Type t = Type.GetType ("Crow." + typeName);
			if (t != null)
				return t;			
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
				if (a.IsDynamic)
					continue;
				foreach (Type expT in a.GetExportedTypes ()) {
					if (expT.Name == typeName)
						return expT;
				}
			}
			return null;
		}
	}
}

