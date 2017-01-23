//
//  BindingMember.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
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
using System.Reflection.Emit;

namespace Crow
{
	/// <summary>
	/// Expression token, a variable, a string constant or a parsable constant (having a static Parse method)
	/// '../' => 1 level up in graphic tree
	/// './' or '/' => template root level
	/// '.Name1.Name2' current level properties
	/// 'name.prop' named descendant in graphic tree, search with 'FindByName' method of GraphicObject
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
		public bool IsTemplateBinding { get { return LevelsUp < 0; }}

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
		public bool IsSingleName { get { return LevelsUp == 0 && Tokens.Length == 1; }}

		#region CTOR
		public BindingMember (){}
		public BindingMember (string expression){
			if (string.IsNullOrEmpty (expression))
				return;

			string[] splitedExp = expression.Trim().Split ('/');

			int ptr = 0;
			if (splitedExp.Length == 1) {
				if (splitedExp [0].StartsWith ("\'")) {
					if (!splitedExp [0].EndsWith ("\'"))
						throw new Exception (string.Format
							("IML:malformed string constant in binding expression: {0}", splitedExp [0]));
					Tokens = new string[] { splitedExp [0].Substring (1, splitedExp [0].Length - 2) };
					IsStringConstant = true;
					return;
				}
			} else {
				if (string.IsNullOrEmpty (splitedExp [0]) || splitedExp [0] == ".") {//template root
					LevelsUp = -1;
					ptr++;
				} else {
					while (splitedExp [ptr] == "..")
						ptr++;
				}
			}
			if (ptr != splitedExp.Length - 1)
				throw new Exception ("invalid expresion: " + expression);
			Tokens = splitedExp [ptr].Split ('.');
		}
		#endregion

		public void emitGetTarget(ILGenerator il, System.Reflection.Emit.Label cancel){

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
				il.Emit (OpCodes.Ldind_I4, LevelsUp);//push arg 2 of goUpLevels
				il.Emit (OpCodes.Callvirt, CompilerServices.miGoUpLevels);
				//test if null
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, cancel);
			}

			if (!string.IsNullOrEmpty (Tokens [0])) {//find by name
				il.Emit (OpCodes.Ldstr, Tokens [0]);
				il.Emit (OpCodes.Callvirt, CompilerServices.miFindByName);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, cancel);
			}

			for (int i = 1; i < Tokens.Length -1; i++) {
				il.Emit (OpCodes.Ldstr, Tokens [i]);//load member name
				il.Emit (OpCodes.Call, CompilerServices.miGetMembIinfoWithRefx);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, cancel);
				il.Emit (OpCodes.Call, CompilerServices.miGetValWithRefx);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse, cancel);
			}
		}
		public void emitGetProperty(ILGenerator il, System.Reflection.Emit.Label cancel) {
			il.Emit (OpCodes.Ldstr, Tokens [Tokens.Length -1]);//load member name
			il.Emit (OpCodes.Call, CompilerServices.miGetMembIinfoWithRefx);
			il.Emit (OpCodes.Dup);
			il.Emit (OpCodes.Brfalse, cancel);
			il.Emit (OpCodes.Call, CompilerServices.miGetValWithRefx);
		}
		public void emitSetProperty(ILGenerator il, System.Reflection.Emit.Label cancel) {
			il.Emit (OpCodes.Ldstr, Tokens [Tokens.Length -1]);//load member name
			il.Emit (OpCodes.Call, CompilerServices.miGetMembIinfoWithRefx);
			il.Emit (OpCodes.Dup);
			il.Emit (OpCodes.Brfalse, cancel);
			il.Emit (OpCodes.Call, CompilerServices.miSetValWithRefx);
		}
	}
}

