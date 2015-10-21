//
//  Interface.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Reflection.Emit;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace go
{
	public static class Interface
	{
		public static int TabSize = 4;
		public static string LineBreak = "\r\n";
		public static bool ReplaceTabsWithSpace = false;
		/// <summary> Allow rendering of interface in development environment </summary>
		public static bool DesignerMode = false;
		public static bool DontResoveGOML = false;
		/// <summary> Threshold to catch borders for sizing </summary>
		public static int BorderThreshold = 5;

		/// <summary>
		/// Graphic objects References use in dynamic delegates for binding
		/// </summary>
		public static List<object> References = new List<object> ();
		public static Queue<int> FreeRefIndices = new Queue<int> ();
		public static List<ListBox> LoadingLists= new List<ListBox>();

		public static void Unreference (Object o)
		{
			int idxt = Interface.References.IndexOf (o);
			if (idxt < 0)
				return;
			References [idxt] = null;
			FreeRefIndices.Enqueue (idxt);
		}

		/// <summary> register target object reference in an array for binding CIL </summary>
		public static int Reference (object o)
		{
			
			int dstIdx = Interface.References.IndexOf (o);

			if (dstIdx < 0) {
				if (FreeRefIndices.Count == 0) {
					dstIdx = Interface.References.Count ();
					Interface.References.Add (o);
				} else {
					dstIdx = FreeRefIndices.Dequeue ();
					Interface.References [dstIdx] = o;
				}
			}
			return dstIdx;
		}

		public static LayoutingQueue LayoutingQueue = new LayoutingQueue ();

		#region Load/Save

		internal static Stack<List<DynAttribute>> GOMLResolutionStack = new Stack<List<DynAttribute>> ();

		internal static List<DynAttribute> GOMLResolver {
			get { return GOMLResolutionStack.Peek (); }
		}
		//internal static List<DynAttribute> Bindings;


		public static void Save<T> (string file, T graphicObject)
		{            
			XmlSerializerNamespaces xn = new XmlSerializerNamespaces ();
			xn.Add ("", "");
			XmlSerializer xs = new XmlSerializer (typeof(T));

			xs = new XmlSerializer (typeof(T));
			using (Stream s = new FileStream (file, FileMode.Create)) {
				xs.Serialize (s, graphicObject, xn);
			}
		}

		public static Stream GetStreamFromPath (string path)
		{
			Stream stream = null;

			if (path.StartsWith ("#")) {
				string resId = path.Substring (1);
				stream = System.Reflection.Assembly.GetEntryAssembly ().GetManifestResourceStream (resId);
				if (stream == null)//try to find ressource in golib assembly				
					stream = System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceStream (resId);
				if (stream == null)
					throw new Exception ("Resource not found: " + path);
			} else {
				if (!File.Exists (path))
					throw new FileNotFoundException ("File not found: ", path);
				stream = new FileStream (path, FileMode.Open, FileAccess.Read);
			}
			return stream;
		}

		/// <summary>
		/// Pre-read first node to set GraphicObject class for loading
		/// and reset stream position to 0
		/// </summary>
		public static Type GetTopContainerOfGOMLStream (Stream stream)
		{
			string root = "Object";
			string ClassName = "";
			stream.Seek (0, SeekOrigin.Begin);
			using (XmlReader reader = XmlReader.Create (stream)) {
				while (reader.Read ()) {
					// first element is the root element
					if (reader.NodeType == XmlNodeType.Element) {
						root = reader.Name;
						ClassName = reader.GetAttribute ("Class");
						if (!string.IsNullOrEmpty (ClassName))
							break;
						if (CurrentGOMLPath.StartsWith ("#"))
							ClassName = System.IO.Path.GetFileNameWithoutExtension (CurrentGOMLPath.Substring (1));
						else
							ClassName = System.IO.Path.GetFileNameWithoutExtension (CurrentGOMLPath);
						break;
					}
				}
			}

			Type t = Type.GetType ("go." + root);

			//t = CreateDynamicType (ClassName, t);

			stream.Seek (0, SeekOrigin.Begin);
			return t;
		}




		static AssemblyBuilder assemblyBuilder;
		static ModuleBuilder moduleBuilder;

		public static void InitDynamicAssembly ()
		{
			AssemblyName an = new AssemblyName ("DynamicGraphicObjects");
			assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly (an, AssemblyBuilderAccess.RunAndSave);
			moduleBuilder = assemblyBuilder.DefineDynamicModule ("MainModule");
		}

		public static void TerminateDynamicAssembly ()
		{
		}

		public static CodeCompileUnit CompileUnit;
		public static CodeTypeDeclaration GOTypeDecl;

		public static CodeTypeDeclaration GenCodeType (string newTypeName, Type baseType)
		{
			CompileUnit = new CodeCompileUnit ();
			CodeNamespace cns = null;

			int idxLastDot = newTypeName.LastIndexOf ('.');
			string typeName = newTypeName;
			if (idxLastDot < 0)
				cns = new CodeNamespace ("go");
			else {
				typeName = newTypeName.Substring (idxLastDot + 1);
				cns = new CodeNamespace (newTypeName.Substring (0, idxLastDot));
			}
			CompileUnit.Namespaces.Add (new CodeNamespace ());
			CompileUnit.Namespaces.Add (cns);
			CompileUnit.Namespaces [0].Imports.Add (new CodeNamespaceImport ("System"));
			CodeTypeDeclaration GOTypeDecl = new CodeTypeDeclaration (typeName);
			GOTypeDecl.IsClass = true;
			GOTypeDecl.IsPartial = true;
			GOTypeDecl.TypeAttributes |= TypeAttributes.Public;
			GOTypeDecl.BaseTypes.Add (baseType.Name);
			cns.Types.Add (GOTypeDecl);
			return GOTypeDecl;
		}

		static void GenNewClassFromGOML (string path)
		{
			string root = null;
			string newClassName = "";

			using (Stream stream = GetStreamFromPath (path)) {
				using (XmlReader reader = XmlReader.Create (stream)) {
					CodeTypeDeclaration GOTypeDecl = null;
					CodeConstructor constructor = null;

					CodeExpression curRef = null;
					Type curType = null;
					Stack<CodeExpression> curRefStack = new Stack<CodeExpression> ();
					Stack<Type> curTypeStack = new Stack<Type> ();

					int arrayIndex = -1;
					int localVarCpt = 0;
					
					while (reader.Read ()) {
						switch (reader.NodeType) {
						case XmlNodeType.Element:							
							if (string.IsNullOrEmpty (root)) {
								//create the new base class
								// first element is the root element
								root = reader.Name;
								newClassName = reader.GetAttribute ("Class");
								if (string.IsNullOrEmpty (newClassName)) {
									if (path.StartsWith ("#"))
										newClassName = System.IO.Path.GetFileNameWithoutExtension (path.Substring (1));
									else
										newClassName = System.IO.Path.GetFileNameWithoutExtension (path);
								}
								curType = Type.GetType ("go." + root);

								GOTypeDecl = GenCodeType (newClassName, curType);
								// Declares a constructor.
								constructor = new CodeConstructor ();
								constructor.Attributes = MemberAttributes.Public;
								GOTypeDecl.Members.Add (constructor);

								curRef = new CodeThisReferenceExpression ();
							} else if (reader.Name == "Template") {
							}else{
								Type childType = Type.GetType ("go." + reader.Name);
								localVarCpt++;
								string localVarName = childType.Name + localVarCpt;
								constructor.Statements.Add (
									new CodeVariableDeclarationStatement (
										childType, 
										localVarName,
										new CodeObjectCreateExpression (childType)
									)
								);
								if (curType == typeof(go.Container) || curType.IsSubclassOf (typeof(go.Container))) {
									constructor.Statements.Add (
										new CodeMethodInvokeExpression (
											curRef, 
											"SetChild",
											new CodeVariableReferenceExpression (localVarName)
										)
									);
								} else if (curType == typeof(go.Group) || curType.IsSubclassOf (typeof(go.Group))) {									
									constructor.Statements.Add (
										new CodeMethodInvokeExpression (curRef, "addChild",
											new CodeVariableReferenceExpression (localVarName)
										)
									);
								}
								curTypeStack.Push (curType);
								curRefStack.Push (curRef);
								curRef = new CodeVariableReferenceExpression (localVarName);
								curType = childType;
							}
							while (reader.MoveToNextAttribute ()) {
								string attName = reader.Name;
								string attValue = reader.Value;

								if (string.IsNullOrEmpty (attValue))
									continue;

								MemberInfo mi = curType.GetMember (attName).FirstOrDefault ();
								if (mi == null) {
									Debug.WriteLine (Interface.CurrentGOMLPath + "=>GOML: Unknown attribute in " + curType.ToString () + " : " + attName);
									continue;
								}
								if (mi.MemberType == MemberTypes.Event) {
									//TODO: handle events
									continue;
								}
								if (mi.MemberType == MemberTypes.Property) {
									PropertyInfo pi = mi as PropertyInfo;

									if (pi.GetSetMethod () == null) {
										Debug.WriteLine (Interface.CurrentGOMLPath + "=>GOML: Read only property in " + curType.ToString () + " : " + attName);
										continue;
									}

									if (attValue.StartsWith("{")) {
										if (Interface.DontResoveGOML)
											continue;
										//binding
										if (!attValue.EndsWith("}"))
											throw new Exception (string.Format("GOML:Malformed binding: {0}", attValue));

										string strBinding = attValue.Substring (1, attValue.Length - 2);

										continue;
									}

									CodeExpression val = null;
									if (pi.PropertyType == typeof(string)) {
										val = new CodePrimitiveExpression (attValue);
									} else if (pi.PropertyType.IsPrimitive) {
										MethodInfo me = pi.PropertyType.GetMethod ("Parse", new Type[] { typeof(string) });
										val = new CodePrimitiveExpression (
											me.Invoke (null, new string[] { attValue }));
									} else if (pi.PropertyType.IsEnum || (pi.PropertyType == typeof(go.Color) && Char.IsLetter (attValue [0]))) {
										val = new CodeFieldReferenceExpression (new CodeTypeReferenceExpression (pi.PropertyType), attValue);
									}else if (pi.PropertyType == typeof(go.Color) || pi.PropertyType == typeof(go.Font)) {
										val = new CodeCastExpression(pi.PropertyType,
											new CodeMethodInvokeExpression (
												new CodeTypeReferenceExpression (pi.PropertyType),
												"Parse", 
												new CodePrimitiveExpression (attValue)
											)
										);
									} else {
										val = new CodeMethodInvokeExpression (new CodeTypeReferenceExpression (pi.PropertyType),
											"Parse", new CodePrimitiveExpression (attValue));

									}
									constructor.Statements.Add (
										new CodeAssignStatement (
											new CodePropertyReferenceExpression (curRef, attName),
											val
										)
									);
								}								
							}
							reader.MoveToElement ();
							if (reader.IsEmptyElement) {
								curType = curTypeStack.Pop ();
								curRef = curRefStack.Pop ();
							}
							break;
						case XmlNodeType.EndElement:
							if (curTypeStack.Count < 1)//GOML last closing tag								
								break;
							curType = curTypeStack.Pop ();
							curRef = curRefStack.Pop ();
							if (curType.IsSubclassOf (typeof(go.Container)))
								arrayIndex = -1;
							break;
						}						
					}
				}
			}
				



			GenerateCSharpCode (CompileUnit, path + ".cs");
		}

		static void GenerateCSharpCode (CodeCompileUnit codeBase, string file)
		{
			CodeDomProvider codeDomProvider = new CSharpCodeProvider ();
			//On définit les options de génération de code
			CodeGeneratorOptions options = new CodeGeneratorOptions ();
			//On demande a ce que le code généré soit dans le même ordre que le code inséré
			options.VerbatimOrder = false;
			//options.BracingStyle = "C";
			//options.BracingStyle = "C";
			options.ElseOnClosing = true;
			options.BlankLinesBetweenMembers = false;

			using (IndentedTextWriter itw = new IndentedTextWriter (new StreamWriter (file, false), "\t")) {
				//On demande la génération proprement dite
				codeDomProvider.GenerateCodeFromCompileUnit (codeBase, itw, options);
				itw.Flush ();
			}
			Console.WriteLine ("C# code generated: " + file);
		}

		public static Type CreateDynamicType (string newTypeName, Type baseType)
		{
			if (moduleBuilder == null)
				InitDynamicAssembly ();
			TypeBuilder tb = moduleBuilder.DefineType (newTypeName
				, TypeAttributes.Public |
			                 TypeAttributes.Class |
			                 TypeAttributes.AutoClass |
			                 TypeAttributes.AnsiClass |
			                 TypeAttributes.BeforeFieldInit |
			                 TypeAttributes.AutoLayout
				, baseType);
			
			ConstructorBuilder constructor = tb.DefineDefaultConstructor (MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

			ConstructorInfo cn = typeof(XmlRootAttribute).GetConstructor (new Type[] { typeof(string) });
			CustomAttributeBuilder cab = new CustomAttributeBuilder (cn, new object[] { baseType.Name });

			tb.SetCustomAttribute (cab);




			Type tmp = tb.CreateType ();
			//assemblyBuilder.Save ("newAssembly.dll",PortableExecutableKinds.ILOnly,ImageFileMachine.I386);
			return tmp;
		}

		public static string CurrentGOMLPath;

		public static GraphicObject Load (string path, object hostClass = null, bool resolveGOML = true)
		{		
//			GenNewClassFromGOML (path);
//			return null;
			CurrentGOMLPath = path;
			using (Stream stream = GetStreamFromPath (path)) {
				return Load(stream, GetTopContainerOfGOMLStream(stream), hostClass, resolveGOML);
			}
			CurrentGOMLPath = "";
		}



		public static GraphicObject Load (Stream stream, Type type, object hostClass = null, bool resolve = true)
		{
			#if DEBUG_LOAD_TIME
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif

			GraphicObject result;


			XmlSerializerNamespaces xn = new XmlSerializerNamespaces ();
			xn.Add ("", "");
			XmlSerializer xs = new XmlSerializer (type);

			result = (GraphicObject)xs.Deserialize (stream);
			//result.DataSource = hostClass;

			#if DEBUG_LOAD_TIME
			loadingTime.Stop ();
			Debug.WriteLine ("GOML Loading ({2}): {0} ticks \t, {1} ms",
				loadingTime.ElapsedTicks,
				loadingTime.ElapsedMilliseconds,
				CurrentGOMLPath);
			#endif

			return result;
		}


		#endregion
	}
}

