//
// MemberAddress.cs
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

namespace Crow.IML
{
	/// <summary>
	/// Address member of a node
	/// </summary>
	public struct MemberAddress
	{
		public string memberName;
		public MemberInfo member;
		public NodeAddress Address;

		public PropertyInfo Property { get { return member as PropertyInfo; }}
		public bool IsTemplateBinding { get { return Address == null ? false : Address.Count == 0; }}

		public MemberAddress (NodeAddress _address, string _member, bool findMember = true)
		{
			Address = _address;
			memberName = _member;
			member = null;

			if (Address == null)
				return;
			if (Address.Count == 0)
				return;

			if (!findMember)
				return;
			if (!tryFindMember ())
				throw new Exception ("Member Not Found: " + memberName);
		}
		public MemberAddress (NodeAddress _address, MemberInfo _member)
		{
			Address = _address;
			member = _member;
			memberName = "";

			if (member != null)
				memberName = member.Name;
		}

		#region Equality Compare
		public override bool Equals (object obj)
		{
			return obj is MemberAddress && this == (MemberAddress)obj;
		}
		public override int GetHashCode ()
		{
			return Address.GetHashCode () ^ member.GetHashCode ();
		}
		public static bool operator == (MemberAddress x, MemberAddress y)
		{
			return x.Address == y.Address && x.memberName == y.memberName;
		}
		public static bool operator != (MemberAddress x, MemberAddress y)
		{
			return !(x == y);
		}
		#endregion

		bool tryFindMember ()
		{
#if DEBUG_BINDING_FUNC_CALLS
            Console.WriteLine ($"tryFindMember ({Address},{member})");
#endif
			if (member != null)
				throw new Exception ("member already found");
			if (Address == null)
				return false;
			if (Address.Count == 0)
				return false;
			Type t = Address.LastOrDefault ().CrowType;
			member = t.GetMember (memberName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();

#region search for extensions methods if member not found in type
			if (member == null && !string.IsNullOrEmpty (memberName)) {
				Assembly a = Assembly.GetExecutingAssembly ();
				string mn = memberName;
				member = CompilerServices.GetExtensionMethods (a, t, mn);
			}
#endregion

			return member != null;
		}
	}
}
