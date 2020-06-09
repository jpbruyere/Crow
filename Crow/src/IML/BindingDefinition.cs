// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow.IML
{
	/// <summary>
	/// store binding source and target addresses and member names
	/// </summary>
	public class BindingDefinition
	{
		public NodeAddress SourceNA = null;//the widget declaring this binding in one of its member
		public string SourceMember = "";//the member where the binding string has been found
		public NodeAddress TargetNA = null;//
		public string TargetMember = "";
		public string TargetName = "";
		public bool TwoWay = false;//two way binding
		public Type targetType = null;//added to store dataSourceType if set

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

