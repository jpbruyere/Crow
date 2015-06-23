using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Linq;


namespace go
{
	public static class CompilerServices
	{
		public static void createDynHandler(string eventName, 
			object dstObj, Type handlerArgsType, string destProp, string src)
		{
			Type dstType = dstObj.GetType ();

			Type[] args = {typeof(object), handlerArgsType};
			DynamicMethod hello = new DynamicMethod("dynHandle",
				typeof(void), 
				args, 
				dstType.Module);

			MethodInfo dstMi = dstType.GetProperty (destProp).GetSetMethod ();
			FieldInfo srcFi = typeof(go.Color).GetField (src, BindingFlags.Static|BindingFlags.Public);
			ILGenerator il = hello.GetILGenerator(256);

			il.Emit(OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldsfld, srcFi);
			il.Emit(OpCodes.Callvirt, dstMi);
			il.Emit(OpCodes.Ret);
			//hello.DefineParameter(1, ParameterAttributes.In, "instance");
			//hello.DefineParameter(2, ParameterAttributes.In, "value");
			FieldInfo fi = getEventHandlerField (dstType, eventName);
			Delegate del = hello.CreateDelegate(fi.FieldType);
			fi.SetValue(dstObj, del);

		}
		static int dynHandleCpt = 0;
		public static void CompileEventSource(DynAttribute es)
		{
			Type srcType = es.Source.GetType ();

			#region Retrieve EventHandler parameter type
			EventInfo ei = srcType.GetEvent (es.MemberName);
			MethodInfo invoke = ei.EventHandlerType.GetMethod ("Invoke");
			ParameterInfo[] pars = invoke.GetParameters ();

			Type handlerArgsType = pars [1].ParameterType;
			#endregion

			Type[] args = {typeof(object), handlerArgsType};
			DynamicMethod dm = new DynamicMethod("dynHandle_" + dynHandleCpt,
				typeof(void), 
				args, 
				srcType.Module);

			#region IL generation
			ILGenerator il = dm.GetILGenerator(256);

			string src = es.Value.Trim();

			if (! (src.StartsWith("{") || src.EndsWith ("}"))) 
				throw new Exception (string.Format("GOML:Malformed {0} Event handler: {1}", es.MemberName, es.Value));

			src = src.Substring (1, src.Length - 2);
			string[] srcLines = src.Split (new char[] { ';' });

			foreach (string srcLine in srcLines) {
				string statement = srcLine.Trim ();

				string[] operandes = statement.Split (new char[] { '=' });
				if (operandes.Length < 2) //not an affectation
				{
					continue;
				}
				string lop = operandes [0].Trim ();
				string rop = operandes [operandes.Length-1].Trim ();

				#region LEFT OPERANDES
				GraphicObject lopObj = es.Source;	//default left operand base object is 
													//the first arg (object sender) of the event handler

				string[] lopParts = lop.Split (new char[] { '.' });
				if (lopParts.Length == 2) {//should search also for member of es.Source
					lopObj = es.Source.FindByName (lopParts [0]);
					if (lopObj==null)
						throw new Exception (string.Format("GOML:Unknown name: {0}", lopParts[0]));
					//TODO: should create private member holding ref of lopObj, and emit
					//a call to FindByName(lopObjName) during #ctor or in a onLoad func or evt handler
					throw new Exception (string.Format("GOML:obj tree ref not yet implemented", lopParts[0]));
				}else
					il.Emit(OpCodes.Ldarg_0);	//load sender ref onto the stack

				int i = lopParts.Length -1;

				MemberInfo lopMbi = lopObj.GetType().GetMember (lopParts[i])[0];
				OpCode lopSetOC;
				dynamic lopSetMbi;
				Type lopT = null;
				switch (lopMbi.MemberType) {
				case MemberTypes.Property:
					PropertyInfo lopPi = srcType.GetProperty (lopParts[i]);
					MethodInfo dstMi = lopPi.GetSetMethod ();
					lopT = lopPi.PropertyType;
					lopSetMbi = dstMi;
					lopSetOC = OpCodes.Callvirt;
					break;
				case MemberTypes.Field:
					FieldInfo dstFi = srcType.GetField(lopParts[i]);
					lopT = dstFi.FieldType;
					lopSetMbi = dstFi;
					lopSetOC = OpCodes.Stfld;
					break;
				default:
					throw new Exception (string.Format("GOML:member type not handle: {0}", lopParts[i]));
				}  
				#endregion

				#region RIGHT OPERANDES
				if (rop.StartsWith("\'")){
					if (!rop.EndsWith("\'"))
						throw new Exception (string.Format
						("GOML:malformed string constant in handler: {0}", rop));	
					string strcst = rop.Substring (1, rop.Length - 2);

					il.Emit(OpCodes.Ldstr,strcst);

				}else{
					//search for a static field in left operand type named 'rop name'
					FieldInfo ropFi = lopT.GetField (rop, BindingFlags.Static|BindingFlags.Public);
					if (ropFi != null)
					{
						il.Emit (OpCodes.Ldsfld, ropFi);
					}else{
						//search if parsing methods are present
						MethodInfo lopTryParseMi = lopT.GetMethod("TryParse");

					}
				}

				#endregion

				//emit left operand assignment
				il.Emit(lopSetOC, lopSetMbi);
			}
				
			il.Emit(OpCodes.Ret);

			#endregion

			FieldInfo evtFi = getEventHandlerField (srcType, es.MemberName);
			Delegate del = dm.CreateDelegate(evtFi.FieldType);
			evtFi.SetValue(es.Source, del);
		}

		public static void ResolveBinding(DynAttribute binding, object _source)
		{			
			object srcGO = _source;
			string statement = binding.Value;

			Type srcType = _source.GetType ();
			Type dstType = binding.Source.GetType ();

			MemberInfo miDst = dstType.GetMember (binding.MemberName).FirstOrDefault ();
			string[] bindingExp = binding.Value.Split ('/');
			MemberInfo miSrc;
			if (bindingExp.Length > 1){
				int i = 0;
				srcGO = binding.Source; //starts parsing from current GO
				while (bindingExp [i] == "..") {
					srcGO = (srcGO as ILayoutable).Parent as GraphicObject;
					i++;
				}
				string[] bindTrg = bindingExp [i].Split ('.');
				if (bindTrg.Length == 1)
					srcType = srcGO.GetType ();
				else {
					srcGO = (srcGO as GraphicObject).FindByName (bindTrg [0]);
					srcType = srcGO.GetType ();
				}
				statement = bindTrg.LastOrDefault ();
			}
			miSrc = srcType.GetMember (statement).FirstOrDefault ();
				

			#region initialize target with actual value
			object srcVal = null;
			if (miSrc == null)
				srcVal = srcGO;//if no member is provided for binding, source raw value is taken
			else {
				if (miSrc.MemberType == MemberTypes.Property)
					srcVal = (miSrc as PropertyInfo).GetGetMethod ().Invoke (srcGO, null);
				else if (miSrc.MemberType == MemberTypes.Field)
					srcVal = (miSrc as FieldInfo).GetValue (srcGO);
				else
					throw new Exception ("unandled source member type for binding");
			}
			if (miDst.MemberType == MemberTypes.Property) {
				PropertyInfo piDst = miDst as PropertyInfo;
				//TODO: handle other dest type conversions
				if (piDst.PropertyType == typeof(string))
					srcVal = srcVal.ToString ();
				piDst.GetSetMethod ().Invoke (binding.Source, new object[] { srcVal });
			} else if (miDst.MemberType == MemberTypes.Field) {
				FieldInfo fiDst = miDst as FieldInfo;
				if (fiDst.FieldType == typeof(string))
					srcVal = srcVal.ToString ();
				fiDst.SetValue (binding.Source, srcVal );
			}else
				throw new Exception("unandled destination member type for binding");
			#endregion

			#region Retrieve EventHandler parameter type
			EventInfo ei = srcType.GetEvent ("ValueChanged");
			if (ei == null)
				return; //no dynamic update if ValueChanged interface is not implemented

			MethodInfo evtInvoke = ei.EventHandlerType.GetMethod ("Invoke");
			ParameterInfo[] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;

			#endregion


			Type[] args = {typeof(object), handlerArgsType};
			DynamicMethod dm = new DynamicMethod("dynHandle_" + dynHandleCpt,
				typeof(void), 
				args, 
				srcType.Module, true);

			//register target object reference
			int dstIdx = Interface.References.IndexOf(binding.Source);

			if (dstIdx < 0) {
				dstIdx = Interface.References.Count;
				Interface.References.Add (binding.Source);
			}



			#region IL generation
			ILGenerator il = dm.GetILGenerator(256);

			System.Reflection.Emit.Label labFailed = il.DefineLabel();
			System.Reflection.Emit.Label labContinue = il.DefineLabel();

			#region test if valueChange event is the correct one
			il.Emit (OpCodes.Ldstr, statement);
			//push name from arg
			il.Emit(OpCodes.Ldarg_1);
			FieldInfo fiMbName = typeof(ValueChangeEventArgs).GetField("MemberName");
			il.Emit(OpCodes.Ldfld, fiMbName);
			MethodInfo miStrEqu = typeof(string).GetMethod("op_Inequality", new Type[] {typeof(string),typeof(string)});
			il.Emit(OpCodes.Call, miStrEqu);
			il.Emit(OpCodes.Brfalse_S, labContinue);
			il.Emit(OpCodes.Br_S, labFailed);
			il.MarkLabel(labContinue);
			#endregion

//			string[] srcLines = binding.Value.Trim().Split (new char[] { ';' });
//			foreach (string srcLine in srcLines) {
				//MethodInfo infoWriteLine = typeof(System.Diagnostics.Debug).GetMethod("WriteLine", new Type[] { typeof(string) });
							

				//load target ref onto the stack
				FieldInfo fiRefs = typeof(Interface).GetField("References");
				il.Emit(OpCodes.Ldsfld, fiRefs);
				il.Emit(OpCodes.Ldc_I4, dstIdx);

				MethodInfo miGetRef = Interface.References.GetType().GetMethod("get_Item");
				il.Emit(OpCodes.Callvirt, miGetRef);
				il.Emit(OpCodes.Isinst, dstType);

				//push new value
				il.Emit(OpCodes.Ldarg_1);
				FieldInfo fiNewValue = typeof(ValueChangeEventArgs).GetField("NewValue");
				il.Emit(OpCodes.Ldfld, fiNewValue);


				MethodInfo miToStr = typeof(object).GetMethod("ToString",Type.EmptyTypes);
				PropertyInfo piTarget = dstType.GetProperty(binding.MemberName);

				Type srcValueType = null;
				if (miSrc.MemberType == MemberTypes.Property)
					srcValueType = (miSrc as PropertyInfo).PropertyType;
				else if (miSrc.MemberType == MemberTypes.Field) 
					srcValueType = (miSrc as FieldInfo).FieldType;
				else
					throw new Exception("unandled source member type for binding");
				
				if (!srcValueType.IsValueType)
					il.Emit(OpCodes.Castclass, srcValueType);
				else if (piTarget.PropertyType != srcValueType)
					il.Emit(OpCodes.Callvirt, GetConvertMethod( piTarget.PropertyType ));
				else
					il.Emit(OpCodes.Unbox_Any, piTarget.PropertyType);
			//	if (piTarget.PropertyType == typeof(string))
//				else if ( srcValueType != piTarget.PropertyType)
//					
				
				il.Emit(OpCodes.Callvirt, piTarget.GetSetMethod());
			//}
			il.MarkLabel(labFailed);
			il.Emit(OpCodes.Ret);

			#endregion

			Delegate del = dm.CreateDelegate(ei.EventHandlerType);
			MethodInfo addHandler = ei.GetAddMethod ();
			//Delegate del = dm.CreateDelegate(typeof(System.EventHandler));
			addHandler.Invoke(srcGO, new object[] {del});
		}

		#region conversions

		internal static MethodInfo GetConvertMethod( Type targetType )
		{
			string name;

			if( targetType == typeof( bool ) )
				name = "ToBoolean";
			else if( targetType == typeof( byte ) )
				name = "ToByte";
			else if( targetType == typeof( short ) )
				name = "ToInt16";
			else if( targetType == typeof( int ) )
				name = "ToInt32";
			else if( targetType == typeof( long ) )
				name = "ToInt64";
			else if( targetType == typeof( double ) )
				name = "ToDouble";
			else if (targetType == typeof (string ) )
				return typeof(object).GetMethod("ToString", Type.EmptyTypes);
			else
				throw new NotImplementedException( string.Format( "Conversion to {0} is not implemented.", targetType.Name ) );

			return typeof( Convert ).GetMethod( name, BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof( object ) }, null );
		}
		#endregion
			
		public static FieldInfo getEventHandlerField(Type type, string eventName)
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
			} while(fi == null);
			return fi;
		}
	}
}

