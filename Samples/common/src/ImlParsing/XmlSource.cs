// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Crow
{
	public class XmlSource {
		public readonly string Source;
		Token[] tokens;
		SyntaxNode RootNode;


		public Token[] Tokens => tokens;

		public XmlSource (string _source) {
			Source = _source;
			Tokenizer tokenizer = new Tokenizer();
			tokens = tokenizer.Tokenize (Source);

			SyntaxAnalyser syntaxAnalyser = new SyntaxAnalyser (this);
			Stopwatch sw = Stopwatch.StartNew ();
			syntaxAnalyser.Process ();
			sw.Stop();

			/*foreach (Token t in Tokens)
				Console.WriteLine ($"{t,-40} {Source.AsSpan(t.Start, t.Length).ToString()}");
			syntaxAnalyser.Root.Dump();*/
			
			Console.WriteLine ($"Syntax Analysis done in {sw.ElapsedMilliseconds}(ms) {sw.ElapsedTicks}(ticks)");
			foreach (SyntaxException ex in syntaxAnalyser.Exceptions)
				Console.WriteLine ($"{ex}");

			RootNode = syntaxAnalyser.Root;
		}

		public Token FindTokenIncludingPosition (int pos) {
			if (pos == 0 || tokens == null || tokens.Length == 0)
				return default;
			int idx = Array.BinarySearch (tokens, 0, tokens.Length, new  Token () {Start = pos});

			return idx == 0 ? tokens[0] : idx < 0 ? tokens[~idx - 1] : tokens[idx - 1];
		}
		public SyntaxNode FindNodeIncludingPosition (int pos) {
			if (RootNode == null)
				return null;
			if (!RootNode.Contains (pos))
				return null;
			return RootNode.FindNodeIncludingPosition (pos);
		}
		public T FindNodeIncludingPosition<T> (int pos) {
			if (RootNode == null)
				return default;
			if (!RootNode.Contains (pos))
				return default;
			return RootNode.FindNodeIncludingPosition<T> (pos);
		}


	}	
}