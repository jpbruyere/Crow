//
// CompilerServices.cs
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
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using Crow.IML;
using System.Text;

namespace Crow.IML
{
	public static class CompilerServices
	{
		/// <summary>
		/// known types cache, prevent rewalking all the assemblies of the domain
		/// the key is the type simple name
		/// </summary>
		internal static Dictionary<string, Type> knownTypes = new Dictionary<string, Type> ();
		/// <summary>
		/// known extension methods.
		/// key is type dot memberName.
		/// </summary>
		internal static Dictionary<string, MethodInfo> knownExtMethods = new Dictionary<string, MethodInfo> ();

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

		internal static MethodInfo miCompileDynEventHandler = typeof(CompilerServices).GetMethod ("compileDynEventHandler", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miRemEvtHdlByName = typeof(CompilerServices).GetMethod("removeEventHandlerByName", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miRemEvtHdlByTarget = typeof(CompilerServices).GetMethod("removeEventHandlerByTarget", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miGetMethInfoWithRefx = typeof(CompilerServices).GetMethod ("getMethodInfoWithReflexion", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miGetMembIinfoWithRefx = typeof(CompilerServices).GetMethod("getMemberInfoWithReflexion", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miSetValWithRefx = typeof(CompilerServices).GetMethod("setValueWithReflexion", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miGetValWithRefx = typeof(CompilerServices).GetMethod("getValueWithReflexion", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miCreateDel = typeof(CompilerServices).GetMethod ("createDel", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miGetImplOp = typeof(CompilerServices).GetMethod ("getImplicitOp", BindingFlags.Static | BindingFlags.NonPublic);
		internal static MethodInfo miGetDataTypeAndFetch = typeof(CompilerServices).GetMethod("getDataTypeAndFetch", BindingFlags.Static | BindingFlags.NonPublic);


		internal static MethodInfo miGoUpLevels = typeof(CompilerServices).GetMethod("goUpNbLevels", BindingFlags.Static | BindingFlags.NonPublic);

		internal static FieldInfo fiCachedDel = typeof(Instantiator).GetField("cachedDelegates", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static FieldInfo fiTemplateBinding = typeof(Instantiator).GetField("templateBinding", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static MethodInfo miDSChangeEmitHelper = typeof(Instantiator).GetMethod("dataSourceChangedEmitHelper", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static MethodInfo miDSReverseBinding = typeof(Instantiator).GetMethod("dataSourceReverseBinding", BindingFlags.Static | BindingFlags.NonPublic);

		internal static FieldInfo miSetCurIface = typeof(Widget).GetField ("IFace", BindingFlags.Public | BindingFlags.Instance);
		internal static MethodInfo miFindByName = typeof (Widget).GetMethod ("FindByName");
		internal static MethodInfo miGetGObjItem = typeof(List<Widget>).GetMethod("get_Item", new Type[] { typeof(Int32) });
		internal static MethodInfo miLoadDefaultVals = typeof (Widget).GetMethod ("loadDefaultValues");
		internal static PropertyInfo piStyle = typeof (Widget).GetProperty ("Style");
		internal static MethodInfo miGetLogicalParent = typeof(Widget).GetProperty("LogicalParent").GetGetMethod();
		internal static MethodInfo miGetDataSource = typeof(Widget).GetProperty("DataSource").GetGetMethod ();
		internal static EventInfo eiLogicalParentChanged = typeof(Widget).GetEvent("LogicalParentChanged");

		internal static MethodInfo miIFaceLoad = typeof(Interface).GetMethod ("CreateInstance", BindingFlags.Instance | BindingFlags.Public);
		internal static MethodInfo miIFaceCreateTemplateInst = typeof (Interface).GetMethod ("CreateTemplateInstance", BindingFlags.Instance | BindingFlags.Public);
		internal static MethodInfo miGetITemp = typeof(Interface).GetMethod ("GetItemTemplate", BindingFlags.Instance | BindingFlags.Public);

		internal static MethodInfo miAddITemp = typeof(Dictionary<string, ItemTemplate>).GetMethod ("set_Item", new Type[] { typeof(string), typeof(ItemTemplate) });
		internal static MethodInfo miGetITempFromDic = typeof(Dictionary<string, ItemTemplate>).GetMethod ("get_Item", new Type[] { typeof(string) });
		internal static FieldInfo fldItemTemplates = typeof(TemplatedGroup).GetField("ItemTemplates");
		internal static MethodInfo miLoadPage = typeof(TemplatedGroup).GetMethod ("loadPage", BindingFlags.Instance | BindingFlags.NonPublic| BindingFlags.Public);
		internal static MethodInfo miIsAlreadyExpanded = typeof(TemplatedGroup).GetMethod("emitHelperIsAlreadyExpanded", BindingFlags.Instance | BindingFlags.NonPublic);
		
		internal static MethodInfo miCreateExpDel = typeof(ItemTemplate).GetMethod ("CreateExpandDelegate");
		internal static FieldInfo fiFetchMethodName = typeof(ItemTemplate).GetField("fetchMethodName", BindingFlags.Instance | BindingFlags.NonPublic);

		#if DESIGN_MODE
		internal static MethodInfo miDicStrStrAdd = typeof(Dictionary<string, string>).GetMethod ("set_Item", new Type[] { typeof(string), typeof(string) });
		#endif

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

		internal static EventInfo eiDSChange = typeof (Widget).GetEvent ("DataSourceChanged");
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
			il.Emit (OpCodes.Ldarg_1);
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
					il.Emit (OpCodes.Ldstr, Convert.ToString (val));//TODO:implement here string format?
					//load false
					il.Emit (OpCodes.Ldc_I4_0);
					il.Emit (OpCodes.Call, CompilerServices.miParseEnum);

					if (CompilerServices.miParseEnum.ReturnType != pi.PropertyType)
						il.Emit (OpCodes.Unbox_Any, pi.PropertyType);
				} else {
					MethodInfo miParse = pi.PropertyType.GetMethod
						("Parse", BindingFlags.Static | BindingFlags.Public,
							Type.DefaultBinder, new Type [] {typeof (string)},null);
					if (miParse == null)
						throw new Exception ("no Parse method found for: " + pi.PropertyType.FullName);

					il.Emit (OpCodes.Ldstr, Convert.ToString (val));//TODO:is this convert required?
					il.Emit (OpCodes.Call, miParse);

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

		#region Reflexion helpers
		static MemberInfo getMemberInfoWithReflexion(object instance, string member){
            Type t = instance.GetType();
#if DEBUG_BINDING_FUNC_CALLS
			Console.WriteLine ($"getMemberInfoWithReflexion ({instance},{member}); type:{t}");
#endif
            MemberInfo mi = t.GetMember (member)?.FirstOrDefault();
			if (mi == null)
				mi = CompilerServices.SearchExtMethod (t, member);
			return mi;
		}
		static MethodInfo getMethodInfoWithReflexion(object instance, string method){
#if DEBUG_BINDING_FUNC_CALLS
            Console.WriteLine ($"getMethodInfoWithReflexion ({instance},{method}); type:{instance.GetType ()}");
#endif
            return instance.GetType ().GetMethod (method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		}
		/// <summary>
		/// set value, convert if required
		/// </summary>
		/// <param name="dest">Destination instance</param>
		/// <param name="value">Value</param>
		/// <param name="destMember">Destination member</param>
		static void setValueWithReflexion(object dest, object value, string destMember){
#if DEBUG_BINDING_FUNC_CALLS
            Console.WriteLine ($"setValueWithReflexion (dest:{dest},value:{value},member:{destMember});");
#endif
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

			try {
				if (value != null) {
					if (destType == typeof (object))//TODO: check that test of destType is not causing problems
						convertedVal = value;
					else {
						origType = value.GetType ();
						if (destType.IsAssignableFrom (origType))
							convertedVal = Convert.ChangeType (value, destType);
						else if (origType == typeof(string) & destType.IsPrimitive)
							convertedVal = Convert.ChangeType(value, destType);
						else if (origType.IsPrimitive & destType.IsPrimitive)
							convertedVal = GetConvertMethod (destType).Invoke (null, new Object[] { value });
						else
							convertedVal = getImplicitOp (origType, destType).Invoke (value, null);
					}
				}
			} catch (Exception ex) {
				Debug.WriteLine (ex.ToString ());
				return;
			}

			if (miDest.MemberType == MemberTypes.Property)
				(miDest as PropertyInfo).SetValue (dest, convertedVal);
			else if (miDest.MemberType == MemberTypes.Field)
				(miDest as FieldInfo).SetValue (dest, convertedVal);
		}
		/// <summary>
		/// Gets value with reflexion, return empty string ("") for string and object and return
		/// default value for valueType data.
		/// </summary>
		static object getValueWithReflexion(object instance, MemberInfo mi){
#if DEBUG_BINDING_FUNC_CALLS
            Console.WriteLine ($"getValueWithReflexion ({instance},{mi});");
#endif
            object tmp = null;
			Type dstType = null;
			if (mi == null)
				return null;
			try {
				if (mi.MemberType == MemberTypes.Property) {
					PropertyInfo pi = mi as PropertyInfo;
					tmp = pi.GetValue (instance);
					dstType = pi.PropertyType;
				}else if (mi.MemberType == MemberTypes.Field) {
					FieldInfo fi = mi as FieldInfo;
					tmp = fi.GetValue (instance);
					dstType = fi.FieldType;
				}else if (mi.MemberType == MemberTypes.Method) {
					MethodInfo gi = mi as MethodInfo;
					if (gi.IsStatic)
						tmp = gi.Invoke(null, new object[] {instance});
					else
						tmp = gi.Invoke(instance, null);
					dstType = gi.ReturnType;
				}
				if (tmp != null)
					return tmp;
				if (dstType == typeof(string) || dstType == typeof (object))//TODO:object should be allowed to return null and not ""
					return "";
				if (dstType.IsValueType)
					return Activator.CreateInstance (dstType);				
			} catch (Exception ex) {
				Debug.WriteLine (ex.ToString ());
				return "";
			}

			return null;
		}
		internal static MethodInfo SearchExtMethod (Type t, string methodName)
		{
			string key = t.Name + "." + methodName;
			if (knownExtMethods.ContainsKey (key))
				return knownExtMethods [key];

			//Console.WriteLine ($"*** search extension method: {t};{methodName} => key={key}");

			MethodInfo mi = null;
			mi = GetExtensionMethods (Assembly.GetEntryAssembly (), t, methodName);
			if (mi == null)
				mi = GetExtensionMethods (t.Module.Assembly, t, methodName);

			//add key even if mi is null to prevent searching again and again for propertyless bindings
			knownExtMethods.Add (key, mi);
			return mi;
		}

		public static MethodInfo GetExtensionMethods (Assembly assembly, Type extendedType, string methodName)
		{
			foreach (Type t in assembly.GetTypes ().Where
					(ty => ty.IsDefined (typeof (ExtensionAttribute), false))) {
				foreach (MethodInfo mi in t.GetMethods 
					(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where
						(m=> m.Name == methodName && m.IsDefined (typeof (ExtensionAttribute), false) &&
						 m.GetParameters ().Length == 1)) {
					Type curType = extendedType;
					while (curType != null) {
						if (mi.GetParameters () [0].ParameterType == curType)
							return mi;
						curType = curType.BaseType;
					}						
				}
			
			}
			return null;
		}
		/// <summary>
		/// retrieve event handler in class or ancestors
		/// </summary>
		static FieldInfo getEventHandlerField (Type type, string eventName)
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
		/// search for an implicit conversion method in origine or destination classes
		/// </summary>
		static MethodInfo getImplicitOp(Type origType, Type dstType){
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
#endregion

		/// <summary>
		/// Emits tree parsing command to fetch dest instance starting from orig node
		/// </summary>
		internal static void emitGetInstance (ILGenerator il, NodeAddress orig, NodeAddress dest){
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
		/// <summary>
		/// Emits tree parsing commands to get child starting at root node
		/// </summary>
		/// <param name="il">MSIL generator</param>
		/// <param name="dest">Absolute Node Address of the instance to get</param>
		internal static void emitGetInstance (ILGenerator il, NodeAddress dest){
			if (dest == null)
				return;
			for (int i = 0; i < dest.Count - 1; i++)
				emitGetChild (il, dest [i].CrowType, dest [i + 1].Index);
		}
		/// <summary>
		/// Emits msil to fetch chil instance of current GraphicObject on the stack
		/// </summary>
		/// <param name="il">Il generator</param>
		/// <param name="parentType">Parent type</param>
		/// <param name="index">Index of child, -1 for template root</param>
		internal static void emitGetChild(ILGenerator il, Type parentType, int index){
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
		/// Emit MSIL for conversion from orig type to dest type
		/// </summary>
		internal static void emitConvert(ILGenerator il, Type origType, Type destType){
			if (destType == typeof(object))
				return;
			if (destType == typeof (string)) {
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
			}else if ((origType.IsEnum || origType == typeof(Enum)) && destType.IsEnum) {
				il.Emit (OpCodes.Unbox_Any, destType);
				return;
			}else if (origType.IsValueType) {
				if (destType != origType) {
					MethodInfo miIO = getImplicitOp (origType, destType);
                    if (miIO != null)
                    {
                        System.Reflection.Emit.Label emitCreateDefault = il.DefineLabel();
                        System.Reflection.Emit.Label emitContinue = il.DefineLabel();
                        LocalBuilder lbStruct = il.DeclareLocal(origType);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Brfalse, emitCreateDefault);
                        il.Emit(OpCodes.Unbox_Any, origType);
                        il.Emit(OpCodes.Br, emitContinue);
                        il.MarkLabel(emitCreateDefault);
                        il.Emit(OpCodes.Pop);//pop null value
                        il.Emit(OpCodes.Ldloca, lbStruct);
                        il.Emit(OpCodes.Initobj, origType);
                        il.Emit(OpCodes.Ldloc, lbStruct);
                        il.MarkLabel(emitContinue);
                        il.Emit(OpCodes.Call, miIO);
                    }
                    else
                    {
                        MethodInfo miconv = CompilerServices.GetConvertMethod(destType);
                        if (miconv.IsStatic)
                            il.Emit(OpCodes.Call, miconv);
                        else
                            il.Emit(OpCodes.Callvirt, miconv);
                    }
				}else
					il.Emit (OpCodes.Unbox_Any, destType);//TODO:double check this
			} else {
				if (destType.IsAssignableFrom(origType))
					il.Emit (OpCodes.Castclass, destType);
				else {
					//implicit conversion can't be defined from or to object base class,
					//so we will check if object underlying type is one of the implicit converter of destType
					if (origType == typeof(object)) {//test all implicit converter to destType on obj
						System.Reflection.Emit.Label emitTestNextImpOp;
						System.Reflection.Emit.Label emitImpOpFound = il.DefineLabel ();
						foreach (MethodInfo mi in destType.GetMethods(BindingFlags.Public|BindingFlags.Static)) {
							if (mi.Name == "op_Implicit") {
								if (mi.GetParameters () [0].ParameterType == destType)
									continue;
								emitTestNextImpOp = il.DefineLabel ();
								il.Emit (OpCodes.Dup);
								il.Emit (OpCodes.Isinst, mi.GetParameters () [0].ParameterType);
								il.Emit (OpCodes.Brfalse, emitTestNextImpOp);
								if (mi.GetParameters () [0].ParameterType.IsValueType)
									il.Emit (OpCodes.Unbox_Any, mi.GetParameters () [0].ParameterType);
								else
									il.Emit (OpCodes.Isinst, mi.GetParameters () [0].ParameterType);

								il.Emit (OpCodes.Call, mi);
								il.Emit (OpCodes.Br, emitImpOpFound);

								il.MarkLabel (emitTestNextImpOp);
							}
						}
						//il.Emit (OpCodes.Br, emitImpOpNotFound);
						il.MarkLabel (emitImpOpFound);
					} else {//search both orig and dest types for implicit operators
						MethodInfo miIO = getImplicitOp (origType, destType);
						if (miIO != null)
							il.Emit (OpCodes.Call, miIO);
					}
				}
			}
		}
		/// <summary>
		/// check type of current object on the stack and convert to dest type,
		/// use loc_0 so store it as object!!!
		/// </summary>
		internal static void emitConvert(ILGenerator il, Type dstType){
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
				System.Reflection.Emit.Label emitNullStr = il.DefineLabel ();
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, emitNullStr);
				il.Emit (OpCodes.Callvirt, CompilerServices.miObjToString);
				il.Emit (OpCodes.Br, endConvert);
				il.MarkLabel (emitNullStr);
				il.Emit (OpCodes.Pop);//remove null string from stack
				il.Emit (OpCodes.Ldstr, "");//replace with empty string
			} else if (dstType.IsPrimitive) {
                //il.Emit (OpCodes.Unbox_Any, dstType);
                MethodInfo miconv = CompilerServices.GetConvertMethod(dstType);
                if (miconv.IsStatic)
                    il.Emit(OpCodes.Call, miconv);
                else
                    il.Emit(OpCodes.Callvirt, miconv);
			} else if (dstType.IsValueType) {
				il.Emit (OpCodes.Unbox_Any, dstType);
			} else{
				il.Emit (OpCodes.Stloc_0); //save orig value in loc0
				//first check if not null
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, endConvert);
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
				il.Emit(OpCodes.Newarr, typeof (object));
				il.Emit (OpCodes.Dup);//duplicate the array ref
				il.Emit (OpCodes.Ldc_I4_0);//push the index 0
				il.Emit (OpCodes.Ldloc_0);//push the orig value to convert
				il.Emit (OpCodes.Stelem, typeof (object));//set the array element at index 0
				il.Emit (OpCodes.Callvirt, miMIInvoke);
			}

			il.MarkLabel (endConvert);
		}

		/// <summary>
		/// Removes delegate from event handler by name
		/// </summary>
		static void removeEventHandlerByName(object instance, string eventName, string delegateName){
			Type t = instance.GetType ();
			FieldInfo fiEvt = getEventHandlerField (t, eventName);
			if (fiEvt == null) {
#if DEBUG_BINDING
				Debug.WriteLine ("RemoveHandlerByName: Event '" + eventName + "' not found in " + instance);
#endif
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
		static void removeEventHandlerByTarget(object instance, string eventName, object target){
			Type t = instance.GetType ();
			FieldInfo fiEvt = getEventHandlerField (t, eventName);
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
		/// <summary>
		/// create delegate helper
		/// </summary>
		static Delegate createDel(object instance, Type eventType, string method){
			Type t = instance.GetType ();
			MethodInfo mi = t.GetMethod (method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (mi == null) {
				Debug.WriteLine ("Handler Method '{0}' not found in '{1}'", method, t);
				return null;
			}
			return Delegate.CreateDelegate (eventType, instance, mi);
		}

		internal static Delegate compileDynEventHandler(EventInfo sourceEvent, string expression, NodeAddress currentNode = null){
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
				if (string.IsNullOrEmpty (srcLine))
					continue;
				string [] operandes = srcLine.Trim ().Split (new char [] { '=' });
				if (operandes.Length != 2) //not an affectation
					throw new NotSupportedException ();

				System.Reflection.Emit.Label cancel = il.DefineLabel ();
				System.Reflection.Emit.Label cancelFinalSet = il.DefineLabel ();
				System.Reflection.Emit.Label success = il.DefineLabel ();

				BindingMember lop = new BindingMember (operandes [0].Trim ());
				BindingMember rop = new BindingMember (operandes [1].Trim ());

				il.Emit (OpCodes.Ldarg_0);  //load sender ref onto the stack, the current node

#region Left operande
				PropertyInfo lopPI = null;

				//in dyn handler, no datasource binding, so single name in expression are also handled as current node property
				if (lop.IsSingleName)
					lopPI = lopType.GetProperty (lop.Tokens [0]);
				else if (lop.IsCurrentNodeProperty)
					lopPI = lopType.GetProperty (lop.Tokens [1]);
				else
					lop.emitGetTarget (il, cancel);
#endregion

#region RIGHT OPERANDES
				if (rop.IsStringConstant){
					il.Emit (OpCodes.Ldstr, rop.Tokens[0]);
					lop.emitSetProperty (il);
				}else if (rop.IsSingleName && rop.Tokens[0] == "this"){
					il.Emit (OpCodes.Ldarg_0);  //load sender ref onto the stack, the current node
					lop.emitSetProperty (il);
				}else if (rop.LevelsUp ==0 && !string.IsNullOrEmpty(rop.Tokens[0])) {//parsable constant depending on lop type
					//if left operand is member of current node, it's easy to fetch type, else we should use reflexion in msil
					if (lopPI == null){//accept GraphicObj members, but it's restricive
						//TODO: we should get the parse method by reflexion, or something else
						lopPI = typeof(Widget).GetProperty (lop.Tokens [lop.Tokens.Length-1]);
						if (lopPI == null)
							throw new NotSupportedException ();
					}

					MethodInfo lopParseMi = CompilerServices.miParseEnum;
					if (lopPI.PropertyType.IsEnum){
						//load type of enum
						il.Emit(OpCodes.Ldtoken, lopPI.PropertyType);
						il.Emit(OpCodes.Call, CompilerServices.miGetTypeFromHandle);
						//load enum value name
						il.Emit (OpCodes.Ldstr, operandes [1].Trim ());
						//load false
						il.Emit (OpCodes.Ldc_I4_0);
					}else{
						lopParseMi = lopPI.PropertyType.GetMethod ("Parse");
						if (lopParseMi == null)
							throw new Exception (string.Format
								("IML: no static 'Parse' method found in: {0}", lopPI.PropertyType.Name));

						il.Emit (OpCodes.Ldstr, operandes [1].Trim ());
					}
                    if (lopParseMi.IsStatic)
					    il.Emit (OpCodes.Call, lopParseMi);
                    else
                        il.Emit(OpCodes.Callvirt, lopParseMi);
                    //il.Emit (OpCodes.Unbox_Any, lopPI.PropertyType);
                    //emit left operand assignment
                    il.Emit (OpCodes.Callvirt, lopPI.GetSetMethod());
				} else {//tree parsing and propert gets
					il.Emit (OpCodes.Ldarg_0);  //load sender ref onto the stack, the current node

					rop.emitGetTarget (il, cancelFinalSet);
					rop.emitGetProperty (il, cancelFinalSet);
					lop.emitSetProperty (il);
				}
#endregion

				il.Emit (OpCodes.Br, success);

				il.MarkLabel (cancelFinalSet);
				il.Emit (OpCodes.Pop);	//pop null MemberInfo on the stack causing cancelation
				il.MarkLabel (cancel);
				il.Emit (OpCodes.Pop);	//pop null instance on the stack causing cancelation
				il.MarkLabel (success);
			}

			il.Emit (OpCodes.Ret);

			return dm.CreateDelegate (sourceEvent.EventHandlerType);
		}

		/// <summary>
		/// MSIL helper, go n levels up
		/// </summary>
		/// <returns><c>true</c>, if logical parents are not null</returns>
		/// <param name="instance">Start Instance</param>
		/// <param name="levelCount">Levels to go upward</param>
		internal static ILayoutable goUpNbLevels(ILayoutable instance, int levelCount){
			ILayoutable tmp = instance;
			int i = 0;
			while (tmp != null && i < levelCount) {
				tmp = tmp.LogicalParent;
				i++;
			}
			return tmp;
		}
		/// <summary>
		/// Splits expression on semicolon but ignore those between accolades
		/// </summary>
		internal static string[] splitOnSemiColumnOutsideAccolades (string expression){
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
		/// Try to get the type named strDataType, search first in crow assembly then in
		/// entry assembly.
		/// </summary>
		/// <returns>the corresponding type object if found</returns>
		/// <param name="strDataType">type name</param>
		internal static Type getTypeFromName (string strDataType){
			if (knownTypes.ContainsKey (strDataType))
				return knownTypes [strDataType];
			Type dataType = Type.GetType(strDataType);
			if (dataType != null) {
				knownTypes.Add (strDataType, dataType);
				return dataType;
			}
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies ()) {
				if (a.IsDynamic)
					continue;
				foreach (Type expT in a.GetExportedTypes ()) {
					if (expT.Name != strDataType)
						continue;
					knownTypes.Add (strDataType, expT);
					return expT;
				}
			}
			knownTypes.Add (strDataType, null);
			return null;
		}

		//get value from member of object
		internal static object getDataTypeAndFetch (object data, string fetchMethod){
			Type dataType = data.GetType();
			//Console.WriteLine ($"get data type and fetch {data}.{fetchMethod}");
			MethodInfo miGetDatas = dataType.GetMethod (fetchMethod, new Type[] {});
			if (miGetDatas == null)
				miGetDatas = CompilerServices.SearchExtMethod (dataType, fetchMethod);

			if (miGetDatas == null) {//in last resort, search among properties
				PropertyInfo piDatas = dataType.GetProperty (fetchMethod);
				if (piDatas == null) {
					FieldInfo fiDatas = dataType.GetField (fetchMethod);
					if (fiDatas == null)//and among fields
						throw new Exception ("Fetch data member not found in ItemTemplate: " + fetchMethod);
					return fiDatas.GetValue (data);
				}
				miGetDatas = piDatas.GetGetMethod ();
				if (miGetDatas == null)
					throw new Exception ("Read only property for fetching data in ItemTemplate: " + fetchMethod);
			}
			return miGetDatas.IsStatic ?
				miGetDatas.Invoke (null, new object [] { data }) : miGetDatas.Invoke (data, null);
		}
		//TODO:memberinfo found here must be cached
		internal static object getValue (Type dataType, object data, string member)
		{
			//Console.WriteLine ($"get value: {dataType} ; {data} ; {member}");

			MethodInfo miGetDatas = dataType.GetMethod (member, new Type [] { });
			if (miGetDatas != null)
				return miGetDatas.Invoke (data, null);
			MemberInfo mbi = dataType.GetMember (member).FirstOrDefault ();
			if (mbi == null) {
				MethodInfo miExt = CompilerServices.SearchExtMethod (dataType, member);
				if (miExt == null)//and among fields
					throw new Exception ($"member {member} not found in {dataType}");
				return miExt.Invoke (null, new object [] { data });
			}
			if (mbi.MemberType == MemberTypes.Property) {
				miGetDatas = (mbi as PropertyInfo)?.GetGetMethod ();
				if (miGetDatas == null)
					throw new Exception ($"no getter found for property {member} in {dataType}");
				return miGetDatas.Invoke (data, null);
			}

			FieldInfo fi = mbi as FieldInfo;
			if (fi == null)
				throw new Exception ($"member {member} not found in {dataType}");

			return fi.GetValue (data);
		}
		internal static MemberInfo GetMemberInfo (Type dataType, string member, out Type returnType)
		{
			MethodInfo miGetDatas = dataType.GetMethod (member, new Type [] { });
			if (miGetDatas != null) {
				returnType = miGetDatas.ReturnType;
				return miGetDatas;
			}

			MemberInfo mbi = dataType.GetMember (member).FirstOrDefault ();
			if (mbi == null)
				miGetDatas = CompilerServices.SearchExtMethod (dataType, member);
			else {
				if (mbi is FieldInfo) {
					FieldInfo fi = mbi as FieldInfo;
					returnType = fi.FieldType;
					return mbi;
				}
				if (mbi.MemberType == MemberTypes.Property)
					miGetDatas = (mbi as PropertyInfo).GetGetMethod ();
				else
					miGetDatas = mbi as MethodInfo;
			}
			returnType = miGetDatas?.ReturnType;
			return miGetDatas;

		}
		internal static void emitGetMemberValue (ILGenerator il, Type dataType, MemberInfo mi)
		{
			switch (mi.MemberType) {
			case MemberTypes.Method:
				MethodInfo mim = mi as MethodInfo;
				if (mim.IsStatic)
					il.Emit (OpCodes.Call, mim);
				else
					il.Emit (OpCodes.Callvirt, mim);
				break;
			case MemberTypes.Field:
				il.Emit (OpCodes.Ldfld, mi as FieldInfo);
				break;
			}
		}
		//object is already on the stack
		internal static void emitGetMemberValue (ILGenerator il, Type dataType, string member)
		{
			MethodInfo miGetDatas = dataType.GetMethod (member, new Type [] { });
			if (miGetDatas != null) {
				il.Emit (OpCodes.Callvirt, miGetDatas);
				return;
			}
			MemberInfo mbi = dataType.GetMember (member).FirstOrDefault ();
			if (mbi == null) {
				MethodInfo miExt = CompilerServices.SearchExtMethod (dataType, member);
				if (miExt == null)//and among fields
					throw new Exception ($"member {member} not found in {dataType}");
				il.Emit (OpCodes.Call, miExt);
				return;
			}
			if (mbi.MemberType == MemberTypes.Property) {
				miGetDatas = (mbi as PropertyInfo)?.GetGetMethod ();
				if (miGetDatas == null)
					throw new Exception ($"no getter found for property {member} in {dataType}");
				il.Emit (OpCodes.Callvirt, miGetDatas);
				return;
			}

			FieldInfo fi = mbi as FieldInfo;
			if (fi == null)
				throw new Exception ($"member {member} not found in {dataType}");

			il.Emit (OpCodes.Ldfld, fi);
		}
	}
}

