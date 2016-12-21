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
		static long curId = 0;
		internal static long NewId {
			get { return curId++; }
		}
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
		Dictionary<string, Delegate> bindingDelegates = new Dictionary<string, Delegate>();//valuechanged del
		Dictionary<string, Delegate> bindingInitializer = new Dictionary<string, Delegate>();//initialize with actual values of binding origine
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

			emitCheckAndBindValueChanged (ctx);
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
							foreach (string exp in splitOnSemiColumnOutsideAccolades(reader.Value)) {
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

		string[] splitOnSemiColumnOutsideAccolades (string expression){
			List<String> exps = new List<string>();
			int accCount = 0;
			int expPtr = 0;
			for (int c = 0; c < expression.Length; c++) {
				switch (expression[c]){
				case '{':
					accCount++;
					break;
				case '}':
					accCount--;
					break;
				case ';':
					if (accCount > 0)
						break;
					exps.Add(expression.Substring(expPtr, c - expPtr - 1));
					expPtr = c + 1;
					break;
				}
			}
			if (exps.Count == 0)
				exps.Add(expression);
			return exps.ToArray ();
		}
		void readPropertyBinding (Context ctx, string sourceMember, string expression)
		{
			NodeAddress sourceNA = ctx.CurrentNodeAddress;
			BindingDefinition bindingDef = splitBindingExp (sourceNA, sourceMember, expression);

			if (bindingDef.IsDataSourceBinding)//bind on data source
				emitDataSourceBindingDelegate (ctx, sourceMember, bindingDef.TargetMember);
			else
				ctx.StorePropertyBinding (bindingDef);
		}
		/// <summary>
		/// Gets the node adress from binding expression splitted with '/' starting at a given node
		/// </summary>
		NodeAddress getNodeAdressFromBindingExp(NodeAddress sourceAddr, string[] bindingExp){
			int ptr = sourceAddr.Count - 1;

			//if exp start with '/' => Graphic tree parsing start at source
			if (string.IsNullOrEmpty (bindingExp [0])) {
				//TODO:
			} else if (bindingExp [0] == ".") { //search template root
				ptr--;
				while (ptr >= 0) {
					if (typeof(TemplatedControl).IsAssignableFrom (sourceAddr [ptr].CrowType))
						break;
					ptr--;
				}
			} else if (bindingExp [0] == "..") { //search starting at current node
				int levelUp = bindingExp.Length - 1;
				if (levelUp > ptr)
					throw new Exception ("Binding error: try to bind outside IML source");
				ptr -= levelUp;
			}
			Node[] targetNode = new Node[ptr+1];
			Array.Copy (sourceAddr.ToArray (), targetNode, ptr + 1);
			return new NodeAddress (targetNode);
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
					bindingDef.TargetNA = getNodeAdressFromBindingExp (sourceNA, bindingExp);

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
		/// Compile events expression in IML attributes, and store the result in the instanciator
		/// Those handlers will be bound when instatiing
		/// </summary>
		void compileAndStoreDynHandler (Context ctx, EventInfo sourceEvent, string expression)
		{
			#if DEBUG_BINDING
			Debug.WriteLine ("\tCompile Event Source ");
			#endif


			#region Retrieve EventHandler parameter type
			MethodInfo evtInvoke = sourceEvent.EventHandlerType.GetMethod ("Invoke");
			ParameterInfo [] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;
			#endregion

			Type [] args = { typeof (object), handlerArgsType };
			DynamicMethod dm = new DynamicMethod ("dyn_eventHandler",
				                   typeof(void),
				                   args, true);


			#region IL generation
			NodeAddress currentNode = ctx.CurrentNodeAddress;
			string strNA = currentNode.ToString();

			ILGenerator il = dm.GetILGenerator (256);
			il.Emit (OpCodes.Nop);

			string [] srcLines = expression.Trim ().Split (new char [] { ';' });

			foreach (string srcLine in srcLines) {
				string statement = srcLine.Trim ();

				string [] operandes = statement.Split (new char [] { '=' });
				if (operandes.Length < 2) //not an affectation
				{
					//maybe we could handle here handler function name
					continue;
				}

				string rop = operandes [operandes.Length - 1].Trim ();

				#region LEFT OPERANDES
				string [] lopParts = operandes [0].Trim ().Split ('/');
				Type lopType = currentNode.NodeType;
				MemberInfo lopMI = null;

				il.Emit (OpCodes.Ldarg_0);  //load sender ref onto the stack

				if (lopParts.Length > 1) {
					NodeAddress lopNA = getNodeAdressFromBindingExp (currentNode, lopParts);
					emitGetInstance (il, currentNode, lopNA);
					lopType = lopNA.NodeType;
				}

				string [] bindTrg = lopParts.Last().Split ('.');

				if (bindTrg.Length == 1)
					lopMI = lopType.GetMember (bindTrg [0]).FirstOrDefault();
				else if (bindTrg.Length == 2) {
					//named target
					//TODO:
					il.Emit(OpCodes.Ldstr, bindTrg[0]);
					il.Emit(OpCodes.Callvirt, typeof(GraphicObject).GetMethod("FindByName"));
					lopMI = lopType.GetMember (bindTrg [1]).FirstOrDefault();
				} else
					throw new Exception ("Syntax error in binding, expected 'go dot member'");


				if (lopMI == null)
					throw new Exception (string.Format ("IML BINDING: Member not found"));

				OpCode lopSetOpCode;
				dynamic lopSetMI;
				Type lopT = null;
				switch (lopMI.MemberType) {
				case MemberTypes.Property:
					lopSetOpCode = OpCodes.Callvirt;
					PropertyInfo lopPi = lopMI as PropertyInfo;
					lopT = lopPi.PropertyType;
					lopSetMI = lopPi.GetSetMethod ();
					break;
				case MemberTypes.Field:
					lopSetOpCode = OpCodes.Stfld;
					FieldInfo dstFi = lopMI as FieldInfo;
					lopT = dstFi.FieldType;
					lopSetMI = dstFi;
					break;
				default:
					throw new Exception (string.Format ("GOML:member type not handle"));
				}
				#endregion

				#region RIGHT OPERANDES
				if (rop.StartsWith ("\'")) {
					if (!rop.EndsWith ("\'"))
						throw new Exception (string.Format
							("GOML:malformed string constant in handler: {0}", rop));
					string strcst = rop.Substring (1, rop.Length - 2);

					il.Emit (OpCodes.Ldstr, strcst);

				} else {
					if (lopT.IsEnum)
						throw new NotImplementedException ();

					MethodInfo lopParseMi = lopT.GetMethod ("Parse");
					if (lopParseMi == null)
						throw new Exception (string.Format
							("GOML:no parse method found in: {0}", lopT.Name));
					il.Emit (OpCodes.Ldstr, rop);
					il.Emit (OpCodes.Callvirt, lopParseMi);
					il.Emit (OpCodes.Unbox_Any, lopT);
				}

				#endregion

				//emit left operand assignment
				il.Emit (lopSetOpCode, lopSetMI);
			}

			il.Emit (OpCodes.Ret);

			#endregion

			//store event handler dynamic method in instanciator
			int dmIdx = cachedDelegates.Count;
			cachedDelegates.Add (dm.CreateDelegate (sourceEvent.EventHandlerType));
			ctx.emitCachedDelegateHandlerAddition(dmIdx, sourceEvent);
		}
		void emitHandlerBinding (Context ctx, EventInfo sourceEvent, string expression){
			NodeAddress currentNode = ctx.CurrentNodeAddress;
			BindingDefinition bindingDef = splitBindingExp (currentNode, sourceEvent.Name, expression);

			string bindOnEventName = null;

			if (bindingDef.TargetNA == null)//datasource handler
				bindOnEventName = "DataSourceChanged";
			else if (bindingDef.TargetNA.Count == 0)//out of tree template handler
				bindOnEventName = "ParentChanged";

			if (!string.IsNullOrEmpty(bindOnEventName)){
				//we need to bind datasource method to source event
				DynamicMethod dm = new DynamicMethod ("dyn_dschangedForHandler",
					typeof (void),
					CompilerServices.argsDSChange, true);

				ILGenerator il = dm.GetILGenerator (256);

				il.DeclareLocal (typeof(MethodInfo));//used to cancel binding if method doesn't exist

				il.Emit (OpCodes.Nop);

				//fetch method in datasource and test if it exist
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Ldstr, bindingDef.TargetMember);//load handler method name
				il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("getMethodInfoWithReflexion", BindingFlags.Static | BindingFlags.Public));
				il.Emit (OpCodes.Stloc_0);//save MethodInfo
				il.Emit (OpCodes.Ldloc_0);//push mi for test if null
				System.Reflection.Emit.Label methodNotFound = il.DefineLabel ();
				il.Emit (OpCodes.Brfalse, methodNotFound);

				il.Emit (OpCodes.Ldarg_1);//load datasource change source where the event handler is as 1st arg of handler.add

				//loat handlerType of sourceEvent to create delegate (1st arg)
				il.Emit(OpCodes.Ldtoken, sourceEvent.EventHandlerType);
				il.Emit (OpCodes.Call, CompilerServices.miGetTypeFromHandle);
				il.Emit (OpCodes.Ldarg_2);//load new datasource where the method is defined
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Ldloc_0);//load methodInfo (3rd arg)

				il.Emit (OpCodes.Callvirt, typeof(Delegate).GetMethod ("CreateDelegate",
					new Type[] {typeof(Type), typeof(object), typeof(MethodInfo)}));//create bound delegate
				il.Emit(OpCodes.Callvirt, sourceEvent.AddMethod);//call add event

				il.MarkLabel(methodNotFound);
				il.Emit (OpCodes.Ret);

				//store dschange delegate in instatiator instance for access while instancing graphic object
				int delDSIndex = cachedDelegates.Count;
				cachedDelegates.Add(dm.CreateDelegate (CompilerServices.ehTypeDSChange, this));

				ctx.emitCachedDelegateHandlerAddition(delDSIndex, typeof(GraphicObject).GetEvent(bindOnEventName));
			}
		}
		/// <summary>
		/// Create and store in the instanciator the ValueChanged delegates
		/// those delegates uses grtree functions to set destination value so they don't
		/// need to be bound to destination instance as in the ancient system.
		/// </summary>
		void emitBindingDelegates(Context ctx){
			foreach (KeyValuePair<NodeAddress,Dictionary<string, List<MemberAddress>>> bindings in ctx.Bindings ) {
				if (bindings.Key.Count == 0)
					emitTemplateBindingDelegate (ctx, bindings.Value);
				else
					emitBindingDelegate (bindings.Key, bindings.Value);
			}
		}
		void emitTemplateBindingDelegate(Context ctx, Dictionary<string, List<MemberAddress>> bindings){
			//value changed dyn method
			DynamicMethod dm = new DynamicMethod ("dyn_tmpValueChanged",
				typeof (void), CompilerServices.argsValueChange, true);
			ILGenerator il = dm.GetILGenerator (256);

			//create parentchanged dyn meth in parallel to have only one loop over bindings
			DynamicMethod dmPC = new DynamicMethod ("dyn_InitAndParentChanged",
				typeof (void),
				CompilerServices.argsDSChange, true);
			ILGenerator ilPC = dmPC.GetILGenerator (256);

			il.Emit (OpCodes.Nop);
			ilPC.Emit (OpCodes.Nop);


			System.Reflection.Emit.Label endMethod = il.DefineLabel ();

			il.DeclareLocal (typeof(object));
			ilPC.DeclareLocal (typeof(object));//used for checking propery less bindings
			ilPC.DeclareLocal (typeof(MemberInfo));//used for checking propery less bindings

			//first check if null
			System.Reflection.Emit.Label cancel = ilPC.DefineLabel ();
			ilPC.Emit (OpCodes.Ldarg_2);//load new parent
			ilPC.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
			ilPC.Emit (OpCodes.Brfalse, cancel);

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
					ilPC.Emit (OpCodes.Ldarg_2);//load destination instance to set actual value of member
					ilPC.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
					emitGetChild (il, typeof(TemplatedControl), -1);
					emitGetInstance (il, ma.Address);
					emitGetChild (ilPC, typeof(TemplatedControl), -1);
					emitGetInstance (ilPC, ma.Address);

					//load new value
					il.Emit (OpCodes.Ldarg_1);
					il.Emit (OpCodes.Ldfld, typeof (ValueChangeEventArgs).GetField ("NewValue"));

					//for the parent changed dyn meth we need to fetch actual value for initialisation thrue reflexion
					ilPC.Emit (OpCodes.Ldloc_0);//push root instance of instanciator as parentChanged source
					ilPC.Emit (OpCodes.Ldloc_1);//push mi for value fetching
					ilPC.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("getValueWithReflexion", BindingFlags.Static | BindingFlags.Public));

					emitConvert (il, ma.Property.PropertyType);

					emitConvert (ilPC, ma.Property.PropertyType);

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

			#region emit ParentChanged handler

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

			ctx.emitCachedDelegateHandlerAddition(delDSIndex, typeof(GraphicObject).GetEvent("ParentChanged"));
		}
		void emitBindingDelegate(NodeAddress origine, Dictionary<string, List<MemberAddress>> bindings){
			Type origineNodeType = origine.NodeType;

			//init method, current instance passed as arg
			DynamicMethod dmInit = new DynamicMethod ("dyn_initBinding_" + NewId.ToString(),
				typeof (void), new Type[] {typeof(object)}, true);
			ILGenerator ilInit = dmInit.GetILGenerator (256);
			ilInit.Emit (OpCodes.Nop);

			//value changed dyn method
			DynamicMethod dm = new DynamicMethod ("dyn_valueChanged",
				typeof (void), CompilerServices.argsValueChange, true);

			ILGenerator il = dm.GetILGenerator (256);

			System.Reflection.Emit.Label endMethod = il.DefineLabel ();

			il.DeclareLocal (typeof(object));
			ilInit.DeclareLocal (typeof(object));

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

					emitGetInstance (il, origine, destination);

					if (origineType != null){//prop less binding, no init requiered
						//for initialisation dynmeth, load current instance
						ilInit.Emit(OpCodes.Ldarg_0);
						emitGetInstance (ilInit, origine, destination);

						//init dynmeth: load actual value
						ilInit.Emit (OpCodes.Ldarg_0);
						ilInit.Emit (OpCodes.Callvirt, origineNodeType.GetProperty (bindingCase.Key).GetGetMethod());
					}
					//load new value
					il.Emit (OpCodes.Ldarg_1);
					il.Emit (OpCodes.Ldfld, typeof (ValueChangeEventArgs).GetField ("NewValue"));

					if (origineType == null)//property less binding, no init
						emitConvert (il, ma.Property.PropertyType);
					else {
						if (origineType.IsValueType)
							ilInit.Emit(OpCodes.Box, origineType);
						emitConvert (ilInit, origineType, ma.Property.PropertyType);
						emitConvert (il, origineType, ma.Property.PropertyType);

						ilInit.Emit (OpCodes.Callvirt, ma.Property.GetSetMethod());//set init value
					}
					il.Emit (OpCodes.Callvirt, ma.Property.GetSetMethod());//set value on value changes

				}
				#endregion
				il.Emit (OpCodes.Br, endMethod);
				il.MarkLabel (nextTest);

				i++;
			}

			ilInit.Emit (OpCodes.Ret);
			il.MarkLabel (endMethod);
			il.Emit (OpCodes.Ret);

			bindingDelegates [origine.ToString()] = dm.CreateDelegate (typeof(EventHandler<ValueChangeEventArgs>));
			bindingInitializer [origine.ToString()] = dmInit.CreateDelegate (typeof(Action<object>));
		}
		/// <summary>
		/// create the valuechanged handler, the datasourcechanged handler and emit event handling
		/// </summary>
		void emitDataSourceBindingDelegate(Context ctx, string sourceMember, string dataSourceMember){
			DynamicMethod dm = null;
			ILGenerator il = null;
			int dmVC = 0;
			PropertyInfo piOrigine = ctx.CurrentNodeType.GetProperty(sourceMember);
			//if no dataSource member name is provided, valuechange is not handle and datasource change
			//will be used as origine value
			if (!string.IsNullOrEmpty(dataSourceMember)){
				#region create valuechanged method
				dm = new DynamicMethod ("dyn_DSvalueChanged",
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

				emitConvert (il, piOrigine.PropertyType);

				il.Emit (OpCodes.Callvirt, piOrigine.GetSetMethod ());

				il.MarkLabel (endMethod);
				il.Emit (OpCodes.Ret);

				//vc dyn meth is stored in a cached list, it will be bound to datasource only
				//when datasource of source graphic object changed
				dmVC = dsValueChangedDynMeths.Count;
				//Delegate tmp = dm.CreateDelegate(typeof(EventHandler<ValueChangeEventArgs>), this);
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
				CompilerServices.argsDSChange, true);

			il = dm.GetILGenerator (256);

			il.DeclareLocal (typeof(object));//used for checking propery less bindings
			il.DeclareLocal (typeof(MemberInfo));//used for checking propery less bindings
			System.Reflection.Emit.Label cancel = il.DefineLabel ();
			System.Reflection.Emit.Label cancelInit = il.DefineLabel ();

			il.Emit (OpCodes.Nop);

			if (!string.IsNullOrEmpty(dataSourceMember)){
				//first test if datasource is null
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Brfalse, cancel);
			}

			//fetch initial Value
			if (!string.IsNullOrEmpty(dataSourceMember)){
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit (OpCodes.Ldstr, dataSourceMember);//load member name
				il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("getMemberInfoWithReflexion", BindingFlags.Static | BindingFlags.Public));
				il.Emit (OpCodes.Stloc_1);//save memberInfo
				il.Emit (OpCodes.Ldloc_1);//push mi for test if null
				il.Emit (OpCodes.Brfalse, cancelInit);
			}

			il.Emit (OpCodes.Ldarg_1);//load source of dataSourceChanged which is the dest instance
			il.Emit (OpCodes.Ldarg_2);//load new datasource
			il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
			if (!string.IsNullOrEmpty(dataSourceMember)){
				il.Emit (OpCodes.Ldloc_1);//push mi for value fetching
				il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod("getValueWithReflexion", BindingFlags.Static | BindingFlags.Public));
			}
			emitConvert (il, piOrigine.PropertyType);
			il.Emit (OpCodes.Callvirt, piOrigine.GetSetMethod ());

			if (!string.IsNullOrEmpty(dataSourceMember)){
				il.MarkLabel(cancelInit);

				il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
				il.Emit (OpCodes.Ldarg_1);//load datasource change source
				il.Emit (OpCodes.Ldarg_2);//load new datasource
				il.Emit (OpCodes.Ldfld, CompilerServices.fiDSCNewDS);
				il.Emit(OpCodes.Ldc_I4, dmVC);//load index of dynmathod
				il.Emit (OpCodes.Call, typeof(Instantiator).GetMethod("dataSourceChangedEmitHelper", BindingFlags.Instance | BindingFlags.NonPublic));

				il.MarkLabel (cancel);
			}
			il.Emit (OpCodes.Ret);

			//store dschange delegate in instatiator instance for access while instancing graphic object
			int delDSIndex = cachedDelegates.Count;
			cachedDelegates.Add(dm.CreateDelegate (CompilerServices.ehTypeDSChange, this));
			#endregion

			ctx.emitCachedDelegateHandlerAddition(delDSIndex, typeof(GraphicObject).GetEvent("DataSourceChanged"));
		}

		void emitGetInstance (ILGenerator il, NodeAddress orig, NodeAddress dest){
			if (orig.Count < dest.Count) {
				for (int i = orig.Count - 1; i < dest.Count - 1; i++)
					emitGetChild (il, dest [i].CrowType, dest [i + 1].Index);
			} else {
				for (int j = dest.Count; j < orig.Count; j++)
					il.Emit (OpCodes.Callvirt, typeof(ILayoutable).GetProperty ("Parent").GetGetMethod ());
			}
		}
		void emitGetInstance (ILGenerator il, NodeAddress dest){
			for (int i = 0; i < dest.Count - 1; i++)
				emitGetChild (il, dest [i].CrowType, dest [i + 1].Index);
		}
		void emitGetChild(ILGenerator il, Type parentType, int index){
			if (typeof (Group).IsAssignableFrom (parentType)) {
				il.Emit (OpCodes.Ldfld, typeof(Group).GetField ("children", BindingFlags.Instance | BindingFlags.NonPublic));
				il.Emit(OpCodes.Ldc_I4, index);
				il.Emit (OpCodes.Callvirt, typeof(List<GraphicObject>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
				return;
			}
			if (typeof(Container).IsAssignableFrom (parentType) || index < 0) {
				il.Emit (OpCodes.Ldfld, typeof(PrivateContainer).GetField ("child", BindingFlags.Instance | BindingFlags.NonPublic));
				return;
			}
			if (typeof(TemplatedContainer).IsAssignableFrom (parentType)) {
				il.Emit (OpCodes.Callvirt, typeof(TemplatedContainer).GetProperty ("Content").GetGetMethod ());
				return;
			}
			if (typeof(TemplatedGroup).IsAssignableFrom (parentType)) {
				il.Emit (OpCodes.Callvirt, typeof(TemplatedGroup).GetProperty ("Items").GetGetMethod ());
				il.Emit(OpCodes.Ldc_I4, index);
				il.Emit (OpCodes.Callvirt, typeof(List<GraphicObject>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
				return;
			}
		}
		/// <summary>
		/// Emit conversion from orig type to dest type
		/// </summary>
		void emitConvert(ILGenerator il, Type origType, Type destType){
			if (destType == typeof(string))
				il.Emit (OpCodes.Callvirt, CompilerServices.miObjToString);
			else if (origType.IsValueType) {
				if (destType != origType)
					il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (destType));
				else
					il.Emit (OpCodes.Unbox_Any, destType);//TODO:double check this
			} else {
				if (origType.IsAssignableFrom(destType))
					il.Emit (OpCodes.Castclass, destType);
				else {
					MethodInfo miIO = getImplicitOp (origType, destType);
					if (miIO != null)
						il.Emit (OpCodes.Callvirt, miIO);
				}
			}
		}
		/// <summary>
		/// check type of current object on the stack and convert to dest type,
		/// use loc_0 so store it as object!!!
		/// </summary>
		void emitConvert(ILGenerator il, Type dstType){
			System.Reflection.Emit.Label endConvert = il.DefineLabel ();
			System.Reflection.Emit.Label convert = il.DefineLabel ();

			il.Emit (OpCodes.Dup);
			il.Emit (OpCodes.Isinst, dstType);
			il.Emit (OpCodes.Brfalse, convert);

			if (dstType.IsValueType)
				il.Emit (OpCodes.Unbox_Any, dstType);
			else
				il.Emit (OpCodes.Castclass, dstType);
			il.Emit (OpCodes.Br, endConvert);

			il.MarkLabel (convert);

			if (dstType == typeof(string)) {
				il.Emit (OpCodes.Callvirt, CompilerServices.miObjToString);
			} else if (dstType.IsPrimitive) {
				//il.Emit (OpCodes.Unbox_Any, dstType);
				il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (dstType));
			} else if (dstType.IsValueType) {
				il.Emit (OpCodes.Unbox_Any, dstType);
			} else{
				il.Emit (OpCodes.Stloc_0); //save orig value in loc0
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Callvirt, typeof(object).GetMethod ("GetType"));
				il.Emit (OpCodes.Ldtoken, dstType);//push destination property type for testing
				il.Emit (OpCodes.Call, CompilerServices.miGetTypeFromHandle);
				il.Emit (OpCodes.Call, typeof(Instantiator).GetMethod ("getImplicitOp", BindingFlags.Static | BindingFlags.Public));
				il.Emit (OpCodes.Dup);
				convert = il.DefineLabel ();
				il.Emit (OpCodes.Brtrue, convert);
				il.Emit (OpCodes.Pop);
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Isinst, dstType);
				il.Emit (OpCodes.Br, endConvert);

				il.MarkLabel (convert);
				il.Emit (OpCodes.Ldnull);//null instance for invoke
				il.Emit (OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Newarr, typeof(object));
				il.Emit (OpCodes.Dup);//duplicate the array ref
				il.Emit (OpCodes.Ldc_I4_0);//push the index 0
				il.Emit (OpCodes.Ldloc_0);//push the orig value to convert
				il.Emit (OpCodes.Stelem, typeof(object));//set the array element at index 0
				il.Emit (OpCodes.Callvirt, typeof(MethodInfo).GetMethod("Invoke", new Type[] { typeof(object), typeof (object[])}));
			}

			il.MarkLabel (endConvert);
		}

		/// <summary>
		/// search for an implicit conversion method in origine or destination classes
		/// </summary>
		public static MethodInfo getImplicitOp(Type origType, Type dstType){
			foreach(MethodInfo mi in origType.GetMethods(BindingFlags.Public|BindingFlags.Static)){
				if (mi.Name == "op_Implicit") {
					if (mi.ReturnType == dstType && mi.GetParameters ().FirstOrDefault ().ParameterType == origType)
						return mi;
				}
			}
			foreach(MethodInfo mi in dstType.GetMethods(BindingFlags.Public|BindingFlags.Static)){
				if (mi.Name == "op_Implicit") {
					if (mi.ReturnType == dstType && mi.GetParameters ().FirstOrDefault ().ParameterType == origType)
						return mi;
				}
			}
			return null;
		}
	}
}

