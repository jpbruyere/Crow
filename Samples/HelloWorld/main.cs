using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Crow;
using Crow.IML;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

/*namespace RoslynVarRewrite
{
	public class VarRewriter : CSharpSyntaxRewriter
	{
		private readonly SemanticModel model;

		public VarRewriter (SemanticModel model)
		{
			this.model = model;
		}

		public override SyntaxNode VisitLocalDeclarationStatement (LocalDeclarationStatementSyntax node)
		{
			var symbolInfo = model.GetSymbolInfo (node.Declaration.Type);
			var typeSymbol = symbolInfo.Symbol;
			var type = typeSymbol.ToDisplayString (
			  SymbolDisplayFormat.MinimallyQualifiedFormat);

			var declaration = SyntaxFactory
				.LocalDeclarationStatement (
					SyntaxFactory
						.VariableDeclaration (SyntaxFactory.IdentifierName (
						  SyntaxFactory.Identifier (type)))
							.WithVariables (node.Declaration.Variables)
							.NormalizeWhitespace ()
					)
					.WithTriviaFrom (node);
			return declaration;
		}
	}

	class Program
	{
		static void Main (string [] args)
		{
			var tree = CSharpSyntaxTree.ParseText (@"
using System;
using System.Collections.Generic;
class Program {
  static void Main(string[] args) {
    var x = 5;
    var s = ""Test string"";
    var l = new List<string>();
    var scores = new byte[8][]; // Test comment
    var names = new string[3] {""Diego"", ""Dani"", ""Seba""};
  }
}");

			// Get the assembly file, the compilation and the semantic model
			var Mscorlib = MetadataReference.CreateFromFile (typeof (object).Assembly.Location);
			var compilation = CSharpCompilation.Create ("RoslynVarRewrite",
			  syntaxTrees: new [] { tree },
			  references: new [] { Mscorlib });
			var model = compilation.GetSemanticModel (tree);

			var varRewriter = new VarRewriter (model);
			var result = varRewriter.Visit (tree.GetRoot ());
			Console.WriteLine (result.ToFullString ());
		}
	}
}*/
namespace HelloWorld
{
	class Program : Interface
	{
		//static void Main ()
		//{
		//	using (Stream s = new FileStream("test.txt", FileMode.Create)) {
		//		using (StreamWriter sw = new StreamWriter (s)) {
		//			foreach (SyntaxKind v in Enum.GetValues(typeof(SyntaxKind))) {
		//				//sw.WriteLine ($"{v,-50} = {(int)v:X8}");
		//				//sw.WriteLine ($"{v,-50} = {Convert.ToString ((int)v, 2).PadLeft (16, '0')}");
		//				sw.WriteLine ($"case SyntaxKind.{v}:");
		//				sw.WriteLine ($"\ttf = editor.formatting [\"default\"];");
		//				sw.WriteLine ($"\tbreak;");
		//			}
		//		}
		//	}
		//}
		//		public class CustomWalker : CSharpSyntaxWalker
		//		{
		//			static int Tabs = 0;
		//			bool cancel;
		//			int visibleLines, firstLine, currentLine, printedLines;

		//			public CustomWalker (int firstLine = 0, int visibleLines = 1) : base (SyntaxWalkerDepth.StructuredTrivia)
		//			{
		//				this.visibleLines = visibleLines;
		//				this.firstLine = firstLine;
		//				currentLine = 0;
		//				printedLines = (firstLine == 0) ? 0 : - 1;//<0 until firstLine is reached

		//			}
		//			public override void DefaultVisit (SyntaxNode node)
		//			{
		//				if (!cancel)
		//					base.DefaultVisit (node);
		//			}
		//			public override void Visit (SyntaxNode node)
		//			{
		//				if (cancel)
		//					return;

		//				FileLinePositionSpan ls = node.SyntaxTree.GetLineSpan(node.FullSpan);
		//				currentLine = ls.StartLinePosition.Line;
		//				if (ls.EndLinePosition.Line >= firstLine)
		//					base.Visit (node);
		//			}
		//			public override void VisitToken (SyntaxToken token)
		//			{
		//				if (cancel)
		//					return;

		//				if ((int)Depth >= 2) {
		//					Console.ForegroundColor = ConsoleColor.Blue;
		//					VisitLeadingTrivia (token);
		//					if (cancel)
		//						return;
		//					if (printedLines >= 0) {
		//						Console.ForegroundColor = ConsoleColor.White;
		//						Console.Write ($"{(token.RawKind)}{token.ToString ()}");
		//					}
		//					Console.ForegroundColor = ConsoleColor.Green;
		//					VisitTrailingTrivia (token);
		//				}
		//			}

		//			bool print => printedLines >= 0 && printedLines < visibleLines;

		//			//public override void VisitLeadingTrivia (SyntaxToken token)
		//			//{
		//			//	leadingTrivia = true;
		//			//	base.VisitLeadingTrivia (token);
		//			//}
		//			//public override void VisitTrailingTrivia (SyntaxToken token)
		//			//{
		//			//	if (cancel)
		//			//		return;
		//			//	leadingTrivia = false;
		//			//	base.VisitTrailingTrivia (token);
		//			//}
		//			//bool leadingTrivia;
		//			public override void VisitTrivia (SyntaxTrivia trivia)
		//			{

		//				if (cancel)
		//					return;

		//				base.VisitTrivia (trivia);

		//				if (print) {
		//					if (trivia.IsKind (SyntaxKind.EndOfLineTrivia)) {
		//						Console.Write ($"(eof)");
		//						printedLines++;
		//						cancel = printedLines == visibleLines;
		//					}
		//					Console.Write ($"{trivia.ToString ()}");
		//				}

		//				if (trivia.IsKind (SyntaxKind.EndOfLineTrivia)) {
		//					currentLine++;
		//					if (printedLines < 0) {
		//						if (currentLine == firstLine)
		//							printedLines = 0;
		//					}
		//				}

		//			}
		//		}

		//		static void Main (string [] args)
		//		{
		//			var tree = CSharpSyntaxTree.ParseText (@"// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
		////
		//// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

		//using System;
		//using System.IO;

		//public class MyClass
		//{
		//	//this is a leading trivia
		//	public void MyMethod()
		//	{
		//		int a = 10;
		//#if DEBUG
		//		int b = a + 10;
		//#endif
		//	}
		///* this is a block comment
		//*/
		//	public void MyMethod(int n)
		//	{
		//	}
		//}");

		//	var walker = new CustomWalker (4,5);
		//	walker.Visit (tree.GetRoot ());
		//}

		//Command CMDQuit;
		static void Main (string [] args)
		{
			Environment.SetEnvironmentVariable ("MSBUILD_EXE_PATH", Path.Combine (msbuildRoot, "MSBuild.dll"));
			Environment.SetEnvironmentVariable ("MSBUILD_NUGET_PATH", "/home/jp/.nuget/packages");
			Environment.SetEnvironmentVariable ("COREHOST_TRACE", "1");

			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += msbuildAssembliesResolve;

			using (Program vke = new Program ()) {
				vke.Run ();
			}
		}

		//const string msbuildRoot = "/usr/lib/mono/msbuild/Current/bin/";
		const string msbuildRoot = "/usr/share/dotnet/sdk/3.1.101/";
		const string toolsVersion = "Current";

		static Assembly msbuildAssembliesResolve (object sender, ResolveEventArgs args)
		{
			string assemblyPath = Path.Combine (msbuildRoot, new AssemblyName (args.Name).Name + ".dll");
			/*Console.BackgroundColor = ConsoleColor.DarkBlue;
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine ($"Probing: {assemblyPath}");
			Console.ResetColor ();*/
			if (!File.Exists (assemblyPath)) return null;
			Assembly assembly = Assembly.LoadFrom (assemblyPath);
			return assembly;
		}

		//protected override void Startup ()
		//{
		//	CMDQuit = new Command (new Action (() => running = false)) { Caption = "Quit", Icon = new SvgPicture ("#Crow.Icons.exit-symbol.svg") };

		//	Widget w = Load ("#HelloWorld.helloworld.crow");
		//	w.DataSource = this;
		//}

		//public string source = @"
		//  using System;

		//  namespace RoslynCompileSample
		//  {
		//      public class Writer
		//      {
		//          public void Write(string message)
		//          {
		//              Console.WriteLine(message);
		//          }
		//      }
		//  }";
		//public string Source {
		//	get => source;
		//	set {
		//		if (source == value)
		//			return;
		//		source = value;
		//		NotifyValueChanged ("Source", source);
		//	}
		//}



		public override bool OnKeyDown (Key key)
		{
			switch (key) {
			//case Key.F2:
			//compile ();
			//break;
			case Key.F3:
				Task t = new Task (testWS);
				t.Start ();
				break;
			default:
				return base.OnKeyDown (key);
			}
			return true;
		}

		//void printChild (IndentedTextWriter stream, CSharpSyntaxNode n)
		//{
		//	stream.WriteLine ($"{n.Kind()} {n.ToString()}");
		//	stream.Indent++;
		//	foreach (CSharpSyntaxNode item in n.ChildNodes()) {
		//		printChild (stream, item);
		//	}
		//	stream.Indent--;
		//}
		string slnPath = "/mnt/devel/glfw-sharp/glfw-sharp.sln";
		async void testWS ()
		{
			if (!File.Exists (slnPath)) {
				Console.WriteLine ($"File not found: {slnPath}");
				return;
			}
			Dictionary<String, String> globalProperties = new Dictionary<String, String> ();

			Console.WriteLine ("starting Compilation.");

			//globalProperties ["NuGetPackageRoot"] = "/home/jp/.nuget/packages";
			//globalProperties ["RoslynTargetsPath"] = Path.Combine (msbuildRoot, "Roslyn/");
			//globalProperties ["MSBuildSDKsPath"] = Path.Combine (msbuildRoot, "Sdks/");
			//globalProperties ["AdditionalLibPaths"] = msbuildRoot;
			//globalProperties ["AssemblySearchPaths"] = msbuildRoot;
			foreach (var item in MSBuildMefHostServices.DefaultAssemblies) {
				Console.WriteLine ($"{item.FullName}");
			}

			var host = MefHostServices.Create (MSBuildMefHostServices.DefaultAssemblies);

			using (var workspace = MSBuildWorkspace.Create (globalProperties)) {
				//workspace. Properties["BuildingInsideVisualStudio"] = "false";
				workspace.WorkspaceFailed += (sender, e) => Console.WriteLine ($"Workspace error: {e.Diagnostic}");
				Console.WriteLine ($"Opening Solution {slnPath}");
				var solution = await workspace.OpenSolutionAsync (slnPath, new ProgressLog ());
				Console.WriteLine ($"Proj Count:{solution.Projects.Count ()}");
				foreach (Project project in solution.Projects) {
					Console.WriteLine ($"Compiling project:{project.FilePath}");

					Compilation compilation = await project.GetCompilationAsync ();
					Document doc = project.Documents.First ();

					CompletionService.GetService (doc);
					using (var ms = new MemoryStream ()) {
						EmitResult result = compilation.Emit (ms);

						if (!result.Success) {
							IEnumerable<Diagnostic> failures = result.Diagnostics.Where (diagnostic =>
								 diagnostic.IsWarningAsError ||
								 diagnostic.Severity == DiagnosticSeverity.Error);

							foreach (Diagnostic diagnostic in failures) {
								Console.Error.WriteLine ("{0}: {1}", diagnostic.Id, diagnostic.GetMessage ());
							}
						} else {
							Console.WriteLine ("Compilation ok.");
						}
					}

				}

				//workspace.TryApplyChanges()
			}
			Console.WriteLine ($"end.");
		}
	}
	class ProgressLog : IProgress<ProjectLoadProgress>
	{
		public void Report (ProjectLoadProgress value)
		{
			Console.WriteLine ($"{value.ElapsedTime} {value.Operation} {value.TargetFramework}");
		}
	}

}
//void compile ()
//{
//	string assemblyName = Path.GetRandomFileName ();
//	MetadataReference [] references = new MetadataReference []
//	{
//		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
//		MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
//	};
//	SourceText txt = SourceText.From (source);

//	SyntaxTree syntaxTree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText (txt);
//	//CSharpCompilationOptions options = new CSharpCompilationOptions ();
//	CSharpCompilation compilation = CSharpCompilation.Create(
//		assemblyName,
//		syntaxTrees: new [] { syntaxTree },
//		references: references,
//		options: new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary));


//	CSharpSyntaxNode n = (CSharpSyntaxNode)syntaxTree.GetRoot ();

//	using (StringWriter sw = new StringWriter ()) {
//		using (IndentedTextWriter itw = new IndentedTextWriter (sw))
//			printChild (itw, n);

//		Console.Write (sw);
//	}

//	using (var ms = new MemoryStream ()) {
//		EmitResult result = compilation.Emit (ms);

//		if (!result.Success) {
//			IEnumerable<Diagnostic> failures = result.Diagnostics.Where (diagnostic =>
//				 diagnostic.IsWarningAsError ||
//				 diagnostic.Severity == DiagnosticSeverity.Error);

//			foreach (Diagnostic diagnostic in failures) {
//				Console.Error.WriteLine ("{0}: {1}", diagnostic.Id, diagnostic.GetMessage ());
//			}
//		} else {
//			ms.Seek (0, SeekOrigin.Begin);
//			Assembly assembly = Assembly.Load (ms.ToArray ());
//			Type type = assembly.GetType ("RoslynCompileSample.Writer");
//			object obj = Activator.CreateInstance (type);
//			type.InvokeMember ("Write", BindingFlags.Default | BindingFlags.InvokeMethod,
//				null,
//				obj,
//				new object [] { "Hello World" });
//		}
//	}



//}
//	}
//}
