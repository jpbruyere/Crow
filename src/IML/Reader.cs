//
//  IMLReader.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
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
using System.Xml;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;

namespace Crow.IML
{
	public class Reader : XmlTextReader
	{		
		static List<Type> CrowTypes = new List<Type> ();

		public enum SubNodeType{
			None,
			Child,
			Children,
			Template,
			Content,
			Items,
			ItemTemplate
		}

			//public static Node Parse(string str){
			//	string[] tmp = str.Trim ().Split ('.');
			//	switch (tmp.Length) {
			//	case 1:
			//		return new Node ((SubNodeType)int.Parse (tmp [0]));
			//	case 2:
			//		return new Node ((SubNodeType)int.Parse (tmp [0]), int.Parse (tmp[1]));
			//	case 0:
			//	default:
			//		return new Node ();
			//	}
			//}
			//public static string AddressToString(Node[] address){
			//	string tmp = "";
			//	foreach (Node n in address) {
			//		tmp += n.ToString () + ";";
			//	}
			//	return string.IsNullOrEmpty(tmp) ? tmp : tmp.Substring (0, tmp.Length - 1);
			//}
			//public static Node[] AddressFromString(string address) {
			//	List<Node> nodes = new List<Node> ();
			//	string[] tmp = address.Split (';');
			//	for (int i = 0; i < tmp.Length; i++)
			//		nodes.Add (Node.Parse (tmp [i]));
			//	return nodes.ToArray();
			//}


		//		public class PropertyBinding {
		//			public string OriginePropertyName = "";
		//			public MemberAddress Destination;
		//		}

		public Type RootType = null;
		public Context context;

		InstanciatorInvoker loader = null;
		DynamicMethod dm = null;

		ILGenerator il { get { return context.il; }}

		/// <summary>
		/// Finalize instatiator MSIL and return LoaderInvoker delegate
		/// </summary>
		public InstanciatorInvoker GetLoader(){
			if (loader != null)
				return loader;

			il.Emit(OpCodes.Ret);
			loader = (InstanciatorInvoker)dm.CreateDelegate (typeof(InstanciatorInvoker));
			return loader;
		}

		protected int curDepth {
			get { return context.nodesStack.Count - 1;}
		}
		protected Node curNode {
			get { return context.nodesStack.Peek(); }
		}
		//protected Stack<int> curTemplateDepth = new Stack<int>(new int[] {0});	//current template root depth

		#region CTOR
		public Reader (string path)
			: this(Interface.GetStreamFromPath (path)){
		}
		public Reader (Stream stream)
			: base(stream)
		{
			createInstantiator ();
		}
		/// <summary>
		/// Used to parse xmlFrament with same code generator linked
		/// If ilGen=null, a new Code Generator will be created.
		/// </summary>
		public Reader (Context ctx, string xmlFragment)
			: base(xmlFragment, XmlNodeType.Element,null){

			context = ctx;
//
//			if (IMLCtx != null)
//				return;
//
//			createInstantiator();
		}
		#endregion

		void createInstantiator(){
			context = new Context();
			RootType = findRootType();
			InitEmitter();
			emitLoader(RootType);
			Read();//close tag
		}

		void createBindingDelegates(){
//			foreach (Dictionary<string, MemberAddress> pb in IMLCtx.PropertyBindings) {
//				
//			}
		}
		/// <summary>
		/// Inits il generator, RootType must have been read first
		/// </summary>
		void InitEmitter(){

			dm = new DynamicMethod("dyn_instantiator",
				MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
				CallingConventions.Standard,
				typeof(void),new Type[] {typeof(object), typeof(Interface)}, RootType, true);

			context.il = dm.GetILGenerator(256);
			il.DeclareLocal(typeof(GraphicObject));
			il.Emit(OpCodes.Nop);
			//set local GraphicObject to root object passed as 1st argument
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Stloc_0);
			CompilerServices.emitSetCurInterface (il);
		}
		void emitLoader(Type crowType){
			Node expectedNode = new Node (crowType);

			string tmpXml = ReadOuterXml ();

			il.Emit (OpCodes.Ldloc_0);//save current go onto the stack if child has to be added

			#region Template and ItemTemplates loading
			if (expectedNode.IsTemplate) {
				//if its a template, first read template elements
				using (Reader reader = new Reader (context, tmpXml)) {
					List<string []> itemTemplateIds = new List<string []> ();
					bool inlineTemplate = false;

					string templatePath = reader.GetAttribute ("Template");

					reader.Read ();
					int depth = reader.Depth + 1;
					while (reader.Read ()) {
						if (!reader.IsStartElement () || reader.Depth > depth)
							continue;
						if (reader.Name == "Template") {
							inlineTemplate = true;
							reader.Read ();
							context.nodesStack.Push (expectedNode);
							readChildren (reader);
							context.nodesStack.Pop();
						} else if (reader.Name == "ItemTemplate") {
							string dataType = "default", datas = "", path = "";
							while (reader.MoveToNextAttribute ()) {
								if (reader.Name == "DataType")
									dataType = reader.Value;
								else if (reader.Name == "Data")
									datas = reader.Value;
								else if (reader.Name == "Path")
									path = reader.Value;
							}
							reader.MoveToElement ();

							string itemTmpID;

							if (string.IsNullOrEmpty (path)) {
								using (Reader iTmp = new Reader (null, reader.ReadInnerXml ())) {
									itemTmpID = Guid.NewGuid ().ToString ();
									Interface.Instantiators [itemTmpID] =
										new ItemTemplate (iTmp.RootType, iTmp.GetLoader (), dataType, datas);
								}
							}else{
								if (!reader.IsEmptyElement)
									throw new Exception ("ItemTemplate with Path attribute may not include sub nodes");
								itemTmpID = path;
								Interface.Instantiators [itemTmpID] =
									new ItemTemplate (itemTmpID, dataType, datas);
							}
							itemTemplateIds.Add (new string[] { dataType, itemTmpID, datas });
						}
					}

					if (!inlineTemplate) {//load from path or default template
						reader.il.Emit (OpCodes.Ldloc_0);//Load  current templatedControl ref
						if (string.IsNullOrEmpty (templatePath)) {
							reader.il.Emit (OpCodes.Ldnull);//default template loading
						}else{
							reader.il.Emit (OpCodes.Ldarg_1);//load currentInterface
							reader.il.Emit (OpCodes.Ldstr, templatePath); //Load template path string
							reader.il.Emit (OpCodes.Callvirt,//call Interface.Load(path)
								CompilerServices.miIFaceLoad);
						}
						reader.il.Emit (OpCodes.Callvirt, CompilerServices.miLoadTmp);//load template
					}
					//copy item templates (review this)
					foreach (string[] iTempId in itemTemplateIds) {
						reader.il.Emit (OpCodes.Ldloc_0);//load TempControl ref
						reader.il.Emit (OpCodes.Ldfld, CompilerServices.fldItemTemplates);//load ItemTemplates dic field
						reader.il.Emit (OpCodes.Ldstr, iTempId[0]);//load key
						reader.il.Emit (OpCodes.Ldstr, iTempId[1]);//load value
						reader.il.Emit (OpCodes.Callvirt, CompilerServices.miGetITemp);
						reader.il.Emit (OpCodes.Callvirt, CompilerServices.miAddITemp);

						if (!string.IsNullOrEmpty (iTempId [2])) {
							//expand delegate creation
							reader.il.Emit (OpCodes.Ldloc_0);//load TempControl ref
							reader.il.Emit (OpCodes.Ldfld, CompilerServices.fldItemTemplates);
							reader.il.Emit (OpCodes.Ldstr, iTempId [0]);//load key
							reader.il.Emit (OpCodes.Callvirt, CompilerServices.miGetITempFromDic);
							reader.il.Emit (OpCodes.Ldloc_0);//load root of treeView
							reader.il.Emit (OpCodes.Callvirt, CompilerServices.miCreateExpDel);
						}
					}
				}
				expectedNode.Index++;
			}
			#endregion

			using (Reader reader = new Reader (context, tmpXml)){
				reader.Read ();

				#region Styling and default values loading
				if (reader.HasAttributes) {
					string style = reader.GetAttribute ("Style");
					if (!string.IsNullOrEmpty (style))						
						CompilerServices.EmitSetValue (reader.il, CompilerServices.piStyle, style);					
				}
				reader.il.Emit (OpCodes.Ldloc_0);
				reader.il.Emit (OpCodes.Callvirt, CompilerServices.miLoadDefaultVals);
				#endregion

				#region Attributes reading
				if (reader.HasAttributes) {

					while (reader.MoveToNextAttribute ()) {
						if (reader.Name == "Style")
							continue;

						MemberInfo mi = crowType.GetMember (reader.Name).FirstOrDefault();
						if (mi == null)
							throw new Exception ("Member '" + reader.Name + "' not found in " + crowType.Name);
						if (mi.MemberType == MemberTypes.Event) {
							CompilerServices.emitBindingCreation (reader.il, reader.Name, reader.Value);
							continue;
						}
						PropertyInfo pi = mi as PropertyInfo;
						if (pi == null)
							throw new Exception ("Member '" + reader.Name + "' not found in " + crowType.Name);

						if (pi.Name == "Name")
							context.Names.Add(reader.Value, Node.AddressToString(context.nodesStack.ToArray()));
						
						if (reader.Value.StartsWith ("{", StringComparison.OrdinalIgnoreCase)) {
							readPropertyBinding(reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));

							//CompilerServices.emitBindingCreation (reader.il, reader.Name, reader.Value.Substring (1, reader.Value.Length - 2));
						}else
							CompilerServices.EmitSetValue (reader.il, pi, reader.Value);

					}
					reader.MoveToElement ();
				}
				#endregion

				if (reader.IsEmptyElement) {
					reader.il.Emit (OpCodes.Pop);//pop saved ref to current object
					return;
				}
				context.nodesStack.Push (expectedNode);
				readChildren (reader);
				context.nodesStack.Pop ();
			}
			il.Emit (OpCodes.Pop);//pop saved ref to current object
		}
		void registerPropertyBinding(string origNode, string origProp, MemberAddress ma){
			if (!context.PropertyBindings.ContainsKey(origNode))
				context.PropertyBindings [origNode] = new Dictionary<string, MemberAddress> ();
			context.PropertyBindings [origNode] [origProp] = ma;
		}
		void readPropertyBinding(string srcProperty, string expression){
			//if binding exp = '{}' => binding is done on datasource
			if (string.IsNullOrEmpty (expression)) {
				registerPropertyBinding ("DS", "", new MemberAddress (context.nodesStack.ToArray (), srcProperty));
				return;
			}

//			if (expression.StartsWith ("²")) {
//				expression = expression.Substring (1);
//				TwoWayBinding = true;
//			}
//
			string [] bindingExp = expression.Split ('/');


			if (bindingExp.Length == 1) {
				registerPropertyBinding ("DS", bindingExp [0],
					new MemberAddress (context.nodesStack.ToArray (), srcProperty));
				return;
			}
				
			string targetName = "";
			string nodeId = "";
			Node[] target = context.nodesStack.ToArray ();

			int nodeIdx = target.Length - 1;//index of target in nodeStack 
			int ptr = 0;//pointer in bindingExp splitted on '/'

//				//if exp start with '/' => WidgetName.property
			if (string.IsNullOrEmpty (bindingExp [0])) {					
				string[] bindTrg = bindingExp [1].Split ('.');
				if (bindTrg.Length == 1) {
					nodeId = Node.AddressToString (target);
					targetName = bindTrg [0];
				}else if (bindTrg.Length == 2) {
					nodeId = context.Names [bindTrg [0]];
					targetName = bindTrg [1];
				} else
					throw new Exception ("Syntax error in binding, expected 'go dot member'");
			} else {
				if (bindingExp [0] == ".") { //template binding
					//parse nodes up until template node
					while (nodeIdx > 0) {
						if (target [nodeIdx].Type == SubNodeType.Template)
							break;
						nodeIdx--;
					}
					ptr++;
				} else {
					while (ptr < bindingExp.Length - 1) {
						if (bindingExp [ptr] == "..")
							nodeIdx--;
						else
							break;
						ptr++;
					}
				}
				Node[] origine = new Node[nodeIdx + 1];
				try {
					Array.Copy (target, origine, nodeIdx + 1);

					int destLength = target.Length - nodeIdx;
					Node[] dest = new Node[destLength];
					Array.Copy (target, nodeIdx, dest, 0, destLength);

				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex.ToString ());
				}


				nodeId = Node.AddressToString (origine);
				targetName = bindingExp [ptr];
			}

			registerPropertyBinding (nodeId, targetName, new MemberAddress (context.nodesStack.ToArray (), srcProperty));
		}
		/// <summary>
		/// Parse child node an generate corresponding msil
		/// </summary>
		void readChildren(Reader reader){
			bool endTagReached = false;
			while (reader.Read()){
				switch (reader.NodeType) {
				case XmlNodeType.EndElement:
					endTagReached = true;
					break;
				case XmlNodeType.Element:
					//Templates
					if (reader.Name == "Template" ||
					    reader.Name == "ItemTemplate") {
						reader.Skip ();
						continue;
					}
						
					
					//push current instance on stack for parenting
					//loc_0 will be used for child
					reader.il.Emit (OpCodes.Ldloc_0);

					Type t = Type.GetType ("Crow." + reader.Name);
					if (t == null) {
						Assembly a = Assembly.GetEntryAssembly ();
						foreach (Type expT in a.GetExportedTypes ()) {
							if (expT.Name == reader.Name)
								t = expT;
						}
					}
					if (t == null)
						throw new Exception (reader.Name + " type not found");

					reader.il.Emit (OpCodes.Newobj, t.GetConstructors () [0]);//TODO:search parameterless ctor
					reader.il.Emit (OpCodes.Stloc_0);//child is now loc_0
					CompilerServices.emitSetCurInterface (il);

					reader.emitLoader (t);

					reader.il.Emit (OpCodes.Ldloc_0);//load child on stack for parenting
					reader.il.Emit (OpCodes.Callvirt, miAddChild [(int)context.nodesStack.Peek ().Type -1]);
					reader.il.Emit (OpCodes.Stloc_0); //reset local to current go
					reader.il.Emit (OpCodes.Ldloc_0);//save current go onto the stack if child has to be added

					context.nodesStack.Peek ().Index++;

					break;
				}
				if (endTagReached)
					break;				
			}
		}
		/// <summary>
		/// read first node to set GraphicObject class for loading
		/// and let reader position on that node
		/// </summary>
		Type findRootType ()
		{
			string root = "Object";
			while (Read()) {
				if (NodeType == XmlNodeType.Element) {
					root = this.Name;
					break;
				}
			}

			Type t = Type.GetType ("Crow." + root);
			if (t == null) {
				Assembly a = Assembly.GetEntryAssembly ();
				foreach (Type expT in a.GetExportedTypes ()) {
					if (expT.Name == root)
						t = expT;
				}
			}
			return t;
		}
	}
}

