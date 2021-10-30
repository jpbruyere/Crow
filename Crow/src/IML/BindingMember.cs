//
// BindingMember.cs
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
using System.Reflection.Emit;

namespace Crow.IML
{
	/// <summary>
	/// Binding expression parser.
	/// 
	/// Valid tokens in binding expression:
	/// - '../' => 1 level up in graphic tree
	/// - './' or '/' => template root level
	/// - '.Name1.Name2' current level properties
	/// - 'name.prop' named descendant in graphic tree, search with 'FindByName' method of Widget
	/// </summary>
	public class BindingMember
	{
		/// <summary>
		/// true if expression was enclosed in '
		/// </summary>
		public bool IsStringConstant = false;
		/// <summary>
		/// Nb level to go up, '-1' for template root
		/// </summary>
		public int LevelsUp;
		/// <summary>
		/// Remaining string after '/' split, splitted on '.'
		/// </summary>
		public string[] Tokens;

		/// <summary>
		/// Target the template's root node, expression was in the form './name[.name[...]]' or '/name[.name[...]]'
		/// </summary>
		public bool IsTemplateBinding => LevelsUp < 0;

		/// <summary>
		/// No level change and expression was '.name'
		/// </summary>
		/// <value><c>true</c> if this instance is current node property; otherwise, <c>false</c>.</value>
		public bool IsCurrentNodeProperty {
			get {
				return LevelsUp == 0 && ( Tokens.Length == 2 && string.IsNullOrEmpty(Tokens[0]));
			}
		}
		/// <summary>
		/// no level change, and only a single name in Tokens[], that's dataSource member if property binding
		/// </summary>
		public bool IsSingleName => LevelsUp == 0 && Tokens.Length == 1;

		#region CTOR
		/// <summary>
		/// Initializes a new instance of BindingMember.
		/// </summary>
		public BindingMember (){}
		/// <summary>
		/// Initializes a new instance of BindingMember by parsing the string passed as argument
		/// </summary>
		/// <param name="expression">binding expression</param>
		public BindingMember (string expression){
			if (string.IsNullOrEmpty (expression))
				return;

			string[] splitedExp = expression.Trim().Split ('/');

			int ptr = 0;
			if (splitedExp.Length == 1) {
				if (splitedExp [0].StartsWith ("\'",StringComparison.Ordinal)) {
					if (!splitedExp [0].EndsWith ("\'", StringComparison.Ordinal))
						throw new Exception (string.Format
							("IML:malformed string constant in binding expression: {0}", splitedExp [0]));
					Tokens = new string[] { splitedExp [0].Substring (1, splitedExp [0].Length - 2) };
					IsStringConstant = true;
					return;
				}
			} else if (string.IsNullOrEmpty (splitedExp [0])) {
				ptr++;
			} else {
				 if (splitedExp [0] == ".") {//template root
					LevelsUp = -1;
					ptr++;
				} else {
					while (splitedExp [ptr] == "..")
						ptr++;
					LevelsUp = ptr;
				}
			}
			if (ptr != splitedExp.Length - 1)
				throw new Exception ("invalid expresion: " + expression);
			Tokens = splitedExp [ptr].Split ('.');
		}
		#endregion

		/// <summary>
		/// Emits the MSIL instructions to get the target of the binding expression
		/// </summary>
		/// <param name="il">current MSIL generator</param>
		/// <param name="cancel">cancel branching in MSIL if something go wrong</param>
		/// <param name="currentNode">if levelUp is 0, node is templated target is not simple name, name
		/// is search in the current node template content, which is avoid normaly.</param>
		public void emitGetTarget(ILGenerator il, System.Reflection.Emit.Label cancel, NodeAddress currentNode = null)
		{
			if (IsTemplateBinding) {
				System.Reflection.Emit.Label nextLogicParent = il.DefineLabel ();
				il.MarkLabel (nextLogicParent);
				il.Emit (OpCodes.Callvirt, CompilerServices.miGetLogicalParent);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, cancel);
				il.Emit (OpCodes.Isinst, typeof(TemplatedControl));
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, nextLogicParent);
			} else if (LevelsUp > 0) {//go upward in logical tree
				il.Emit (OpCodes.Ldc_I4, LevelsUp);//push arg 2 of goUpLevels
				il.Emit (OpCodes.Call, CompilerServices.miGoUpLevels);
				//test if null
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, cancel);
			}

			if (!string.IsNullOrEmpty (Tokens [0])) {//find by name
				il.Emit (OpCodes.Ldstr, Tokens [0]);
				if (LevelsUp == 0 && currentNode[currentNode.Count-1].HasTemplate)
					//search in template
					il.Emit (OpCodes.Callvirt, CompilerServices.miFindByNameInTemplate);
				else				
					il.Emit (OpCodes.Callvirt, CompilerServices.miFindByName);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, cancel);
			}

			for (int i = 1; i < Tokens.Length -1; i++) {
				System.Reflection.Emit.Label miOK = il.DefineLabel ();
				il.Emit (OpCodes.Dup);//duplicate instance
				il.Emit (OpCodes.Ldstr, Tokens [i]);//load member name
				il.Emit (OpCodes.Call, CompilerServices.miGetMembIinfoWithRefx);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brtrue, miOK);
				il.Emit (OpCodes.Pop);//pop dup instance
				il.Emit (OpCodes.Br, cancel);
				il.MarkLabel (miOK);
				il.Emit (OpCodes.Call, CompilerServices.miGetValWithRefx);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, cancel);
			}
		}
		/// <summary>
		/// Emit the MSIL instructions to get the target property of the binding expression
		/// </summary>
		/// <param name="il">current MSIL generator</param>
		/// <param name="cancel">cancel branching in MSIL if something go wrong</param>
		public void emitGetProperty(ILGenerator il, System.Reflection.Emit.Label cancel) {
			System.Reflection.Emit.Label miOK = il.DefineLabel ();
			il.Emit (OpCodes.Dup);//duplicate instance
			il.Emit (OpCodes.Ldstr, Tokens [Tokens.Length -1]);//load member name
			il.Emit (OpCodes.Call, CompilerServices.miGetMembIinfoWithRefx);
			il.Emit (OpCodes.Dup);
			il.Emit (OpCodes.Brtrue, miOK);
			il.Emit (OpCodes.Pop);//pop dup instance
			il.Emit (OpCodes.Br, cancel);
			il.MarkLabel (miOK);
			il.Emit (OpCodes.Call, CompilerServices.miGetValWithRefx);
		}
		/// <summary>
		/// Emit the MSIL instructions to set the target property of the binding expression
		/// </summary>
		/// <param name="il">current MSIL generator</param>
		public void emitSetProperty(ILGenerator il) {
			il.Emit (OpCodes.Ldstr, Tokens [Tokens.Length -1]);//load member name
			il.Emit (OpCodes.Call, CompilerServices.miSetValWithRefx);
		}
	}
}

