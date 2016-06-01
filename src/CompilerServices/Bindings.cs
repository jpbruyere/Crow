//
//  Bindings.cs
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
using System.Diagnostics;

namespace Crow
{
	/// <summary>
	/// Binding Class
	/// </summary>
	public class Binding
	{
		static int bindingCpt;
		string dynMethodId = "";
		bool resolved;

		public bool TwoWayBinding;
		public MemberReference Target;
		public MemberReference Source;

		public string Expression;

		public string DynMethodId {
			get { return dynMethodId; }
		}

		public bool Resolved {
			get { return resolved; }
			set {
				if (value == resolved)
					return;
#if DEBUG_BINDING
				if (value == true)
					Debug.WriteLine ("\tOk => " + this.ToString());
				else
					Debug.WriteLine ("\tresolved state reseted => " + this.ToString());
#endif
				resolved = value;
			}
		}

		#region CTOR
		public Binding () { }
		public Binding (MemberReference _target, string _expression)
		{
			Target = _target;
			Expression = _expression;
		}
		public Binding (object _target, string _member, string _expression)
		{
			Target = new MemberReference (_target, _target.GetType ().GetMember (_member) [0]);
			Expression = _expression;
		}
		public Binding (object _target, string _targetMember, object _source, string _sourceMember)
		{
			Target = new MemberReference (_target, _target.GetType ().GetMember (_targetMember) [0]);
			Source = new MemberReference (_source, _source.GetType ().GetMember (_sourceMember) [0]);
		}
		public Binding (MemberReference _target, MemberReference _source)
		{
			Target = _target;
			Source = _source;
		}
		#endregion

		public string CreateNewDynMethodId ()
		{
			if (!string.IsNullOrEmpty (dynMethodId))
				return dynMethodId;
			dynMethodId = "dynHandle_" + bindingCpt;
			bindingCpt++;
			return dynMethodId;
		}

		public bool FindSource ()
		{
			if (Source != null)
				return true;

			string member = null;

			//if binding exp = '{}' => binding is done on datasource
			if (string.IsNullOrEmpty (Expression)) {
				Object o = (Target.Instance as GraphicObject).DataSource;
				if (o == null)
					return false;
				Source = new MemberReference (o);
				return true;
			}

			string expression = Expression;

			if (expression.StartsWith ("²")) {
				expression = expression.Substring (1);
				TwoWayBinding = true;
			}

			string [] bindingExp = expression.Split ('/');

			if (bindingExp.Length == 1) {
				//datasource binding
				Source = new MemberReference ((Target.Instance as GraphicObject).DataSource);
				member = bindingExp [0];
			} else {
				int ptr = 0;
				ILayoutable tmp = Target.Instance as ILayoutable;
				if (string.IsNullOrEmpty (bindingExp [0])) {
					//if exp start with '/' => Graphic tree parsing start at top container
					tmp = Interface.CurrentInterface as ILayoutable;
					ptr++;
				}
				while (ptr < bindingExp.Length - 1) {
					if (tmp == null) {
#if DEBUG_BINDING
						Debug.WriteLine ("\tERROR: target not found => " + this.ToString());
#endif
						return false;
					}
					if (bindingExp [ptr] == "..")
						tmp = tmp.LogicalParent;
					else if (bindingExp [ptr] == ".") {
						if (ptr > 0)
							throw new Exception ("Syntax error in binding, './' may only appear in first position");
						tmp = Target.Instance as ILayoutable;
					} else
						tmp = (tmp as GraphicObject).FindByName (bindingExp [ptr]);
					ptr++;
				}

				if (tmp == null) {
#if DEBUG_BINDING
					Debug.WriteLine ("\tERROR: target not found => " + this.ToString());
#endif
					return false;
				}

				string [] bindTrg = bindingExp [ptr].Split ('.');

				if (bindTrg.Length == 1)
					member = bindTrg [0];
				else if (bindTrg.Length == 2) {
					tmp = (tmp as GraphicObject).FindByName (bindTrg [0]);
					member = bindTrg [1];
				} else
					throw new Exception ("Syntax error in binding, expected 'go dot member'");

				Source = new MemberReference (tmp);
			}
			if (Source == null) {
				Debug.WriteLine ("Binding Source is null: " + Expression);
				return false;
			}

			if (Source.TryFindMember (member)) {
				if (TwoWayBinding) {
					IBindable source = Source.Instance as IBindable;
					if (source == null)
						throw new Exception (Target.Instance + " does not implement IBindable for 2 way bindings");
					source.Bindings.Add (new Binding (Source, Target));
				}
				return true;
			}

			Debug.WriteLine ("Binding member not found: " + member);
			Source = null;
			return false;
		}
		public void Reset ()
		{
			Source = null;
			dynMethodId = "";
			Resolved = false;
		}
		public override string ToString ()
		{
			return string.Format ("[Binding: {0}.{1} <= {2}]", Target.Instance, Target.Member.Name, Expression);
		}
	}

}

