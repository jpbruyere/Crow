using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Crow
{
	public class MemberReference
	{
		public object Instance;
		public MemberInfo Member;

		public PropertyInfo Property { get { return Member as PropertyInfo; } }
		public FieldInfo Field { get { return Member as FieldInfo; } }
		public EventInfo Event { get { return Member as EventInfo; } }
		public MethodInfo Method { get { return Member as MethodInfo; } }

		public MemberReference(){
		}
		public MemberReference(object _instance, MemberInfo _member = null)
		{
			Instance = _instance;
			Member = _member;
		}
		public bool FindMember(string _memberName)
		{
			if (Instance == null)
				return false;
			Type t = Instance.GetType ();
			Member = t.GetMember (_memberName,BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();

			#region search for extensions methods if member not found in type
			if (Member == null && !string.IsNullOrEmpty(_memberName))
			{
				Assembly a = Assembly.GetExecutingAssembly();
				Member =  CompilerServices.GetExtensionMethods(a, t).Where(em=>em.Name == _memberName).FirstOrDefault();
			}			
			#endregion
		
			return string.IsNullOrEmpty(_memberName) ? false : true;
		}
	}
	public class Binding{
		static int bindingCpt;
		string dynMethodId = "";
		bool resolved;

		public bool TwoWayBinding;

		public string NewDynMethodId {
			get {
				if (!string.IsNullOrEmpty (dynMethodId))
					return dynMethodId;
				dynMethodId = "dynHandle_" + bindingCpt;
				bindingCpt++;
				return dynMethodId;
			}
		}
		public string DynMethodId {
			get { return dynMethodId; }
		}


		public bool Resolved {
			get {
				return resolved;
			}
			set {
				if (value == resolved)
					return;
				#if DEBUG_BINDING
				if (value == true)
					Debug.WriteLine ("\tOk => " + this.ToString());
				else
					Debug.WriteLine ("\tresolved state reseted => " + this.ToString());
				#endif
				resolved = value;
			}
		}

		public MemberReference Target;
		public MemberReference Source;

		public string Expression;

		#region CTOR
		public Binding(){}
		public Binding(MemberReference _target, string _expression)
		{
			Target = _target;
			Expression = _expression;
		}
		public Binding(object _target, string _member, string _expression)
		{
			Target = new MemberReference (_target, _target.GetType().GetMember (_member) [0]);
			Expression = _expression;
		}
		public Binding(object _target, string _targetMember, object _source, string _sourceMember)
		{
			Target = new MemberReference (_target, _target.GetType().GetMember (_targetMember) [0]);
			Source = new MemberReference (_source, _source.GetType().GetMember (_sourceMember) [0]);
		}
		public Binding(MemberReference _target, MemberReference _source)
		{
			Target = _target;
			Source = _source;
		}
		#endregion

		public bool FindSource(){
			if (Source != null)
				return true;
			
			string member = null;

			//if binding exp = '{}' => binding is done on datasource
			if (string.IsNullOrEmpty (Expression)) {
				Object o = (Target.Instance as GraphicObject).DataSource;
				if (o == null)
					return false;
				Source = new MemberReference (o);
				return true;
			}

			string expression = Expression;

			if (Expression.StartsWith ("²")) {
				expression = expression.Substring (1);
				TwoWayBinding = true;
			}

			string[] bindingExp = expression.Split ('/');

			if (bindingExp.Length == 1) {
				//datasource binding
				Source = new MemberReference((Target.Instance as GraphicObject).DataSource);
				member = bindingExp [0];
			} else {
				int ptr = 0;
				ILayoutable tmp = Target.Instance as ILayoutable;
				if (string.IsNullOrEmpty (bindingExp [0])) {
					//if exp start with '/' => Graphic tree parsing start at top container
					tmp = Interface.CurrentInterface as ILayoutable;
					ptr++;
				}
				while (ptr < bindingExp.Length - 1) {
					if (tmp == null) {
						#if DEBUG_BINDING
						Debug.WriteLine ("\tERROR: target not found => " + this.ToString());
						#endif
						return false;
					}
					if (bindingExp [ptr] == "..")
						tmp = tmp.LogicalParent;
					else if (bindingExp [ptr] == ".") {
						if (ptr > 0)
							throw new Exception ("Syntax error in binding, './' may only appear in first position");						
						tmp = Target.Instance as ILayoutable;
					}else
						tmp = (tmp as GraphicObject).FindByName (bindingExp [ptr]);
					ptr++;
				}

				if (tmp == null) {
					#if DEBUG_BINDING
					Debug.WriteLine ("\tERROR: target not found => " + this.ToString());
					#endif
					return false;
				}

				string[] bindTrg = bindingExp [ptr].Split ('.');

				if (bindTrg.Length == 1)
					member = bindTrg [0];
				else if (bindTrg.Length == 2){
					tmp = (tmp as GraphicObject).FindByName (bindTrg [0]);
					member = bindTrg [1];
				} else
					throw new Exception ("Syntax error in binding, expected 'go dot member'");

				Source = new MemberReference(tmp);
			}
			if (Source == null) {
				Debug.WriteLine ("Binding Source is null: " + Expression);
				return false;
			}

			if (Source.FindMember (member)) {
				if (TwoWayBinding) {
					IBindable source = Source.Instance as IBindable;
					if (source == null)
						throw new Exception (Target.Instance + " does not implement IBindable for 2 way bindings");
					source.Bindings.Add (new Binding (Source, Target));
				}
				return true;
			}
			
			Debug.WriteLine ("Binding member not found: " + member);
			Source = null;
			return false;
		}
		public void Reset()
		{
			Source = null;
			dynMethodId = "";
			Resolved = false;
		}
		public override string ToString ()
		{
			return string.Format ("[Binding: {0}.{1} <= {2}]", Target.Instance, Target.Member.Name, Expression);
		}
	}



	public static class CompilerServices
	{
		public static void ResolveBindings(List<Binding> Bindings)
		{
			if (Bindings == null)
				return;
			if (Bindings.Count == 0)
				return;
//#if DEBUG_BINDING
//			Debug.WriteLine ("Resolve Bindings => " + this.ToString ());
//#endif
			//grouped bindings by Instance of Source
			Dictionary<object,List<Binding>> resolved = new Dictionary<object, List<Binding>>();

			foreach (Binding b in Bindings) {
				if (b.Resolved)
					continue;
				if (b.Target.Member.MemberType == MemberTypes.Event) {
					if (b.Expression.StartsWith("{")){
						CompilerServices.CompileEventSource(b);
						continue;
					}
					if (!b.FindSource ())
						continue;
					//register handler for event
					if (b.Source.Method == null) {
						Debug.WriteLine ("\tError: Handler Method not found: " + b.ToString());
						continue;
					}
					try {
						MethodInfo addHandler = b.Target.Event.GetAddMethod ();
						Delegate del = Delegate.CreateDelegate (b.Target.Event.EventHandlerType, b.Source.Instance, b.Source.Method);
						addHandler.Invoke (b.Target.Instance, new object[] { del });

#if DEBUG_BINDING
						Debug.WriteLine ("\tHandler binded => " + b.ToString());
#endif
						b.Resolved = true;
					} catch (Exception ex) {
						Debug.WriteLine ("\tERROR: " + ex.ToString());
					}
					continue;
				}

				if (!b.FindSource ())
					continue;

				List<Binding> bindings = null;
				if (!resolved.TryGetValue (b.Source.Instance, out bindings)) {
					bindings = new List<Binding> ();
					resolved [b.Source.Instance] = bindings;
				}
				bindings.Add (b);
				b.Resolved = true;
			}

			MethodInfo stringEquals = typeof(string).GetMethod
				("Equals", new Type[3] {typeof(string), typeof(string), typeof(StringComparison)});
			Type target_Type = Bindings[0].Target.Instance.GetType();
			EventInfo ei = typeof(IValueChange).GetEvent("ValueChanged");
			MethodInfo evtInvoke = ei.EventHandlerType.GetMethod ("Invoke");
			ParameterInfo[] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;
			Type[] args = {typeof(object), typeof(object),handlerArgsType};
			FieldInfo fiNewValue = typeof(ValueChangeEventArgs).GetField("NewValue");
			FieldInfo fiMbName = typeof(ValueChangeEventArgs).GetField("MemberName");

			//group;only one dynMethods by target (valuechanged event source)
			//changed value name tested in switch
			//IEnumerable<Binding[]> groupedByTarget = resolved.GroupBy (g => g.Target.Instance, g => g, (k, g) => g.ToArray ());
			foreach (List<Binding> grouped in resolved.Values) {
				int i = 0;
				Type source_Type = grouped[0].Source.Instance.GetType();

				DynamicMethod dm = null;
				ILGenerator il = null;

				System.Reflection.Emit.Label[] jumpTable = null;
				System.Reflection.Emit.Label endMethod = new System.Reflection.Emit.Label();

#region Retrieve EventHandler parameter type
				//EventInfo ei = targetType.GetEvent ("ValueChanged");
				//no dynamic update if ValueChanged interface is not implemented
				if (source_Type.GetInterfaces().Contains(typeof(IValueChange))){
					dm = new DynamicMethod(grouped[0].NewDynMethodId,
						MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
						CallingConventions.Standard,
						typeof(void),
						args,
						target_Type,true);

					il = dm.GetILGenerator(256);

					endMethod = il.DefineLabel();
					jumpTable = new System.Reflection.Emit.Label[grouped.Count];
					for (i = 0; i < grouped.Count; i++)
						jumpTable [i] = il.DefineLabel ();
					il.DeclareLocal(typeof(string));
					il.DeclareLocal(typeof(object));

					il.Emit(OpCodes.Nop);
					il.Emit(OpCodes.Ldarg_0);
					//il.Emit(OpCodes.Isinst, sourceType);
					//push new value onto stack
					il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Ldfld, fiNewValue);
					il.Emit(OpCodes.Stloc_1);
					//push name
					il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Ldfld, fiMbName);
					il.Emit(OpCodes.Stloc_0);
					il.Emit(OpCodes.Ldloc_0);
					il.Emit(OpCodes.Brfalse, endMethod);
				}
#endregion

				i = 0;
				foreach (Binding b in grouped) {
#region initialize target with actual value
					object targetValue = null;
					if (b.Source.Member != null){
						if (b.Source.Member.MemberType == MemberTypes.Property)
							targetValue = b.Source.Property.GetGetMethod ().Invoke (b.Source.Instance, null);
						else if (b.Source.Member.MemberType == MemberTypes.Field)
							targetValue = b.Source.Field.GetValue (b.Source.Instance);
						else if (b.Source.Member.MemberType == MemberTypes.Method){
							MethodInfo mthSrc = b.Source.Method;
							if (mthSrc.IsDefined(typeof(ExtensionAttribute), false))
								targetValue = mthSrc.Invoke(null, new object[] {b.Source.Instance});
							else
								targetValue = mthSrc.Invoke(b.Source.Instance, null);
						}else
							throw new Exception ("unandled source member type for binding");
					}else if (string.IsNullOrEmpty(b.Expression))
						targetValue= grouped [0].Source.Instance;//empty binding exp=> bound to target object by default
					//TODO: handle other dest type conversions
					if (b.Target.Property.PropertyType == typeof(string)){
						if (targetValue == null){
							//set default value

						}else
							targetValue = targetValue.ToString ();
					}
					try {
						if (targetValue != null)
							b.Target.Property.GetSetMethod ().Invoke
							(b.Target.Instance, new object [] { b.Target.Property.PropertyType.Cast (targetValue) });
						else
							b.Target.Property.GetSetMethod ().Invoke
							(b.Target.Instance, new object [] { targetValue });
					} catch (Exception ex) {
						Debug.WriteLine (ex.ToString ());
					}
#endregion

					//if no dyn update, skip jump table
					if (il == null)
						continue;

					il.Emit (OpCodes.Ldloc_0);
					if (b.Source.Member != null)
						il.Emit (OpCodes.Ldstr, b.Source.Member.Name);
					else
						il.Emit (OpCodes.Ldstr, b.Expression.Split('/').LastOrDefault());
					il.Emit (OpCodes.Ldc_I4_4);//StringComparison.Ordinal
					il.Emit (OpCodes.Callvirt, stringEquals);
					il.Emit (OpCodes.Brtrue, jumpTable[i]);
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
					il.Emit (OpCodes.Unbox_Any, b.Target.Property.PropertyType);
					il.Emit (OpCodes.Callvirt, b.Target.Property.GetSetMethod ());
					il.Emit (OpCodes.Br, endMethod);

					il.MarkLabel (labSetValue);
					//new value not null

					//by default, source value type is deducted from target member type to allow
					//memberless binding, if targetMember exists, it will be used to determine target
					//value type for conversion
					Type sourceValueType = b.Target.Property.PropertyType;
					if (b.Source.Member != null) {
						if (b.Source.Member.MemberType == MemberTypes.Property)
							sourceValueType = b.Source.Property.PropertyType;
						else if (b.Source.Member.MemberType == MemberTypes.Field)
							sourceValueType = b.Source.Field.FieldType;
						else
							throw new Exception ("unhandle target member type in binding");
					}



					if (b.Target.Property.PropertyType == typeof(string)) {
						MemberReference tostring = new MemberReference (b.Target.Instance);
						if (!tostring.FindMember ("ToString"))
							throw new Exception ("ToString method not found");
						il.Emit (OpCodes.Callvirt, tostring.Method);
					} else if (!sourceValueType.IsValueType)
						il.Emit (OpCodes.Castclass, sourceValueType);
					else if (b.Target.Property.PropertyType != sourceValueType) {
						il.Emit (OpCodes.Callvirt, CompilerServices.GetConvertMethod (b.Target.Property.PropertyType));
					}else
						il.Emit(OpCodes.Unbox_Any, b.Target.Property.PropertyType);

					il.Emit(OpCodes.Callvirt, b.Target.Property.GetSetMethod());

					//il.BeginCatchBlock (typeof (Exception));
					//il.Emit (OpCodes.Pop);
					//il.EndExceptionBlock ();

					il.Emit (OpCodes.Br, endMethod);
					i++;

				}
				il.MarkLabel(endMethod);
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);

				Delegate del = dm.CreateDelegate(ei.EventHandlerType, Bindings[0].Target.Instance);
				MethodInfo addHandler = ei.GetAddMethod ();
				addHandler.Invoke(grouped [0].Source.Instance, new object[] {del});
			}
		}

		/// <summary>
		/// Compile events expression in GOML attributes
		/// </summary>
		/// <param name="es">Event binding details</param>
		public static void CompileEventSource(Binding binding)
		{
#if DEBUG_BINDING
			Debug.WriteLine ("\tCompile Event Source => " + binding.ToString());
#endif

			Type target_type = binding.Target.Instance.GetType();

#region Retrieve EventHandler parameter type
			MethodInfo evtInvoke = binding.Target.Event.EventHandlerType.GetMethod ("Invoke");
			ParameterInfo[] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;
#endregion

			Type[] args = {typeof(object), typeof(object),handlerArgsType};
			DynamicMethod dm = new DynamicMethod(binding.NewDynMethodId,
				typeof(void),
				args,
				target_type);


#region IL generation
			ILGenerator il = dm.GetILGenerator(256);

			string src = binding.Expression.Trim();

			if (! (src.StartsWith("{") || src.EndsWith ("}")))
				throw new Exception (string.Format("GOML:Malformed {0} Event handler: {1}", binding.Target.Member.Name, binding.Expression));

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
				GraphicObject lopObj = binding.Target.Instance as GraphicObject;	//default left operand base object is
				//the first arg (object sender) of the event handler

				il.Emit(OpCodes.Ldarg_0);	//load sender ref onto the stack

				string[] lopParts = lop.Split (new char[] { '.' });
				if (lopParts.Length > 1) {//should search also for member of es.Source
					MethodInfo FindByNameMi = typeof(GraphicObject).GetMethod("FindByName");
					for (int j = 0; j < lopParts.Length - 1; j++) {
						il.Emit (OpCodes.Ldstr, lopParts[j]);
						il.Emit(OpCodes.Callvirt, FindByNameMi);
					}
				}

				int i = lopParts.Length -1;

				MemberInfo[] lopMbis = lopObj.GetType().GetMember (lopParts[i]);

				if (lopMbis.Length<1)
					throw new Exception (string.Format("CROW BINDING: Member not found '{0}'", lop));

				OpCode lopSetOC;
				dynamic lopSetMbi;
				Type lopT = null;
				switch (lopMbis[0].MemberType) {
				case MemberTypes.Property:
					PropertyInfo lopPi = target_type.GetProperty (lopParts[i]);
					MethodInfo dstMi = lopPi.GetSetMethod ();
					lopT = lopPi.PropertyType;
					lopSetMbi = dstMi;
					lopSetOC = OpCodes.Callvirt;
					break;
				case MemberTypes.Field:
					FieldInfo dstFi = target_type.GetField(lopParts[i]);
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
					if (lopT.IsEnum)
						throw new NotImplementedException();

					MethodInfo lopParseMi = lopT.GetMethod("Parse");
					if (lopParseMi == null)
						throw new Exception (string.Format
							("GOML:no parse method found in: {0}", lopT.Name));
					il.Emit(OpCodes.Ldstr, rop);
					il.Emit(OpCodes.Callvirt, lopParseMi);
					il.Emit(OpCodes.Unbox_Any, lopT);
				}

#endregion

				//emit left operand assignment
				il.Emit(lopSetOC, lopSetMbi);
			}

			il.Emit(OpCodes.Ret);

#endregion

			Delegate del = dm.CreateDelegate(binding.Target.Event.EventHandlerType, binding.Target.Instance);
			MethodInfo addHandler = binding.Target.Event.GetAddMethod ();
			addHandler.Invoke(binding.Target.Instance, new object[] {del});

			binding.Resolved = true;
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
			else if( targetType == typeof( float ) )
				name = "ToSingle";
			else if (targetType == typeof (string ) )
				return typeof(object).GetMethod("ToString", Type.EmptyTypes);
			else //try to find implicit convertion
				throw new NotImplementedException( string.Format( "Conversion to {0} is not implemented.", targetType.Name ) );

			return typeof( Convert ).GetMethod( name, BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof( object ) }, null );
		}
#endregion
			
		public static FieldInfo GetEventHandlerField(Type type, string eventName)
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
	
		/// <summary>
		/// Gets extension methods defined in assembley for extendedType
		/// </summary>
		/// <returns>Extension methods enumerable</returns>
		/// <param name="assembly">Assembly</param>
		/// <param name="extendedType">Extended type to search for</param>
		public static IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly,
			Type extendedType)
		{
			IEnumerable<MethodInfo> query = null;
			Type curType = extendedType;

			do {
				query = from type in assembly.GetTypes ()
				        where type.IsSealed && !type.IsGenericType && !type.IsNested
				        from method in type.GetMethods (BindingFlags.Static
				            | BindingFlags.Public | BindingFlags.NonPublic)
				        where method.IsDefined (typeof(ExtensionAttribute), false)
				        where method.GetParameters () [0].ParameterType == curType
				        select method;

				if (query.Count() > 0)
					break;
				
				curType = curType.BaseType;
			} while (curType != null);
				
			return query;
		}
	}
}

