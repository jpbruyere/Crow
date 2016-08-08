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
		public MemberReference Source;
		public MemberReference Target;

		public string Expression;

		public string DynMethodId {
			get { return dynMethodId; }
		}
		public Type SourceType {
			get { return Source == null ? null 
					: Source.Instance == null ? null 
					: Source.Instance.GetType();}
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
		public Binding (MemberReference _source, string _expression)
		{
			Source = _source;
			Expression = _expression;
		}
		public Binding (object _source, string _member, string _expression)
		{
			Source = new MemberReference (_source, _source.GetType ().GetMember (_member) [0]);
			Expression = _expression;
		}
		public Binding (object _source, string _sourceMember, object _target, string _targetMember)
		{
			Source = new MemberReference (_source, _source.GetType ().GetMember (_sourceMember) [0]);
			Target = new MemberReference (_target, _target.GetType ().GetMember (_targetMember) [0]);
		}
		public Binding (MemberReference _source, MemberReference _target)
		{
			Source = _source;
			Target = _target;
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
		/// <summary>
		/// resolve target expression
		/// </summary>
		/// <returns><c>true</c>, if target was found, <c>false</c> otherwise.</returns>
		public bool TryFindTarget ()
		{
			if (Target != null)
				return true;

			string memberName = null;

			//if binding exp = '{}' => binding is done on datasource
			if (string.IsNullOrEmpty (Expression)) {
				Object o = (Source.Instance as GraphicObject).DataSource;
				if (o == null)
					return false;
				Target = new MemberReference (o);
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
				object dataSource = (Source.Instance as GraphicObject).DataSource;
				if (dataSource == null) {
					Debug.WriteLine ("\tDataSource is null => " + this.ToString());
					return false;
				}
					
				Target = new MemberReference (dataSource);
				memberName = bindingExp [0];
			} else {
				int ptr = 0;
				ILayoutable tmpTarget = Source.Instance as ILayoutable;
				//if exp start with '/' => Graphic tree parsing start at source
				if (string.IsNullOrEmpty (bindingExp [0]))
					ptr++;
				else if (bindingExp[0] == "."){ //search template root
					do {
						tmpTarget = tmpTarget.Parent;
						if (tmpTarget == null)
							return false;
						if (tmpTarget is Interface)
							throw new Exception ("Not in Templated Control");
					} while (!(tmpTarget is TemplatedControl));
					ptr++;
				}
				while (ptr < bindingExp.Length - 1) {
					if (tmpTarget == null) {
#if DEBUG_BINDING
						Debug.WriteLine ("\tERROR: target not found => " + this.ToString());
#endif
						return false;
					}
					if (bindingExp [ptr] == "..")
						tmpTarget = tmpTarget.LogicalParent;
					else if (bindingExp [ptr] == ".") {
						if (ptr > 0)
							throw new Exception ("Syntax error in binding, './' may only appear in first position");
						tmpTarget = Source.Instance as ILayoutable;
					} else
						tmpTarget = (tmpTarget as GraphicObject).FindByName (bindingExp [ptr]);
					ptr++;
				}

				if (tmpTarget == null) {
#if DEBUG_BINDING
					Debug.WriteLine ("\tERROR: Binding Target not found => " + this.ToString());
#endif
					return false;
				}

				Target = new MemberReference (tmpTarget);

				string [] bindTrg = bindingExp [ptr].Split ('.');

				if (bindTrg.Length == 1)
					memberName = bindTrg [0];
				else if (bindTrg.Length == 2) {
					tmpTarget = (tmpTarget as GraphicObject).FindByName (bindTrg [0]);
					memberName = bindTrg [1];
				} else
					throw new Exception ("Syntax error in binding, expected 'go dot member'");

			}

			if (Target.TryFindMember (memberName)) {
				if (TwoWayBinding)
					Interface.RegisterBinding (new Binding (Target, Source));				
			}
			#if DEBUG_BINDING
			else
				Debug.WriteLine ("Property less binding: " + Target + expression);
			#endif

			return true;
		}
		public void Reset ()
		{
			Target = null;
			dynMethodId = "";
			Resolved = false;
		}
		public override string ToString ()
		{
			return string.Format ("[Binding: {0}.{1} <= {2}]", Source.Instance, Source.Member.Name, Expression);
		}
	}

}

