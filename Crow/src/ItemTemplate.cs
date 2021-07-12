// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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
using System.Collections;

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
				using (Widget go = this.CreateInstance())
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

		#region CTOR
		/// <summary>
		/// Initializes a new instance of the <see cref="Crow.ItemTemplate"/> class by parsing the file passed as argument.
		/// </summary>
		/// <param name="path">IML file to parse</param>
		/// <param name="_dataType">type this item will be choosen for, or member of the data item</param>
		/// <param name="_fetchDataMethod">for hierarchical data, method to call for children fetching</param>
		public ItemTemplate (Interface _iface, string path, string _dataTest = "TypeOf", string _dataType = "default", string _fetchDataMethod = null)
			: base(_iface, _iface.GetStreamFromPath (path)) {
			strDataType = _dataType;
			fetchMethodName = _fetchDataMethod;
			dataTest = _dataTest;

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Crow.ItemTemplate"/> class by parsing the IML fragment passed as arg.
		/// </summary>
		/// <param name="ImlFragment">IML fragment to parse</param>
		/// <param name="_dataType">type this item will be choosen for, or member of the data item</param>
		/// <param name="_fetchDataMethod">for hierarchical data, method to call for children fetching</param>
		public ItemTemplate (Interface _iface, Stream ImlFragment, string _dataTest = "TypeOf", string _dataType = "default", string _fetchDataMethod = null)
			: base(_iface, ImlFragment)
		{
			strDataType = _dataType;
			fetchMethodName = _fetchDataMethod;
			dataTest = _dataTest;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="Crow.ItemTemplate"/> class using the opened XmlReader in args.
		/// </summary>
		/// <param name="reader">XML reader positionned before or at the root node</param>
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
				typeof (void), args, typeof(TemplatedGroup), true);			

			System.Reflection.Emit.Label gotoEnd;
			System.Reflection.Emit.Label ifDataIsNull;
			System.Reflection.Emit.Label gotoItemsContainerNotFound;

			ILGenerator il = dm.GetILGenerator (256);			
			
			il.DeclareLocal(typeof(Widget));
			il.DeclareLocal(typeof(IEnumerable));

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
			il.Emit (OpCodes.Stloc_1);//save and reload datas IEnumerable for registering IObsList
			il.Emit (OpCodes.Ldloc_1);

			//set 'return' from the fetch method as 'data' of the list
			//il.Emit (OpCodes.Callvirt, piData.GetSetMethod ());
			il.Emit (OpCodes.Ldloc_0);//load second arg of loadPage, the sender node
			il.Emit (OpCodes.Ldstr, dataTest);//load 3rd arg, dataTest kind on subitems
			il.Emit (OpCodes.Callvirt, CompilerServices.miLoadPage);

			//try register Observable list events
			il.Emit (OpCodes.Ldarg_0);//root templated group
			il.Emit (OpCodes.Ldloc_1);//datas enumerable
			il.Emit (OpCodes.Ldstr, dataTest);//load dataTest kind on subitems
			il.Emit (OpCodes.Ldloc_0);//load items container
			//load datas parent
			/*il.Emit (OpCodes.Ldarg_0);//push root TemplatedGroup into the stack
			il.Emit (OpCodes.Ldarg_1);//load sender node of expand
			il.Emit (OpCodes.Callvirt, CompilerServices.miGetDataSource);*/
			il.Emit (OpCodes.Call, CompilerServices.miRegisterSubData);

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
			System.Reflection.Emit.Label end = il.DefineLabel ();
			System.Reflection.Emit.Label test = il.DefineLabel ();

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
			il.Emit (OpCodes.Isinst, typeof(System.Collections.ICollection));
			il.Emit (OpCodes.Dup);//duplicate children for testing if it's a collection for childs counting
			il.Emit (OpCodes.Brtrue, test);//if true, jump to perform count
			il.Emit (OpCodes.Pop);//pop null
			il.Emit (OpCodes.Ldc_I4_0);//push false			
			il.Emit (OpCodes.Br, end);

			il.MarkLabel (test);

			il.Emit (OpCodes.Callvirt, CompilerServices.miGetColCount);			
			il.Emit (OpCodes.Ldc_I4_0);
			il.Emit (OpCodes.Cgt);

			il.MarkLabel (end);

			il.Emit (OpCodes.Ret);
			HasSubItems = (BooleanTestOnInstance)dm.CreateDelegate (typeof(BooleanTestOnInstance));
			#endregion
		}
		//data is on the stack
		void emitGetSubData(ILGenerator il, Type dataType){
			if (dataType.IsValueType)
				il.Emit (OpCodes.Unbox_Any, dataType);
			MethodInfo miGetDatas = dataType.GetMethod (fetchMethodName, new Type[] {});
			if (miGetDatas == null)
				miGetDatas = iface.SearchExtMethod (dataType, fetchMethodName);

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

	public static partial class Extensions
	{
		public static string GetIcon (this Widget go) {
			return "#Icons." + go.GetType ().FullName + ".svg";
		}
		public static IList<Widget> GetChildren (this Widget go) {
			Type goType = go.GetType ();
			if (typeof (GroupBase).IsAssignableFrom (goType))
				return (go as GroupBase).Children;
			if (typeof (Container).IsAssignableFrom (goType))
				return new List<Widget> (new Widget[] { (go as Container).Child });
			if (typeof (TemplatedContainer).IsAssignableFrom (goType))
				return new List<Widget> (new Widget[] { (go as TemplatedContainer).Content });
			if (typeof (TemplatedGroup).IsAssignableFrom (goType))
				return (go as TemplatedGroup).Items;

			return new List<Widget> ();
		}
	}
}

