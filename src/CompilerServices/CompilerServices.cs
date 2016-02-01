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
		static int bindingCpt = 0;
		string dynMethodId = "";
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
				

		public MemberReference Source;
		public MemberReference Target;

		public string Expression;

		#region CTOR
		public Binding(){}
		public Binding(MemberReference _source, string _expression)
		{
			Source = _source;
			Expression = _expression;
		}
		#endregion

		public bool FindTarget(){
			string member = null;

			//if binding exp = '{}' => binding is done on datasource
			if (string.IsNullOrEmpty (Expression)) {
				Target = new MemberReference ((Source.Instance as GraphicObject).DataSource);
				return true;
			}

			string[] bindingExp = Expression.Split ('/');

			if (bindingExp.Length == 1) {
				//datasource binding
				Target = new MemberReference((Source.Instance as GraphicObject).DataSource);
				member = bindingExp [0];
			} else {
				int ptr = 0;
				ILayoutable tmp = Source.Instance as ILayoutable;
				if (string.IsNullOrEmpty (bindingExp [0])) {
					//if exp start with '/' => Graphic tree parsing start at top container
					tmp = tmp.HostContainer as ILayoutable;
					ptr++;
				}
				while (ptr < bindingExp.Length - 1) {
					if (tmp == null)
						return false;
					if (bindingExp [ptr] == "..")
						tmp = tmp.Parent as ILayoutable;
					else if (bindingExp [ptr] == ".") {
						if (ptr > 0)
							throw new Exception ("Syntax error in binding, './' may only appear in first position");						
						tmp = Source.Instance as ILayoutable;
					}else
						tmp = (tmp as GraphicObject).FindByName (bindingExp [ptr]);
					ptr++;
				}

				if (tmp == null)
					return false;

				string[] bindTrg = bindingExp [ptr].Split ('.');

				if (bindTrg.Length == 1)
					member = bindTrg [0];
				else if (bindTrg.Length == 2){
					tmp = (tmp as GraphicObject).FindByName (bindTrg [0]);
					member = bindTrg [1];
				} else
					throw new Exception ("Syntax error in binding, expected 'go dot member'");

				Target = new MemberReference(tmp);
			}
			if (Target == null) {
				Debug.WriteLine ("Binding Source is null: " + Expression);
				return false;
			}

			if (Target.FindMember (member))
				return true;
			
			Debug.WriteLine ("Binding member not found: " + member);
			return false;
		}
		public void Reset()
		{
			Target = null;
			dynMethodId = "";
		}
	}



	public static class CompilerServices
	{
		static int dynHandleCpt = 0;

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

