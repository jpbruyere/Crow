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

namespace Crow.IML2
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


		void createBindingDelegates(){
//			foreach (Dictionary<string, MemberAddress> pb in IMLCtx.PropertyBindings) {
//				
//			}
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

	}
}

