//
// ItemTemplate.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Crow.IML;

namespace Crow
{
	/// <summary> Test func on data, return yes if there's children </summary>
	public delegate bool BooleanTestOnInstance(object instance);

	/// <summary>
	/// Derived from Instantiator with sub data fetching facilities for hierarchical data access.
	/// 
	/// ItemTemplate stores the dynamic method for instantiating the control tree defined in a valid IML file.
	/// 
	/// </summary>
	public class ItemTemplate : Instantiator {
		#if DESIGN_MODE
		public void getIML (System.Xml.XmlDocument doc, System.Xml.XmlNode parentElem){		
			if (sourcePath == "#Crow.DefaultItem.template")
				return;
			
			XmlElement xe = doc.CreateElement("ItemTemplate");
			XmlAttribute xa = null;

			if (string.IsNullOrEmpty (sourcePath)) {
				//inline item template
				using (GraphicObject go = this.CreateInstance())
					go.getIML (doc, xe);
			} else {
				xa = doc.CreateAttribute ("Path");
				xa.Value = sourcePath;
				xe.Attributes.Append (xa);
			}

			if (strDataType != "default") {
				xa = doc.CreateAttribute ("DataType");
				xa.Value = strDataType;
				xe.Attributes.Append (xa);

				if (dataTest != "TypeOf"){
					xa = doc.CreateAttribute ("DataTest");
					xa.Value = dataTest;
					xe.Attributes.Append (xa);
				}
			}

			if (!string.IsNullOrEmpty(fetchMethodName)){
				xa = doc.CreateAttribute ("Data");
				xa.Value = fetchMethodName;
				xe.Attributes.Append (xa);
			}
				
			parentElem.AppendChild (xe);

				
		}
		#endif

		public EventHandler Expand;
		public BooleanTestOnInstance HasSubItems;
		string strDataType;
		string fetchMethodName;
		string dataTest;

		static Stream getItemTemplateStream (string path, Type declaringType)
		{
			Stream s = null;
			if (path.StartsWith ("#", StringComparison.Ordinal)) {
				string resId = path.Substring (1);
				s = Assembly.GetEntryAssembly ().GetManifestResourceStream (resId);
				if (s == null)
					s = Assembly.GetAssembly (declaringType).GetManifestResourceStream (resId);
				if (s == null)
					throw new Exception ($"Item Template not found '{path}'");
			} else {
				if (!File.Exists (path))
					throw new FileNotFoundException ("Item Template not found: ", path);
				s = new FileStream (path, FileMode.Open, FileAccess.Read);
			}
			return s;
		}

		#region CTOR
		/// <summary>
		/// Initializes a new instance of the <see cref="Crow.ItemTemplate"/> class by parsing the file passed as argument.
		/// </summary>
		/// <param name="path">IML file to parse</param>
		/// <param name="_dataType">type this item will be choosen for, or member of the data item</param>
		/// <param name="_fetchDataMethod">for hierarchical data, method to call for children fetching</param>
		public ItemTemplate (Interface _iface, string path, Type declaringType, string _dataTest = "TypeOf", string _dataType = "default", string _fetchDataMethod = null)
			: base(_iface, getItemTemplateStream(path, declaringType)) {
			strDataType = _dataType;
			fetchMethodName = _fetchDataMethod;
			dataTest = _dataTest;

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Crow.ItemTemplate"/> class by parsing the IML fragment passed as arg.
		/// </summary>
		/// <param name="path">IML fragment to parse</param>
		/// <param name="_dataType">type this item will be choosen for, or member of the data item</param>
		/// <param name="_fetchDataMethod">for hierarchical data, method to call for children fetching</param>
		public ItemTemplate (Interface _iface, Stream ImlFragment, string _dataTest, string _dataType, string _fetchDataMethod)
			:base(_iface, ImlFragment)
		{
			strDataType = _dataType;
			fetchMethodName = _fetchDataMethod;
			dataTest = _dataTest;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="Crow.ItemTemplate"/> class using the opened XmlReader in args.
		/// </summary>
		/// <param name="path">XML reader positionned before or at the root node</param>
		/// <param name="_dataType">type this item will be choosen for, or member of the data item</param>
		/// <param name="_fetchDataMethod">for hierarchical data, method to call for children fetching</param>
		public ItemTemplate (Interface _iface, XmlReader reader, string _dataTest = "TypeOf" , string _dataType = null, string _fetchDataMethod = null)
			:base(_iface, reader)
		{
			strDataType = _dataType;
			fetchMethodName = _fetchDataMethod;
			dataTest = _dataTest;
		}
		#endregion

		/// <summary>
		/// Creates the expand delegate.
		/// </summary>
		/// <param name="host">Host.</param>
		public void CreateExpandDelegate (TemplatedGroup host){
			Type dataType = CompilerServices.getTypeFromName(strDataType);
			Type tmpGrpType = typeof(TemplatedGroup);
			Type evtType = typeof(EventHandler);

			MethodInfo evtInvoke = evtType.GetMethod ("Invoke");
			ParameterInfo [] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;

			Type [] args = { typeof(object), typeof(object), handlerArgsType };

			#region Expand dyn meth
			//DM is bound to templatedGroup root (arg0)
			//arg1 is the sender of the expand event
			DynamicMethod dm = new DynamicMethod ("dyn_expand_" + fetchMethodName,
				typeof (void), args,typeof(TemplatedGroup), true);

			System.Reflection.Emit.Label gotoEnd;
			System.Reflection.Emit.Label ifDataIsNull;
			System.Reflection.Emit.Label gotoItemsContainerNotFound;

			ILGenerator il = dm.GetILGenerator (256);
			il.DeclareLocal(typeof(GraphicObject));

			gotoEnd = il.DefineLabel ();
			ifDataIsNull = il.DefineLabel ();
			gotoItemsContainerNotFound = il.DefineLabel ();

			il.Emit (OpCodes.Ldarg_1);//load sender of expand event
			//TODO:double check if items container could be known when expand del is created
			//to avoid a find by name
			il.Emit(OpCodes.Ldstr, "ItemsContainer");//load name to find
			il.Emit (OpCodes.Callvirt, CompilerServices.miFindByName);
			il.Emit (OpCodes.Stloc_0);//save items container as loc0

			//ensure ItemsContainer is not null
			il.Emit (OpCodes.Ldloc_0);
			il.Emit (OpCodes.Brfalse, gotoItemsContainerNotFound);

			//check that node is not already expanded
			il.Emit (OpCodes.Ldarg_0);//push root TemplatedGroup into the stack
			il.Emit (OpCodes.Ldarg_1);
			il.Emit (OpCodes.Call, CompilerServices.miIsAlreadyExpanded);
			il.Emit (OpCodes.Brtrue, gotoEnd);

			//get the dataSource of the sender
			il.Emit (OpCodes.Ldarg_0);//push root TemplatedGroup into the stack
			il.Emit (OpCodes.Ldarg_1);//load sender node of expand
			il.Emit (OpCodes.Callvirt, CompilerServices.miGetDataSource);

			if (fetchMethodName != "self") {//special keyword self allows the use of recurent list<<<
				if (dataType == null) {
					//dataTest was not = TypeOF, so we have to get the type of data
					//dynamically and fetch

					il.Emit (OpCodes.Ldstr, fetchMethodName);
					il.Emit (OpCodes.Call, CompilerServices.miGetDataTypeAndFetch);
				}else
					emitGetSubData(il, dataType);			
			}
			//set 'return' from the fetch method as 'data' of the list
			//il.Emit (OpCodes.Callvirt, piData.GetSetMethod ());
			il.Emit (OpCodes.Ldloc_0);//load second arg of loadPage, the sender node
			il.Emit (OpCodes.Ldstr, dataTest);//load 3rd arg, dataTest kind on subitems
			il.Emit (OpCodes.Callvirt, CompilerServices.miLoadPage);
			il.Emit (OpCodes.Br, gotoEnd);

			il.MarkLabel(gotoItemsContainerNotFound);
			il.EmitWriteLine("ItemsContainer not found in ItemTemplate for " + host.ToString());


			il.MarkLabel(gotoEnd);
			il.Emit (OpCodes.Ret);

			Expand = (EventHandler)dm.CreateDelegate (evtType, host);
			#endregion

			#region Items counting dyn method
			//dm is unbound, arg0 is instance of Item container to expand
			dm = new DynamicMethod ("dyn_count_" + fetchMethodName,
				typeof (bool), new Type[] {typeof(object)}, true);
			il = dm.GetILGenerator (256);

			//get the dataSource of the arg0
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Callvirt, CompilerServices.miGetDataSource);

			if (fetchMethodName != "self") {//special keyword self allows the use of recurent list<<<
				if (dataType == null) {
					//dataTest was not = TypeOF, so we have to get the type of data
					//dynamically and fetch

					il.Emit (OpCodes.Ldstr, fetchMethodName);
					il.Emit (OpCodes.Call, CompilerServices.miGetDataTypeAndFetch);
				}else
					emitGetSubData(il, dataType);			
			}
			
			il.Emit (OpCodes.Callvirt, CompilerServices.miGetColCount);
			il.Emit (OpCodes.Ldc_I4_0);
			il.Emit (OpCodes.Cgt);
			il.Emit (OpCodes.Ret);
			HasSubItems = (BooleanTestOnInstance)dm.CreateDelegate (typeof(BooleanTestOnInstance));
			#endregion
		}
		//data is on the stack
		void emitGetSubData(ILGenerator il, Type dataType){
			MethodInfo miGetDatas = dataType.GetMethod (fetchMethodName, new Type[] {});
			if (miGetDatas == null)
				miGetDatas = CompilerServices.SearchExtMethod (dataType, fetchMethodName);

			if (miGetDatas == null) {//in last resort, search among properties
				PropertyInfo piDatas = dataType.GetProperty (fetchMethodName);
				if (piDatas == null) {
					FieldInfo fiDatas = dataType.GetField (fetchMethodName);
					if (fiDatas == null)//and among fields
						throw new Exception ("Fetch data member not found in ItemTemplate: " + fetchMethodName);
					il.Emit (OpCodes.Ldfld, fiDatas);
					return;
				}
				miGetDatas = piDatas.GetGetMethod ();
				if (miGetDatas == null)
					throw new Exception ("Write only property for fetching data in ItemTemplate: " + fetchMethodName);
			}
            if (miGetDatas.IsStatic)
			    il.Emit (OpCodes.Call, miGetDatas);
            else
                il.Emit (OpCodes.Callvirt, miGetDatas);
        }

	
	
	}
}

