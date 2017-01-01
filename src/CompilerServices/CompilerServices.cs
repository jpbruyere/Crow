using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using Crow.IML;


namespace Crow
{
	public static class CompilerServices
	{
		internal static Type TObject = typeof(object);
		internal static MethodInfo stringEquals = typeof (string).GetMethod("Equals", new Type [3] { typeof (string), typeof (string), typeof (StringComparison) });
		internal static MethodInfo miObjToString = typeof(object).GetMethod("ToString");
		internal static MethodInfo miGetType = typeof(object).GetMethod("GetType");
		internal static MethodInfo miParseEnum = typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public,
			Type.DefaultBinder, new Type [] {typeof (Type), typeof (string), typeof (bool)}, null);

		internal static MethodInfo miGetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
		internal static MethodInfo miGetEvent = typeof(Type).GetMethod("GetEvent", new Type[] {typeof(string)});

		internal static MethodInfo miMIInvoke = typeof(MethodInfo).GetMethod ("Invoke", new Type[] {
			typeof(object),
			typeof(object[])
		});

		internal static MethodInfo miCreateBoundDel = typeof(Delegate).GetMethod ("CreateDelegate", new Type[] { typeof(Type), typeof(object), typeof(MethodInfo) });//create bound delegate
		internal static MethodInfo miGetColCount = typeof(System.Collections.ICollection).GetProperty("Count").GetGetMethod();
		internal static MethodInfo miGetDelegateListItem = typeof(List<Delegate>).GetMethod("get_Item", new Type[] { typeof(Int32) });

		internal static MethodInfo miCompileDynEventHandler = typeof(CompilerServices).GetMethod ("compileDynEventHandler", BindingFlags.Static | BindingFlags.Public);
		internal static MethodInfo miRemEvtHdlByName = typeof(CompilerServices).GetMethod("RemoveEventHandlerByName", BindingFlags.Static | BindingFlags.Public);
		internal static MethodInfo miRemEvtHdlByTarget = typeof(CompilerServices).GetMethod("RemoveEventHandlerByTarget", BindingFlags.Static | BindingFlags.Public);
		internal static MethodInfo miGetMethInfoWithRefx = typeof(CompilerServices).GetMethod ("getMethodInfoWithReflexion", BindingFlags.Static | BindingFlags.Public);
		internal static MethodInfo miGetMembIinfoWithRefx = typeof(CompilerServices).GetMethod("getMemberInfoWithReflexion", BindingFlags.Static | BindingFlags.Public);
		internal static MethodInfo miSetValWithRefx = typeof(CompilerServices).GetMethod("setValueWithReflexion", BindingFlags.Static | BindingFlags.Public);
		internal static MethodInfo miGetValWithRefx = typeof(CompilerServices).GetMethod("getValueWithReflexion", BindingFlags.Static | BindingFlags.Public);
		internal static MethodInfo miCreateDel = typeof(CompilerServices).GetMethod ("createDel", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miGetImplOp = typeof(CompilerServices).GetMethod ("getImplicitOp", BindingFlags.Static | BindingFlags.Public);

		internal static FieldInfo fiCachedDel = typeof(Instantiator).GetField("cachedDelegates", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static FieldInfo fiTemplateBinding = typeof(Instantiator).GetField("templateBinding", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static MethodInfo miDSChangeEmitHelper = typeof(Instantiator).GetMethod("dataSourceChangedEmitHelper", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static MethodInfo miDSReverseBinding = typeof(Instantiator).GetMethod("dataSourceReverseBinding", BindingFlags.Static | BindingFlags.NonPublic);

		internal static FieldInfo miSetCurIface = typeof(GraphicObject).GetField ("currentInterface", BindingFlags.NonPublic | BindingFlags.Instance);
		internal static MethodInfo miFindByName = typeof (GraphicObject).GetMethod ("FindByName");
		internal static MethodInfo miGetGObjItem = typeof(List<GraphicObject>).GetMethod("get_Item", new Type[] { typeof(Int32) });
		internal static MethodInfo miLoadDefaultVals = typeof (GraphicObject).GetMethod ("loadDefaultValues");
		internal static PropertyInfo piStyle = typeof (GraphicObject).GetProperty ("Style");
		internal static MethodInfo miGetLogicalParent = typeof(GraphicObject).GetProperty("LogicalParent").GetGetMethod();
		internal static MethodInfo miGetDataSource = typeof(GraphicObject).GetProperty("DataSource").GetGetMethod ();
		internal static EventInfo eiLogicalParentChanged = typeof(GraphicObject).GetEvent("LogicalParentChanged");

		internal static MethodInfo miIFaceLoad = typeof(Interface).GetMethod ("Load", BindingFlags.Instance | BindingFlags.Public);
		internal static MethodInfo miGetITemp = typeof(Interface).GetMethod ("GetItemTemplate");

		internal static MethodInfo miAddITemp = typeof(Dictionary<string, ItemTemplate>).GetMethod ("set_Item", new Type[] { typeof(string), typeof(ItemTemplate) });
		internal static MethodInfo miGetITempFromDic = typeof(Dictionary<string, ItemTemplate>).GetMethod ("get_Item", new Type[] { typeof(string) });
		internal static FieldInfo fldItemTemplates = typeof(TemplatedGroup).GetField("ItemTemplates");
		internal static MethodInfo miCreateExpDel = typeof(ItemTemplate).GetMethod ("CreateExpandDelegate");

		#region tree handling methods
		internal static FieldInfo fiChild = typeof(PrivateContainer).GetField ("child", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static MethodInfo miSetChild = typeof (Container).GetMethod ("SetChild");
		internal static MethodInfo miAddChild = typeof (Group).GetMethod ("AddChild");
		internal static FieldInfo fiChildren = typeof(Group).GetField ("children", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static MethodInfo miLoadTmp = typeof (TemplatedControl).GetMethod ("loadTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static PropertyInfo piContent = typeof(TemplatedContainer).GetProperty ("Content");
		internal static MethodInfo miAddItem = typeof (TemplatedGroup).GetMethod ("AddItem", BindingFlags.Instance | BindingFlags.Public);
		internal static MethodInfo miGetItems = typeof(TemplatedGroup).GetProperty ("Items").GetGetMethod ();
		#endregion

		#region ValueChange & DSChange Reflexion member info
		internal static EventInfo eiValueChange = typeof (IValueChange).GetEvent ("ValueChanged");
		internal static MethodInfo miInvokeValueChange = eiValueChange.EventHandlerType.GetMethod ("Invoke");
		internal static Type [] argsBoundValueChange = { typeof (object), typeof (object), miInvokeValueChange.GetParameters () [1].ParameterType };
		internal static Type [] argsValueChange = { typeof (object), miInvokeValueChange.GetParameters () [1].ParameterType };
		internal static FieldInfo fiVCNewValue = typeof (ValueChangeEventArgs).GetField ("NewValue");
		internal static FieldInfo fiVCMbName = typeof (ValueChangeEventArgs).GetField ("MemberName");
		internal static MethodInfo miValueChangeAdd = eiValueChange.GetAddMethod ();

		internal static EventInfo eiDSChange = typeof (GraphicObject).GetEvent ("DataSourceChanged");
		internal static MethodInfo miInvokeDSChange = eiDSChange.EventHandlerType.GetMethod ("Invoke");
		internal static Type [] argsBoundDSChange = {typeof (object), typeof (object), miInvokeDSChange.GetParameters () [1].ParameterType };
		internal static FieldInfo fiDSCNewDS = typeof (DataSourceChangeEventArgs).GetField ("NewDataSource");
		internal static FieldInfo fiDSCOldDS = typeof (DataSourceChangeEventArgs).GetField ("OldDataSource");
		internal static Type ehTypeDSChange = eiDSChange.EventHandlerType;
		#endregion

		/// <summary>
		/// Loc0 is the current graphic object and arg2 of loader is the current interface
		/// </summary>
		/// <param name="il">Il.</param>
		public static void emitSetCurInterface(ILGenerator il){
			il.Emit (OpCodes.Ldloc_0);
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Stfld, miSetCurIface);
		}

		public static void EmitSetValue(ILGenerator il, PropertyInfo pi, object val){
			il.Emit (OpCodes.Ldloc_0);

			if (val == null) {
				il.Emit (OpCodes.Ldnull);
				il.Emit (OpCodes.Callvirt, pi.GetSetMethod ());
				return;
			}
			Type dvType = val.GetType ();

			if (dvType.IsValueType) {
				if (pi.PropertyType.IsValueType) {
					if (pi.PropertyType.IsEnum) {
						if (pi.PropertyType != dvType)
							throw new Exception ("Enum mismatch in default values: " + pi.PropertyType.FullName);
						il.Emit (OpCodes.Ldc_I4, Convert.ToInt32 (val));
					} else {
						switch (Type.GetTypeCode (dvType)) {
						case TypeCode.Boolean:
							if ((bool)val == true)
								il.Emit (OpCodes.Ldc_I4_1);
							else
								il.Emit (OpCodes.Ldc_I4_0);
							break;
							//						case TypeCode.Empty:
							//							break;
							//						case TypeCode.Object:
							//							break;
							//						case TypeCode.DBNull:
							//							break;
							//						case TypeCode.SByte:
							//							break;
							//						case TypeCode.Decimal:
							//							break;
							//						case TypeCode.DateTime:
							//							break;
						case TypeCode.Char:
							il.Emit (OpCodes.Ldc_I4, Convert.ToChar (val));
							break;
						case TypeCode.Byte:
						case TypeCode.Int16:
						case TypeCode.Int32:
							il.Emit (OpCodes.Ldc_I4, Convert.ToInt32 (val));
							break;
						case TypeCode.UInt16:
						case TypeCode.UInt32:
							il.Emit (OpCodes.Ldc_I4, Convert.ToUInt32 (val));
							break;
						case TypeCode.Int64:
							il.Emit (OpCodes.Ldc_I8, Convert.ToInt64 (val));
							break;
						case TypeCode.UInt64:
							il.Emit (OpCodes.Ldc_I8, Convert.ToUInt64 (val));
							break;
						case TypeCode.Single:
							il.Emit (OpCodes.Ldc_R4, Convert.ToSingle (val));
							break;
						case TypeCode.Double:
							il.Emit (OpCodes.Ldc_R8, Convert.ToDouble (val));
							break;
						case TypeCode.String:
							il.Emit (OpCodes.Ldstr, Convert.ToString (val));
							break;
						default:
							il.Emit (OpCodes.Pop);
							return;
						}
					}
				} else
					throw new Exception ("Expecting valuetype in default values for: " + pi.Name);
			}else{
				//surely a class or struct
				if (dvType != typeof(string))
					throw new Exception ("Expecting String in default values for: " + pi.Name);
				if (pi.PropertyType == typeof(string))
					il.Emit (OpCodes.Ldstr, Convert.ToString (val));
				else if (pi.PropertyType.IsEnum) {
					//load type of enum
					il.Emit(OpCodes.Ldtoken, pi.PropertyType);
					il.Emit(OpCodes.Call, CompilerServices.miGetTypeFromHandle);
					//load enum value name
					il.Emit (OpCodes.Ldstr, Convert.ToString (val));//TODO:is this convert required?
					//load false
					il.Emit (OpCodes.Ldc_I4_0);
					il.Emit (OpCodes.Callvirt, CompilerServices.miParseEnum);

					if (CompilerServices.miParseEnum.ReturnType != pi.PropertyType)
						il.Emit (OpCodes.Unbox_Any, pi.PropertyType);
				} else {
					MethodInfo miParse = pi.PropertyType.GetMethod
						("Parse", BindingFlags.Static | BindingFlags.Public,
							Type.DefaultBinder, new Type [] {typeof (string)},null);
					if (miParse == null)
						throw new Exception ("no Parse method found for: " + pi.PropertyType.FullName);

					il.Emit (OpCodes.Ldstr, Convert.ToString (val));//TODO:is this convert required?
					il.Emit (OpCodes.Callvirt, miParse);

					if (miParse.ReturnType != pi.PropertyType)
						il.Emit (OpCodes.Unbox_Any, pi.PropertyType);
				}
			}
			il.Emit (OpCodes.Callvirt, pi.GetSetMethod ());
		}

		#region conversions

		internal static MethodInfo GetConvertMethod (Type targetType)
		{
			string name;

			if (targetType == typeof (bool))
				name = "ToBoolean";
			else if (targetType == typeof (byte))
				name = "ToByte";
			else if (targetType == typeof (short))
				name = "ToInt16";
			else if (targetType == typeof (int))
				name = "ToInt32";
			else if (targetType == typeof (long))
				name = "ToInt64";
			else if (targetType == typeof (double))
				name = "ToDouble";
			else if (targetType == typeof (float))
				name = "ToSingle";
			else if (targetType == typeof (string))
				return typeof (object).GetMethod ("ToString", Type.EmptyTypes);
			else //try to find implicit convertion
				throw new NotImplementedException (string.Format ("Conversion to {0} is not implemented.", targetType.Name));

			return typeof (Convert).GetMethod (name, BindingFlags.Static | BindingFlags.Public, null, new Type [] { typeof (object) }, null);
		}
		#endregion

		/// <summary>
		/// retrieve event handler in class or ancestors
		/// </summary>
		public static FieldInfo GetEventHandlerField (Type type, string eventName)
		{
			FieldInfo fi;
			Type ty = type;
			do {
				fi = ty.GetField (eventName,
					BindingFlags.NonPublic |
					BindingFlags.Instance |
					BindingFlags.GetField);
				ty = ty.BaseType;
				if (ty == null)
					break;
			} while (fi == null);
			return fi;
		}

		/// <summary>
		/// Gets extension methods defined in assembley for extendedType
		/// </summary>
		/// <returns>Extension methods enumerable</returns>
		/// <param name="assembly">Assembly</param>
		/// <param name="extendedType">Extended type to search for</param>
		public static IEnumerable<MethodInfo> GetExtensionMethods (Assembly assembly,
			Type extendedType)
		{
			IEnumerable<MethodInfo> query = null;
			Type curType = extendedType;

			do {
				query = from type in assembly.GetTypes ()
						where type.IsSealed && !type.IsGenericType && !type.IsNested
						from method in type.GetMethods (BindingFlags.Static
							| BindingFlags.Public | BindingFlags.NonPublic)
						where method.IsDefined (typeof (ExtensionAttribute), false)
						where method.GetParameters () [0].ParameterType == curType
						select method;

				if (query.Count () > 0)
					break;

				curType = curType.BaseType;
			} while (curType != null);

			return query;
		}

		public static MethodInfo SearchExtMethod(Type t, string methodName){
			MethodInfo mi = null;
			mi = GetExtensionMethods (Assembly.GetEntryAssembly(), t)
				.Where (em => em.Name == methodName).FirstOrDefault ();
			if (mi != null)
				return mi;

			return GetExtensionMethods (Assembly.GetExecutingAssembly(), t)
				.Where (em => em.Name == methodName).FirstOrDefault ();
		}

		public static MemberInfo getMemberInfoWithReflexion(object instance, string member){
			return instance.GetType ().GetMember (member).FirstOrDefault();
		}
		public static MethodInfo getMethodInfoWithReflexion(object instance, string method){
			return instance.GetType ().GetMethod (method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		}
		public static Type getEventHandlerType(object instance, string eventName){
			return instance.GetType ().GetEvent (eventName).EventHandlerType;
		}
		public static void setValueWithReflexion(object dest, object value, string destMember){
			Type destType = null;
			Type origType = null;
			object convertedVal = null;

			MemberInfo miDest = getMemberInfoWithReflexion (dest, destMember);

			if (miDest == null) {
				Debug.WriteLine ("Reverse template binding error: " + destMember + " not found in " + dest);
				return;
			}

			if (miDest.MemberType == MemberTypes.Property)
				destType =(miDest as PropertyInfo).PropertyType;
			else if (miDest.MemberType == MemberTypes.Field)
				destType =(miDest as FieldInfo).FieldType;

			if (value != null) {
				origType = value.GetType ();
				if (destType.IsAssignableFrom (origType))
					convertedVal = Convert.ChangeType (value, destType);
				else if (origType.IsPrimitive & destType.IsPrimitive)
					convertedVal = GetConvertMethod (destType).Invoke (null, new Object[] { value });
				else
					convertedVal = getImplicitOp (origType, destType).Invoke (value, null);
			}

			if (miDest.MemberType == MemberTypes.Property)
				(miDest as PropertyInfo).SetValue (dest, convertedVal);
			else if (miDest.MemberType == MemberTypes.Field)
				(miDest as FieldInfo).SetValue (dest, convertedVal);
		}
		public static object getValueWithReflexion(object instance, MemberInfo mi){
			object tmp = null;
			Type dstType = null;
			if (mi == null)
				return null;
			if (mi.MemberType == MemberTypes.Property) {
				PropertyInfo pi = mi as PropertyInfo;
				tmp = pi.GetValue (instance);
				dstType = pi.PropertyType;
			}
			if (mi.MemberType == MemberTypes.Field) {
				FieldInfo fi = mi as FieldInfo;
				tmp = fi.GetValue (instance);
				dstType = fi.FieldType;
			}
			if (tmp != null)
				return tmp;
			if (dstType == typeof(string) || dstType == CompilerServices.TObject)//TODO:object should be allowed to return null and not ""
				return "";
			if (dstType.IsValueType)
				return Activator.CreateInstance (dstType);

			return null;
		}
		public static void emitGetInstance (ILGenerator il, NodeAddress orig, NodeAddress dest){
			int ptr = 0;
			while (orig [ptr] == dest [ptr]) {
				ptr++;
				if (ptr == orig.Count || ptr == dest.Count)
					break;
			}
			for (int i = 0; i < orig.Count - ptr; i++)
				il.Emit (OpCodes.Callvirt, CompilerServices.miGetLogicalParent);
			while (ptr < dest.Count) {
				emitGetChild (il, dest [ptr-1].CrowType, dest [ptr].Index);
				ptr++;
			}
		}
		public static void emitGetInstance (ILGenerator il, NodeAddress dest){
			if (dest == null)
				return;
			for (int i = 0; i < dest.Count - 1; i++)
				emitGetChild (il, dest [i].CrowType, dest [i + 1].Index);
		}
		public static void emitGetChild(ILGenerator il, Type parentType, int index){
			if (typeof (Group).IsAssignableFrom (parentType)) {
				il.Emit (OpCodes.Ldfld, fiChildren);
				il.Emit(OpCodes.Ldc_I4, index);
				il.Emit (OpCodes.Callvirt, miGetGObjItem);
				return;
			}
			if (typeof(Container).IsAssignableFrom (parentType) || index < 0) {
				il.Emit (OpCodes.Ldfld, fiChild);
				return;
			}
			if (typeof(TemplatedContainer).IsAssignableFrom (parentType)) {
				il.Emit (OpCodes.Callvirt, piContent.GetGetMethod());
				return;
			}
			if (typeof(TemplatedGroup).IsAssignableFrom (parentType)) {
				il.Emit (OpCodes.Callvirt, miGetItems);
				il.Emit(OpCodes.Ldc_I4, index);
				il.Emit (OpCodes.Callvirt, miGetGObjItem);
				return;
			}
		}
		/// <summary>
		/// Emit conversion from orig type to dest type
		/// </summary>
		public static void emitConvert(ILGenerator il, Type origType, Type destType){
			if (destType == CompilerServices.TObject)
				return;
			if (destType == typeof(string)) {
				System.Reflection.Emit.Label emitNullStr = il.DefineLabel ();
				System.Reflection.Emit.Label endConvert = il.DefineLabel ();
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, emitNullStr);
				il.Emit (OpCodes.Callvirt, CompilerServices.miObjToString);
				il.Emit (OpCodes.Br, endConvert);
				il.MarkLabel (emitNullStr);
				il.Emit (OpCodes.Pop);//remove null string from stack
				il.Emit (OpCodes.Ldstr, "");//replace with empty string
				il.MarkLabel (endConvert);
			}else if (origType.IsValueType) {
				if (destType != origType) {
					il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (destType));
				}else
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
		public static void emitConvert(ILGenerator il, Type dstType){
			System.Reflection.Emit.Label endConvert = il.DefineLabel ();
			System.Reflection.Emit.Label convert = il.DefineLabel ();

			il.Emit (OpCodes.Dup);
			il.Emit (OpCodes.Isinst, dstType);
			il.Emit (OpCodes.Brfalse, convert);

			if (dstType.IsValueType)
				il.Emit (OpCodes.Unbox_Any, dstType);
			else
				il.Emit (OpCodes.Isinst, dstType);
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
				il.Emit (OpCodes.Callvirt, miGetType);
				il.Emit (OpCodes.Ldtoken, dstType);//push destination property type for testing
				il.Emit (OpCodes.Call, CompilerServices.miGetTypeFromHandle);
				il.Emit (OpCodes.Call, miGetImplOp);
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
				il.Emit(OpCodes.Newarr, CompilerServices.TObject);
				il.Emit (OpCodes.Dup);//duplicate the array ref
				il.Emit (OpCodes.Ldc_I4_0);//push the index 0
				il.Emit (OpCodes.Ldloc_0);//push the orig value to convert
				il.Emit (OpCodes.Stelem, CompilerServices.TObject);//set the array element at index 0
				il.Emit (OpCodes.Callvirt, miMIInvoke);
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
		/// <summary>
		/// Removes delegate from event handler by name
		/// </summary>
		public static void RemoveEventHandlerByName(object instance, string eventName, string delegateName){
			Type t = instance.GetType ();
			FieldInfo fiEvt = CompilerServices.GetEventHandlerField (t, eventName);
			if (fiEvt == null) {
				Debug.WriteLine ("RemoveHandlerByName: Event '" + eventName + "' not found in " + instance);
				return;
			}
			EventInfo eiEvt = t.GetEvent (eventName);
			MulticastDelegate multiDel = fiEvt.GetValue (instance) as MulticastDelegate;
			if (multiDel != null) {
				foreach (Delegate d in multiDel.GetInvocationList()) {
					if (d.Method.Name == delegateName) {
						eiEvt.RemoveEventHandler (instance, d);
						#if DEBUG_BINDING
						Debug.WriteLine ("\t{0} handler removed in {1} for: {2}", d.Method.Name,instance, eventName);
						#endif
					}
				}
			}
		}
		/// <summary>
		/// Removes delegate from event handler by searching for the object they are bond to
		/// </summary>
		public static void RemoveEventHandlerByTarget(object instance, string eventName, object target){
			Type t = instance.GetType ();
			FieldInfo fiEvt = CompilerServices.GetEventHandlerField (t, eventName);
			EventInfo eiEvt = t.GetEvent (eventName);
			MulticastDelegate multiDel = fiEvt.GetValue (instance) as MulticastDelegate;
			if (multiDel != null) {
				foreach (Delegate d in multiDel.GetInvocationList()) {
					if (d.Target == target) {
						eiEvt.RemoveEventHandler (instance, d);
						#if DEBUG_BINDING
						Debug.WriteLine ("\t{0} handler removed in {1} for: {2}", d.Method.Name,instance, eventName);
						#endif
					}
				}
			}
		}
		internal static Delegate createDel(Type eventType, object instance, string method){
			Type t = instance.GetType ();
			MethodInfo mi = t.GetMethod (method);
			if (mi == null)
				return null;
			return Delegate.CreateDelegate (eventType, instance, mi);
		}
		public static Delegate compileDynEventHandler(EventInfo sourceEvent, string expression, NodeAddress currentNode = null){
			#if DEBUG_BINDING
			Debug.WriteLine ("\tCompile Event {0}: {1}", sourceEvent.Name, expression);
			#endif

			Type lopType = null;

			if (currentNode == null)
				lopType = sourceEvent.DeclaringType;
			else
				lopType = currentNode.NodeType;

			#region Retrieve EventHandler parameter type
			MethodInfo evtInvoke = sourceEvent.EventHandlerType.GetMethod ("Invoke");
			ParameterInfo [] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;
			#endregion

			Type [] args = { CompilerServices.TObject, handlerArgsType };
			DynamicMethod dm = new DynamicMethod ("dyn_eventHandler",
				typeof(void),
				args, true);
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
				MemberInfo lopMI = null;

				il.Emit (OpCodes.Ldarg_0);  //load sender ref onto the stack

				if (lopParts.Length > 1) {
					NodeAddress lopNA = getNodeAdressFromBindingExp (currentNode, lopParts);
					CompilerServices.emitGetInstance (il, currentNode, lopNA);
					lopType = lopNA.NodeType;
				}

				string [] bindTrg = lopParts.Last().Split ('.');

				if (bindTrg.Length == 1)
					lopMI = lopType.GetMember (bindTrg [0]).FirstOrDefault();
				else if (bindTrg.Length == 2) {
					//named target
					//TODO:
					il.Emit(OpCodes.Ldstr, bindTrg[0]);
					il.Emit(OpCodes.Callvirt, miFindByName);
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

			return dm.CreateDelegate (sourceEvent.EventHandlerType);
		}
		public static string[] splitOnSemiColumnOutsideAccolades (string expression){
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
		/// <summary>
		/// Gets the node adress from binding expression splitted with '/' starting at a given node
		/// </summary>
		public static NodeAddress getNodeAdressFromBindingExp(NodeAddress sourceAddr, string[] bindingExp){
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
				if (levelUp > ptr + 1)
					throw new Exception ("Binding error: try to bind outside IML source");
				ptr -= levelUp;
			}
			//TODO:change Template special address identified with Nodecount = 0 to something not using array count to 0,
			//here linq is working without limits checking in compile option
			//but defining a 0 capacity array with limits cheking enabled, cause 'out of memory' error
			return new NodeAddress (sourceAddr.Take(ptr+1).ToArray());//[ptr+1];
			//Array.Copy (sourceAddr.ToArray (), targetNode, ptr + 1);
			//return new NodeAddress (targetNode);
		}

	}
}

