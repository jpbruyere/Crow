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
		static FieldInfo miSetCurIface = typeof(GraphicObject).GetField ("currentInterface",
			BindingFlags.NonPublic | BindingFlags.Instance);
		internal static MethodInfo stringEquals = typeof (string).GetMethod
			("Equals", new Type [3] { typeof (string), typeof (string), typeof (StringComparison) });
		public static MethodInfo miFindByName = typeof (GraphicObject).GetMethod ("FindByName");

		public static MethodInfo miIFaceLoad = typeof(Interface).GetMethod ("Load", BindingFlags.Instance | BindingFlags.Public);
		public static MethodInfo miGetITemp = typeof(Interface).GetMethod ("GetItemTemplate");
		public static MethodInfo miAddITemp = typeof(Dictionary<string, ItemTemplate>).
			GetMethod ("set_Item", new Type[] { typeof(string), typeof(ItemTemplate) });
		public static MethodInfo miGetITempFromDic = typeof(Dictionary<string, ItemTemplate>).
			GetMethod ("get_Item", new Type[] { typeof(string) });
		public static FieldInfo fldItemTemplates = typeof(TemplatedGroup).GetField("ItemTemplates");
		public static MethodInfo miCreateExpDel = typeof(ItemTemplate).GetMethod ("CreateExpandDelegate");
		public static MethodInfo miLoadDefaultVals = typeof (GraphicObject).GetMethod ("loadDefaultValues");
		public static PropertyInfo piStyle = typeof (GraphicObject).GetProperty ("Style");
		public static MethodInfo miGetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
		#region tree handling methods
		internal static MethodInfo miSetChild = typeof (Container).GetMethod ("SetChild");
		internal static MethodInfo miAddChild = typeof (Group).GetMethod ("AddChild");
		internal static MethodInfo miLoadTmp = typeof (TemplatedControl).GetMethod ("loadTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static MethodInfo miSetContent = typeof (TemplatedContainer).GetProperty ("Content").GetSetMethod ();
		internal static MethodInfo miAddItem = typeof (TemplatedGroup).GetMethod ("AddItem", BindingFlags.Instance | BindingFlags.Public);
		#endregion

		#region ValueChange Reflexion member info
		internal static EventInfo eiValueChange = typeof (IValueChange).GetEvent ("ValueChanged");
		internal static MethodInfo miInvokeValueChange = eiValueChange.EventHandlerType.GetMethod ("Invoke");
		internal static Type [] argsBoundValueChange = { typeof (object), typeof (object), miInvokeValueChange.GetParameters () [1].ParameterType };
		internal static Type [] argsValueChange = { typeof (object), miInvokeValueChange.GetParameters () [1].ParameterType };
		internal static FieldInfo fiNewValue = typeof (ValueChangeEventArgs).GetField ("NewValue");
		internal static FieldInfo fiMbName = typeof (ValueChangeEventArgs).GetField ("MemberName");
		internal static MethodInfo miValueChangeAdd = eiValueChange.GetAddMethod ();

		internal static EventInfo eiDSChange = typeof (GraphicObject).GetEvent ("DataSourceChanged");
		internal static MethodInfo miInvokeDSChange = eiDSChange.EventHandlerType.GetMethod ("Invoke");
		internal static Type [] argsBoundDSChange = {typeof (object), typeof (object), miInvokeDSChange.GetParameters () [1].ParameterType };
		internal static FieldInfo fiDSCNewDS = typeof (DataSourceChangeEventArgs).GetField ("NewDataSource");

		internal static MethodInfo miCreateBoundDelegate = typeof(DynamicMethod).
			GetMethod("CreateDelegate", new Type[] { typeof(Type), typeof(object)});
		internal static MethodInfo miObjToString = typeof(object).GetMethod("ToString");

		internal static Type ehTypeDSChange = eiDSChange.EventHandlerType;
		internal static FieldInfo fi_ehTypeDSChange  = typeof(CompilerServices).GetField("ehTypeDSChange", BindingFlags.Static | BindingFlags.NonPublic);

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
					MethodInfo miParse = typeof(Enum).GetMethod
						("Parse", BindingFlags.Static | BindingFlags.Public,
							Type.DefaultBinder, new Type [] {typeof (Type), typeof (string), typeof (bool)}, null);

					if (miParse == null)
						throw new Exception ("Enum Parse method not found");

					//load type of enum
					il.Emit(OpCodes.Ldtoken, pi.PropertyType);
					il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new
						Type[1]{typeof(RuntimeTypeHandle)}));
					//load enum value name
					il.Emit (OpCodes.Ldstr, Convert.ToString (val));//TODO:is this convert required?
					//load false
					il.Emit (OpCodes.Ldc_I4_0);
					il.Emit (OpCodes.Callvirt, miParse);

					if (miParse.ReturnType != pi.PropertyType)
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

		public static void ResolveBindings (List<Binding> Bindings)
		{
			if (Bindings == null)
				return;
			if (Bindings.Count == 0)
				return;
			//#if DEBUG_BINDING
			//			Debug.WriteLine ("Resolve Bindings => " + this.ToString ());
			//#endif
			//grouped bindings by Instance of Source
			Dictionary<object, List<Binding>> resolved = new Dictionary<object, List<Binding>> ();

			foreach (Binding b in Bindings) {
				if (b.Resolved)
					continue;
				if (b.Source.Member.MemberType == MemberTypes.Event) {
					if (b.Expression.StartsWith ("{")) {
						CompilerServices.CompileEventSource (b);
						continue;
					}
					if (!b.TryFindTarget ())
						continue;
					//register handler for event
					if (b.Target.Method == null) {
						//Debug.WriteLine ("\tError: Handler Method not found: " + b.ToString ());
						continue;
					}
					try {
						MethodInfo addHandler = b.Source.Event.GetAddMethod ();
						Delegate del = Delegate.CreateDelegate (b.Source.Event.EventHandlerType, b.Target.Instance, b.Target.Method);
						addHandler.Invoke (b.Source.Instance, new object [] { del });

#if DEBUG_BINDING
						Debug.WriteLine ("\tHandler binded => " + b.ToString());
#endif
						b.Resolved = true;
					} catch (Exception ex) {
						//Debug.WriteLine ("\tERROR: " + ex.ToString ());
					}
					continue;
				}

				if (!b.TryFindTarget ())
					continue;

				//group Bindings by target instanceq
				List<Binding> bindings = null;
				if (!resolved.TryGetValue (b.Target.Instance, out bindings)) {
					bindings = new List<Binding> ();
					resolved [b.Target.Instance] = bindings;
				}
				bindings.Add (b);
				b.Resolved = true;
			}

			Type target_Type = Bindings [0].Source.Instance.GetType ();

			//group;only one dynMethods by target (valuechanged event source)
			//changed value name tested in switch
			//IEnumerable<Binding[]> groupedByTarget = resolved.GroupBy (g => g.Target.Instance, g => g, (k, g) => g.ToArray ());
			foreach (List<Binding> grouped in resolved.Values) {
				int i = 0;
				Type source_Type = grouped [0].Target.Instance.GetType ();

				DynamicMethod dm = null;
				ILGenerator il = null;

				System.Reflection.Emit.Label [] jumpTable = null;
				System.Reflection.Emit.Label endMethod = new System.Reflection.Emit.Label ();

				#region Retrieve EventHandler parameter type
				//EventInfo ei = targetType.GetEvent ("ValueChanged");
				//no dynamic update if ValueChanged interface is not implemented
				if (source_Type.GetInterfaces ().Contains (typeof (IValueChange))) {
					dm = new DynamicMethod (grouped [0].CreateNewDynMethodId (),
						MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
						CallingConventions.Standard,
						typeof (void),
						argsBoundValueChange,
						target_Type, true);

					il = dm.GetILGenerator (256);

					endMethod = il.DefineLabel ();
					jumpTable = new System.Reflection.Emit.Label [grouped.Count];
					for (i = 0; i < grouped.Count; i++)
						jumpTable [i] = il.DefineLabel ();
					il.DeclareLocal (typeof (string));
					il.DeclareLocal (typeof (object));

					il.Emit (OpCodes.Nop);
					il.Emit (OpCodes.Ldarg_0);
					//il.Emit(OpCodes.Isinst, sourceType);
					//push new value onto stack
					il.Emit (OpCodes.Ldarg_2);
					il.Emit (OpCodes.Ldfld, fiNewValue);
					il.Emit (OpCodes.Stloc_1);
					//push name
					il.Emit (OpCodes.Ldarg_2);
					il.Emit (OpCodes.Ldfld, fiMbName);
					il.Emit (OpCodes.Stloc_0);
					il.Emit (OpCodes.Ldloc_0);
					il.Emit (OpCodes.Brfalse, endMethod);
				}
				#endregion

				i = 0;
				foreach (Binding b in grouped) {
					#region initialize target with actual value
					object targetValue = null;
					if (b.Target.Member != null) {
						if (b.Target.Member.MemberType == MemberTypes.Property)
							targetValue = b.Target.Property.GetGetMethod ().Invoke (b.Target.Instance, null);
						else if (b.Target.Member.MemberType == MemberTypes.Field)
							targetValue = b.Target.Field.GetValue (b.Target.Instance);
						else if (b.Target.Member.MemberType == MemberTypes.Method) {
							MethodInfo mthSrc = b.Target.Method;
							if (mthSrc.IsDefined (typeof (ExtensionAttribute), false))
								targetValue = mthSrc.Invoke (null, new object [] { b.Target.Instance });
							else
								targetValue = mthSrc.Invoke (b.Target.Instance, null);
						} else
							throw new Exception ("unandled source member type for binding");
					} else if (string.IsNullOrEmpty (b.Expression))
						targetValue = grouped [0].Target.Instance;//empty binding exp=> bound to target object by default
																  //TODO: handle other dest type conversions
					if (b.Source.Property.PropertyType == typeof (string)) {
						if (targetValue == null) {
							//set default value

						} else
							targetValue = targetValue.ToString ();
					}
					try {
						if (targetValue != null)
							b.Source.Property.GetSetMethod ().Invoke
							(b.Source.Instance, new object [] { b.Source.Property.PropertyType.Cast (targetValue) });
						else
							b.Source.Property.GetSetMethod ().Invoke
							(b.Source.Instance, new object [] { targetValue });
					} catch (Exception ex) {
						Debug.WriteLine (ex.ToString ());
					}
					#endregion

					//if no dyn update, skip jump table
					if (il == null)
						continue;

					il.Emit (OpCodes.Ldloc_0);
					if (b.Target.Member != null)
						il.Emit (OpCodes.Ldstr, b.Target.Member.Name);
					else
						il.Emit (OpCodes.Ldstr, b.Expression.Split ('/').LastOrDefault ().Split('.').LastOrDefault());
					il.Emit (OpCodes.Ldc_I4_4);//StringComparison.Ordinal
					il.Emit (OpCodes.Callvirt, stringEquals);
					il.Emit (OpCodes.Brtrue, jumpTable [i]);
					i++;
				}

				if (il == null)
					continue;

				il.Emit (OpCodes.Br, endMethod);

				i = 0;
				foreach (Binding b in grouped) {

					il.MarkLabel (jumpTable [i]);


					//load 2 times to check first for null
					il.Emit (OpCodes.Ldloc_1);
					il.Emit (OpCodes.Ldloc_1);

					System.Reflection.Emit.Label labSetValue = il.DefineLabel ();
					il.Emit (OpCodes.Brtrue, labSetValue);
					//if null
					il.Emit (OpCodes.Unbox_Any, b.Source.Property.PropertyType);
					il.Emit (OpCodes.Callvirt, b.Source.Property.GetSetMethod ());
					il.Emit (OpCodes.Br, endMethod);

					il.MarkLabel (labSetValue);
					//new value not null

					//by default, source value type is deducted from target member type to allow
					//memberless binding, if targetMember exists, it will be used to determine target
					//value type for conversion
					Type sourceValueType = b.Source.Property.PropertyType;
					if (b.Target.Member != null) {
						if (b.Target.Member.MemberType == MemberTypes.Property)
							sourceValueType = b.Target.Property.PropertyType;
						else if (b.Target.Member.MemberType == MemberTypes.Field)
							sourceValueType = b.Target.Field.FieldType;
						else
							throw new Exception ("unhandle target member type in binding");
					}



					if (b.Source.Property.PropertyType == typeof (string)) {
						MemberReference tostring = new MemberReference (b.Source.Instance);
						if (!tostring.TryFindMember ("ToString"))
							throw new Exception ("ToString method not found");
						il.Emit (OpCodes.Callvirt, tostring.Method);
					} else if (!sourceValueType.IsValueType)
						il.Emit (OpCodes.Castclass, sourceValueType);
					else if (b.Source.Property.PropertyType != sourceValueType && b.Source.Property.PropertyType != typeof(object)) {
						il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (b.Source.Property.PropertyType));
					} else
						il.Emit (OpCodes.Unbox_Any, b.Source.Property.PropertyType);

					il.Emit (OpCodes.Callvirt, b.Source.Property.GetSetMethod ());

					//il.BeginCatchBlock (typeof (Exception));
					//il.Emit (OpCodes.Pop);
					//il.EndExceptionBlock ();

					il.Emit (OpCodes.Br, endMethod);
					i++;

				}
				il.MarkLabel (endMethod);
				il.Emit (OpCodes.Pop);
				il.Emit (OpCodes.Ret);

				try {
					Delegate del = dm.CreateDelegate (eiValueChange.EventHandlerType, Bindings [0].Source.Instance);
					miValueChangeAdd.Invoke (grouped [0].Target.Instance, new object [] { del });

				} catch (Exception ex) {					
					Debug.WriteLine ("Binding Delegate error for {0}: \n{1}", Bindings [0].Source.Instance, ex.ToString ());
				}
			}
		}

		/// <summary>
		/// Compile events expression in GOML attributes
		/// </summary>
		/// <param name="binding">Event binding details</param>
		public static void CompileEventSource (Binding binding)
		{
#if DEBUG_BINDING
			Debug.WriteLine ("\tCompile Event Source => " + binding.ToString());
#endif

			Type target_type = binding.Source.Instance.GetType ();

			#region Retrieve EventHandler parameter type
			MethodInfo evtInvoke = binding.Source.Event.EventHandlerType.GetMethod ("Invoke");
			ParameterInfo [] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;
			#endregion

			Type [] args = { typeof (object), typeof (object), handlerArgsType };
			DynamicMethod dm = new DynamicMethod (binding.CreateNewDynMethodId (),
				typeof (void),
				args,
				target_type);


			#region IL generation
			ILGenerator il = dm.GetILGenerator (256);

			string src = binding.Expression.Trim ();

			if (!(src.StartsWith ("{") || src.EndsWith ("}")))
				throw new Exception (string.Format ("GOML:Malformed {0} Event handler: {1}", binding.Source.Member.Name, binding.Expression));

			src = src.Substring (1, src.Length - 2);
			string [] srcLines = src.Split (new char [] { ';' });

			foreach (string srcLine in srcLines) {
				string statement = srcLine.Trim ();

				string [] operandes = statement.Split (new char [] { '=' });
				if (operandes.Length < 2) //not an affectation
				{
					continue;
				}
				string lop = operandes [0].Trim ();
				string rop = operandes [operandes.Length - 1].Trim ();

				#region LEFT OPERANDES
				GraphicObject lopObj = binding.Source.Instance as GraphicObject;    //default left operand base object is
																					//the first arg (object sender) of the event handler

				il.Emit (OpCodes.Ldarg_0);  //load sender ref onto the stack

				string [] lopParts = lop.Split (new char [] { '.' });
				if (lopParts.Length > 1) {//should search also for member of es.Source
					for (int j = 0; j < lopParts.Length - 1; j++) {
						il.Emit (OpCodes.Ldstr, lopParts [j]);
						il.Emit (OpCodes.Callvirt, miFindByName);
					}
				}

				int i = lopParts.Length - 1;

				MemberInfo [] lopMbis = lopObj.GetType ().GetMember (lopParts [i]);

				if (lopMbis.Length < 1)
					throw new Exception (string.Format ("CROW BINDING: Member not found '{0}'", lop));

				OpCode lopSetOC;
				dynamic lopSetMbi;
				Type lopT = null;
				switch (lopMbis [0].MemberType) {
				case MemberTypes.Property:
					PropertyInfo lopPi = target_type.GetProperty (lopParts [i]);
					MethodInfo dstMi = lopPi.GetSetMethod ();
					lopT = lopPi.PropertyType;
					lopSetMbi = dstMi;
					lopSetOC = OpCodes.Callvirt;
					break;
				case MemberTypes.Field:
					FieldInfo dstFi = target_type.GetField (lopParts [i]);
					lopT = dstFi.FieldType;
					lopSetMbi = dstFi;
					lopSetOC = OpCodes.Stfld;
					break;
				default:
					throw new Exception (string.Format ("GOML:member type not handle: {0}", lopParts [i]));
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
				il.Emit (lopSetOC, lopSetMbi);
			}

			il.Emit (OpCodes.Ret);

			#endregion

			Delegate del = dm.CreateDelegate (binding.Source.Event.EventHandlerType, binding.Source.Instance);
			MethodInfo addHandler = binding.Source.Event.GetAddMethod ();
			addHandler.Invoke (binding.Source.Instance, new object [] { del });

			binding.Resolved = true;
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
			if (dstType == typeof(string) || dstType == typeof(object))//TODO:object should be allowed to return null and not ""
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
				il.Emit (OpCodes.Callvirt, typeof(ILayoutable).GetProperty ("LogicalParent").GetGetMethod ());
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
		public static void emitConvert(ILGenerator il, Type origType, Type destType){
			if (destType == typeof(object))
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
				il.Emit (OpCodes.Callvirt, typeof(object).GetMethod ("GetType"));
				il.Emit (OpCodes.Ldtoken, dstType);//push destination property type for testing
				il.Emit (OpCodes.Call, CompilerServices.miGetTypeFromHandle);
				il.Emit (OpCodes.Call, typeof(CompilerServices).GetMethod ("getImplicitOp", BindingFlags.Static | BindingFlags.Public));
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

			Type [] args = { typeof (object), handlerArgsType };
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

