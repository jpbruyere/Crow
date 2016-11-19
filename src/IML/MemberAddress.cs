//
//  MemberAddress.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
		string memberName;
		MemberInfo member;
		public NodeAddress Address;

//		public string Name {
//			get { return memberName; } 
//			set { memberName = value; }
//		}

		public MemberAddress (NodeAddress _address, string _member, bool findMember = true)
		{
			Address = _address;
			memberName = _member;
			member = null;

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
			if (member != null)
				throw new Exception ("member already found");
			if (Address == null)
				return false;
			Type t = Address.LastOrDefault ().CrowType;
			member = t.GetMember (memberName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();

			#region search for extensions methods if member not found in type
			if (member == null && !string.IsNullOrEmpty (memberName)) {
				Assembly a = Assembly.GetExecutingAssembly ();
				string mn = memberName;
				member = CompilerServices.GetExtensionMethods (a, t).Where (em => em.Name == mn).FirstOrDefault ();
			}
			#endregion

			return member != null;
		}
	}
}
