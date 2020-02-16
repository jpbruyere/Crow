// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Text.RegularExpressions;
using Crow.Cairo;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Crow.Coding
{
	public class SyntaxNodePrinter : CSharpSyntaxWalker
	{
		static int tabSize = 4;
		bool cancel;
		int firstLine, visibleLines, currentLine, printedLines, totalLines;
		Context ctx;
		RoslynEditor editor;
		FontExtents fe;
		double x, y;
		Rectangle bounds;
		public readonly int [] printedLinesNumbers;

		public SyntaxNodePrinter (RoslynEditor editor, Context ctx, int totalLines, int firstLine = 0, int visibleLines = 1) : base (SyntaxWalkerDepth.StructuredTrivia)
		{
			this.editor = editor;
			this.ctx = ctx;
			this.totalLines = totalLines;
			this.firstLine = firstLine;
			this.visibleLines = visibleLines;

			printedLinesNumbers = new int [visibleLines];

			bounds = editor.ClientRectangle;

			fe = ctx.FontExtents;
			x = bounds.Left;
			y = bounds.Top;

			currentLine = 0;
			printedLines = (firstLine == 0) ? 0 : -1;//<0 until firstLine is reached
			if (printedLines == 0)
				checkPrintMargin ();

		}
		public override void DefaultVisit (SyntaxNode node)
		{
			if (!cancel)
				base.DefaultVisit (node);
		}

		public override void Visit (SyntaxNode node)
		{
			if (cancel)
				return;

			FileLinePositionSpan ls = node.SyntaxTree.GetLineSpan (node.FullSpan);

			currentLine = ls.StartLinePosition.Line;
			checkFirstLine ();
					
			if (ls.EndLinePosition.Line >= firstLine || node.IsStructuredTrivia)
				base.Visit (node);

			currentLine = ls.EndLinePosition.Line;
		}
		public override void VisitToken (SyntaxToken token)
		{
			if (cancel)
				return;

			if ((int)Depth >= 2) {
				VisitLeadingTrivia (token);

				if (cancel)
					return;

				if (token.IsKind (SyntaxKind.XmlTextLiteralNewLineToken)) {
					if (printedLines < 0) {
						currentLine++;
						if (currentLine == firstLine) {
							printedLines = 0;
							checkPrintMargin ();
						}
					} else {
						storeAndIncrementPrintedLine ();
						if (cancel)
							return;
						currentLine++;
						checkPrintMargin ();
					}
				}else if (printedLines >= 0) 
					printToken (token.ToString(), token.Kind());

				VisitTrailingTrivia (token);
			}
		}

		void checkFirstLine ()
		{
			if (printedLines < 0 && currentLine == firstLine) {
				printedLines = 0;
				checkPrintMargin ();
			}
		}

		void printToken (string lstr, SyntaxKind kind, bool trivia = false)
		{
			TextFormatting tf = editor.formatting ["default"];

			if (SyntaxFacts.IsTypeSyntax (kind))
				tf = editor.formatting ["TypeSyntax"];
			else if (SyntaxFacts.IsPreprocessorDirective (kind))
				tf = editor.formatting ["PreprocessorDirective"];
			else if (SyntaxFacts.IsDocumentationCommentTrivia (kind))
				tf = editor.formatting ["DocumentationCommentTrivia"];
			else if (kind == SyntaxKind.DisabledTextTrivia)
				tf = editor.formatting ["DisabledTextTrivia"];
			else if (SyntaxFacts.IsTrivia (kind))
				tf = editor.formatting ["Trivia"];
			else if (SyntaxFacts.IsPunctuation (kind))
				tf = editor.formatting ["Punctuation"];
			else if (SyntaxFacts.IsName (kind))
				tf = editor.formatting ["Name"];
			else if (SyntaxFacts.IsLiteralExpression (kind))
				tf = editor.formatting ["LiteralExpression"];
			else if (SyntaxFacts.IsPredefinedType (kind))
				tf = editor.formatting ["PredefinedType"];
			else if (SyntaxFacts.IsPrimaryFunction (kind))
				tf = editor.formatting ["PrimaryFunction"];
			else if (SyntaxFacts.IsContextualKeyword (kind))
				tf = editor.formatting ["ContextualKeyword"];
			else if (SyntaxFacts.IsKeywordKind (kind))
				tf = editor.formatting ["keyword"];
			else if (SyntaxFacts.IsGlobalMemberDeclaration (kind))
				tf = editor.formatting ["GlobalMemberDeclaration"];
			else if (SyntaxFacts.IsInstanceExpression (kind))
				tf = editor.formatting ["InstanceExpression"];
			else if (SyntaxFacts.IsNamespaceMemberDeclaration (kind))
				tf = editor.formatting ["NamespaceMemberDeclaration"];
			else if (SyntaxFacts.IsTypeDeclaration (kind))
				tf = editor.formatting ["TypeDeclaration"];


			Color selbg = editor.SelectionBackground;
			Color selfg = editor.SelectionForeground;
			FontSlant fts = FontSlant.Normal;
			FontWeight ftw = FontWeight.Normal;


			if (tf.Bold)
				ftw = FontWeight.Bold;
			if (tf.Italic)
				fts = FontSlant.Italic;
			

			ctx.SelectFontFace (editor.Font.Name, fts, ftw);
			ctx.SetSourceColor (tf.Foreground);

			//ctx.SetSourceColor (Color.Black);

			ctx.MoveTo (x, y + fe.Ascent);
			ctx.ShowText (lstr);
			x += fe.MaxXAdvance * lstr.Length;

		}
		void checkPrintMargin ()
		{
			if (!editor.PrintLineNumbers || x > bounds.Left)
				return;
			Rectangle mgR = new Rectangle ((int)x, (int)y, editor.leftMargin - RoslynEditor.leftMarginGap, (int)Math.Ceiling ((fe.Ascent + fe.Descent)));
			/*if (cl.exception != null) {
				mgBg = Color.Red;
				if (CurrentLine == lineIndex)
					mgFg = Color.White;
				else
					mgFg = Color.LightGrey;
			}else */
			Color mgFg = Color.Jet;
			Color mgBg = Color.Grey;
			if (editor.CurrentLine == currentLine && editor.HasFocus) {
				mgFg = Color.Black;
				mgBg = Color.DarkGrey;
			}
			string strLN = (currentLine + 1).ToString ();
			ctx.SetSourceColor (mgBg);
			ctx.Rectangle (mgR);
			ctx.Fill ();
			ctx.SetSourceColor (mgFg);

			ctx.MoveTo (bounds.X + (int)(ctx.TextExtents (totalLines.ToString ()).Width - ctx.TextExtents (strLN).Width), y + fe.Ascent);
			ctx.ShowText (strLN);
			ctx.Fill ();
			x += editor.leftMargin;
		}

		bool print => printedLines >= 0 && printedLines < visibleLines;
		
		public override void VisitTrivia (SyntaxTrivia trivia)
		{
			if (cancel)
				return;

			//if (trivia.IsKind (SyntaxKind.DocumentationCommentExteriorTrivia))
				//System.Diagnostics.Debugger.Break ();

			base.VisitTrivia (trivia);

			if (trivia.HasStructure)
				return;

			if (trivia.IsKind (SyntaxKind.DisabledTextTrivia) || trivia.IsKind (SyntaxKind.MultiLineCommentTrivia)) {
				string [] lines = Regex.Split (trivia.TabulatedText (tabSize), @"\r\n|\r|\n|\\\n");
				for (int i = 0; i < lines.Length-1; i++) {
					/*if (string.IsNullOrEmpty (lines [i]))
						continue;*/
					if (printedLines < 0) {
						currentLine++;
						if (currentLine == firstLine) {
							printedLines = 0;
							checkPrintMargin ();
						}
					} else {
						printToken (lines [i], trivia.Kind (), true);
						storeAndIncrementPrintedLine ();
						if (cancel)
							return;
						currentLine++;
						checkPrintMargin ();
					}
				}
				if (printedLines >= 0)
					printToken (lines [lines.Length - 1], trivia.Kind (), true);
			}else if (print) {
				if (trivia.IsKind (SyntaxKind.EndOfLineTrivia))
					storeAndIncrementPrintedLine ();
				else if (trivia.IsKind (SyntaxKind.WhitespaceTrivia))
					x += trivia.TabulatedText (tabSize).Length * fe.MaxXAdvance;
				else 
					printToken (trivia.TabulatedText (tabSize), trivia.Kind (), true);
			}

			if (trivia.IsKind (SyntaxKind.EndOfLineTrivia)) {
				currentLine++;
				if (printedLines < 0) {
					if (currentLine == firstLine) {
						printedLines = 0;
						checkPrintMargin ();
					}
				}else
					checkPrintMargin ();
			}
		}


		void storeAndIncrementPrintedLine ()
		{
			printedLinesNumbers [printedLines] = currentLine;
			printedLines++;
			y += (fe.Ascent + fe.Descent);
			x = bounds.Left;
			cancel = printedLines == visibleLines;

		}
	}
}
