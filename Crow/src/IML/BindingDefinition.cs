//
// BindingDefinition.cs
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

