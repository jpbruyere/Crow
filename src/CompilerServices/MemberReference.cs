//
// MemberReference.cs
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
using System.Linq;
using System.Reflection;

namespace Crow
{
	/// <summary>
	/// link MemberInfo and instance in one class
	/// </summary>
	public class MemberReference
	{		
		public object Instance;
		public MemberInfo Member;

		public PropertyInfo Property { get { return Member as PropertyInfo; } }
		public FieldInfo Field { get { return Member as FieldInfo; } }
		public EventInfo Event { get { return Member as EventInfo; } }
		public MethodInfo Method { get { return Member as MethodInfo; } }

		#region CTOR
		public MemberReference () {}
		public MemberReference (object _instance, MemberInfo _member = null)
		{
			Instance = _instance;
			Member = _member;
		}
		#endregion

		/// <summary>
		/// Try to find member by name in instance class or, if not found,
		/// in extension methods
		/// </summary>
		/// <returns>True if found, false otherwise</returns>
		/// <param name="_memberName">Member name to search for</param>
		public bool TryFindMember (string _memberName)
		{
			if (Instance == null)
				return false;
			Type t = Instance.GetType ();
			Member = t.GetMember (_memberName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();

			#region search for extensions methods if member not found in type
			if (Member == null && !string.IsNullOrEmpty (_memberName)) {
				Assembly a = Assembly.GetExecutingAssembly ();
				Member = CompilerServices.GetExtensionMethods (a, t).Where (em => em.Name == _memberName).FirstOrDefault ();
			}
			#endregion

			return Member != null;
		}
		public override string ToString ()
		{
			return string.Format ("{0}.{1}", Instance, Member);
		}
	}
}

