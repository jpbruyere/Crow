using System;
using System.Reflection.Emit;
using System.Reflection;

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
		public static void CompileEventSource(EventSource es)
		{
			Type srcType = es.Source.GetType ();

			#region Retrieve EventHandler parameter type
			EventInfo ei = srcType.GetEvent (es.EventName);
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

			string src = es.Handler.Trim();

			if (! (src.StartsWith("{") || src.EndsWith ("}"))) 
				throw new Exception (string.Format("GOML:Malformed {0} Event handler: {1}", es.EventName, es.Handler));

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

			FieldInfo evtFi = getEventHandlerField (srcType, es.EventName);
			Delegate del = dm.CreateDelegate(evtFi.FieldType);
			evtFi.SetValue(es.Source, del);
		}

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

