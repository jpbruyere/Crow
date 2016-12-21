//
//  BindingDefinition.cs
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
	/// store binding source and target addresses and member names
	/// </summary>
	public class BindingDefinition
	{
		public NodeAddress SourceNA;
		public string SourceMember;
		public NodeAddress TargetNA;
		public string TargetMember;
		public string TargetName;
		public bool TwoWay = false;

		#region CTOR
		public BindingDefinition (NodeAddress _sourceNA, string _sourceMember, NodeAddress _targetNA, string _targetMember, string _targetName = "", bool _twoWay = false)
		{
			SourceNA = _sourceNA;
			SourceMember = _sourceMember;
			TargetNA = _targetNA;
			TargetMember = _targetMember;
			TargetName = _targetName;
			TwoWay = _twoWay;
		}
		#endregion

		/// <summary>
		/// replace the target node address with corresponding named node address, and clear the target name once resolved
		/// </summary>
		/// <param name="newTargetNA">Named Node</param>
		public void ResolveTargetName(NodeAddress newTargetNA){
			TargetNA = newTargetNA;
			TargetName = "";
		}
	}
}

