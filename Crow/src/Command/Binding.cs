// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Reflection;
using System.Linq;
using EnumsNET;
using Glfw;
using System.Reflection.Emit;

namespace Crow
{
	public class Binding<T> {
		public string SourceMember;
		public Func<T> FetchFunc;

		public Binding (string memberName, Func<T> fetchFunc = null) {
			SourceMember = memberName;
			FetchFunc = fetchFunc;
		}
		public T Get (object instance) {
			MemberInfo mbi = instance.GetType().GetMember (SourceMember, BindingFlags.Instance | BindingFlags.Public ).FirstOrDefault();
			if (mbi is PropertyInfo pi)
				return (T)pi.GetGetMethod ().Invoke (instance, null);
			else if (mbi is FieldInfo fi)
				return (T)fi.GetValue (instance);
			else if (mbi is MethodInfo mi)
				return (T)mi.Invoke (instance, null);
			else
				throw new Exception ($"unsupported member type for Binding<T>: {mbi.MemberType}");
		}
		public Func<T> CreateGetter (object instance) {
			MemberInfo mbi = instance.GetType().GetMember (SourceMember, BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
			if (mbi is PropertyInfo pi) {
				return (Func<T>)Delegate.CreateDelegate (typeof (Func<T>), instance, pi.GetGetMethod ());
			} else if (mbi is FieldInfo fi) {
				DynamicMethod dm = new DynamicMethod($"{fi.ReflectedType.FullName}_fldget", typeof(T), new Type[] { fi.ReflectedType }, false);
				ILGenerator il = dm.GetILGenerator ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, fi);

				il.Emit(OpCodes.Ret);

				return (Func<T>)dm.CreateDelegate (typeof (Func<T>), instance);
			} else
				throw new Exception ("unsupported member type for Binding.CreateGetter");
		}
		public Action<T> CreateSetter (object instance) {
			MemberInfo mbi = instance.GetType().GetMember (SourceMember, BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
			if (mbi is PropertyInfo pi) {
				return (Action<T>)Delegate.CreateDelegate (typeof (Action<T>), instance, pi.GetSetMethod ());
			} else if (mbi is FieldInfo fi) {
				DynamicMethod dm = new DynamicMethod($"{fi.ReflectedType.FullName}_fldset", null, new Type[] { fi.ReflectedType, typeof(T)}, false);
				ILGenerator il = dm.GetILGenerator ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Stfld, fi);

				il.Emit(OpCodes.Ret);

				return (Action<T>)dm.CreateDelegate (typeof (Action<T>), instance);
			} else
				throw new Exception ("unsupported member type for Binding CreateSetter");
		}
	}
}