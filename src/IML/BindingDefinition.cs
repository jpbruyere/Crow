﻿//
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
		public NodeAddress SourceNA = null;
		public string SourceMember = "";
		public NodeAddress TargetNA = null;
		public string TargetMember = "";
		public string TargetName = "";
		public bool TwoWay = false;

		#region CTOR
		public BindingDefinition (NodeAddress _sourceNA, string _sourceMember){
			SourceNA = _sourceNA;
			SourceMember = _sourceMember;
		}
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

		public bool IsDataSourceBinding { get { return TargetNA == null; }}
		public bool IsTemplateBinding { get { return IsDataSourceBinding ? false : TargetNA.Count == 0; }}
		public bool HasUnresolvedTargetName { get { return !string.IsNullOrEmpty(TargetName); }}
		public MemberAddress SourceMemberAddress { get { return new MemberAddress (SourceNA, SourceMember);}}
		public MemberAddress TargetMemberAddress { get { return new MemberAddress (TargetNA, TargetMember);}}

		/// <summary>
		/// replace the target node address with corresponding named node address, and clear the target name once resolved
		/// </summary>
		/// <param name="newTargetNA">Named Node</param>
		public void ResolveTargetName(NodeAddress newTargetNA){
			TargetNA = newTargetNA;
			TargetName = "";
		}

		public override string ToString ()
		{
			string tmp = string.Format ("Source:{0}.{1}", SourceNA, SourceMember);
			if (TwoWay)
				tmp += " <=> ";
			else
				tmp += " <=  ";
					
			if (string.IsNullOrEmpty (TargetName))
				tmp += string.Format ("Target:{0}.{1}]", TargetNA, TargetMember);
			else
				tmp += string.Format ("Target:{0}.{1}.{2}]", TargetNA, TargetName, TargetMember);
			return tmp;
		}
	}
}

