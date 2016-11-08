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
namespace Crow.IML
{
	/// <summary>
	/// Address member of a node
	/// </summary>
	public struct MemberAddress
	{		
		public NodeAddress Address;
		public string Name;

		public MemberAddress (NodeAddress _address, string _member)
		{
			Address = _address;
			Name = _member;
		}

		#region Equality Compare
		public override bool Equals (object obj)
		{
			return obj is MemberAddress && this == (MemberAddress)obj;
		}
		public override int GetHashCode ()
		{
			return Address.GetHashCode () ^ Name.GetHashCode ();
		}
		public static bool operator == (MemberAddress x, MemberAddress y)
		{
			return x.Address == y.Address && x.Name == y.Name;
		}
		public static bool operator != (MemberAddress x, MemberAddress y)
		{
			return !(x == y);
		}
		#endregion
	}
}
