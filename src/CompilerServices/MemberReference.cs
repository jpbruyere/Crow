//
//  MemberReference.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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

