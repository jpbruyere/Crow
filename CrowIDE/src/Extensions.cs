// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Crow
{
	public static partial class Extensions
	{
		public static string GetIcon(this Widget go){
			return "#Icons." + go.GetType().FullName + ".svg";
		}
		public static List<Widget> GetChildren(this Widget go){
			Type goType = go.GetType();
			if (typeof (Group).IsAssignableFrom (goType))
				return (go as Group).Children;
			if (typeof(Container).IsAssignableFrom (goType))
				return new List<Widget>( new Widget[] { (go as Container).Child });
			if (typeof(TemplatedContainer).IsAssignableFrom (goType))
				return new List<Widget>( new Widget[] { (go as TemplatedContainer).Content });
			if (typeof(TemplatedGroup).IsAssignableFrom (goType))
				return (go as TemplatedGroup).Items;

			return new List<Widget>();
		}

		public static string TabulatedText (this SyntaxToken st, int tabSize) =>
			st.ToString ().Replace ("\t", new string (' ', tabSize));
		public static string TabulatedText (this SyntaxTrivia st, int tabSize) =>
			st.ToString ().Replace ("\t", new string (' ', tabSize));

		public static ObservableList<object> GetChilNodesOrTokens (this SyntaxNode node) {
			ObservableList<object> tmp = new ObservableList<object> ();

			var childs = node.ChildNodesAndTokens().GetEnumerator();

			while (childs.MoveNext()) {
				var c = childs.Current;
				if (c.IsNode) {
					tmp.Add (c.AsNode ());
					continue;
				}
				SyntaxToken tok = c.AsToken ();
				if (tok.HasLeadingTrivia) {
					foreach (var trivia in tok.LeadingTrivia) 
						tmp.Add (trivia);
				}
				tmp.Add (tok);
				if (tok.HasTrailingTrivia) {
					foreach (var trivia in tok.TrailingTrivia)
						tmp.Add (trivia);
				}
			}

			return tmp;
		}
		//kind is a language extension, not found by crow.
		public static SyntaxKind CSKind (this SyntaxToken tok) => tok.Kind ();
		public static SyntaxKind CSKind (this SyntaxTrivia tok) => tok.Kind ();
	}
}
