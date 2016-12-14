using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;


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
		internal static Type [] argsDSChange = {typeof (object), typeof (object), miInvokeDSChange.GetParameters () [1].ParameterType };
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
						il.Emit (OpCodes.Ldstr, b.Expression.Split ('/').LastOrDefault ());
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
					else if (b.Source.Property.PropertyType != sourceValueType) {
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

				Delegate del = dm.CreateDelegate (eiValueChange.EventHandlerType, Bindings [0].Source.Instance);
				miValueChangeAdd.Invoke (grouped [0].Target.Instance, new object [] { del });
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
			if (dstType == typeof(string))
				return "";
			if (dstType.IsValueType)
				return Activator.CreateInstance (dstType);
			return null;
		}
	}
}

