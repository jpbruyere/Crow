//
//  IMLStream.cs
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
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Crow
{
	public class ItemTemplate : Instantiator {		
		public EventHandler Expand;
		string strDataType;
		string fetchMethodName;

		#region CTOR
		public ItemTemplate(string path, string _dataType = null, string _fetchDataMethod = null)
			: base(path) {
			strDataType = _dataType;
			fetchMethodName = _fetchDataMethod;
		}
		public ItemTemplate (Type _root, InstanciatorInvoker _loader,string _dataType, string _fetchDataMethod)
			:base(_root, _loader)
		{
			strDataType = _dataType;
			fetchMethodName = _fetchDataMethod;
		}
		#endregion

		public void CreateExpandDelegate (TemplatedControl host){
			Type dataType = Type.GetType(strDataType);
			Type hostType = typeof(TemplatedControl);//not sure is the best place to put the dyn method
			Type evtType = typeof(EventHandler);
			Type listBoxType = typeof(ListBox);

			PropertyInfo piListData = listBoxType.GetProperty ("Data");

			MethodInfo evtInvoke = evtType.GetMethod ("Invoke");
			ParameterInfo [] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;

			Type [] args = { typeof (object), typeof (object), handlerArgsType };
			DynamicMethod dm = new DynamicMethod ("dyn_expand_" + fetchMethodName,
				typeof (void),
				args,
				hostType);


			#region IL generation
			System.Reflection.Emit.Label gotoEnd;
			System.Reflection.Emit.Label ifDataIsNull;

			ILGenerator il = dm.GetILGenerator (256);
			il.DeclareLocal(typeof(GraphicObject));

			gotoEnd = il.DefineLabel ();
			ifDataIsNull = il.DefineLabel ();

			il.Emit (OpCodes.Ldarg_1);//load sender of expand event

			MethodInfo miFindByName = typeof(GraphicObject).GetMethod("FindByName");
			il.Emit(OpCodes.Ldstr, "List");
			il.Emit (OpCodes.Callvirt, miFindByName);
			il.Emit (OpCodes.Stloc_0);

			//check that 'Data' of list is not already set
			il.Emit (OpCodes.Ldloc_0);
			il.Emit (OpCodes.Callvirt, piListData.GetGetMethod ());
			il.Emit (OpCodes.Brfalse, ifDataIsNull);
			il.Emit (OpCodes.Br, gotoEnd);

			il.MarkLabel(ifDataIsNull);
			//copy the ref of ItemTemplates list TODO: maybe find another way to share it among the nodes?
			FieldInfo fiTemplates = typeof(TemplatedControl).GetField("ItemTemplates");
			il.Emit (OpCodes.Ldloc_0);
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, fiTemplates);
			il.Emit (OpCodes.Stfld, fiTemplates);

			//call 'fetchMethodName' from the dataSource to build the sub nodes list
			il.Emit (OpCodes.Ldloc_0);//push 'List' (of sub nodes) into the stack
			il.Emit (OpCodes.Ldarg_1);//get the dataSource of the sender
			il.Emit (OpCodes.Callvirt, typeof(GraphicObject).GetProperty("DataSource").GetGetMethod ());

			emitGetSubData(il, dataType);

			//set 'return' from the fetch method as 'data' of the list
			il.Emit (OpCodes.Callvirt, piListData.GetSetMethod ());

			il.MarkLabel(gotoEnd);
			il.Emit (OpCodes.Ret);

			#endregion

			Expand = (EventHandler)dm.CreateDelegate (evtType, host);
		}
		void emitGetSubData(ILGenerator il, Type dataType){
			MethodInfo miGetDatas = dataType.GetMethod (fetchMethodName, new Type[] {});
			if (miGetDatas == null)
				miGetDatas = CompilerServices.SearchExtMethod (dataType, fetchMethodName);

			if (miGetDatas == null) {//in last resort, search among properties
				PropertyInfo piDatas = dataType.GetProperty (fetchMethodName);
				if (piDatas == null)
					throw new Exception ("Fetch data member not found in ItemTemplate: " + fetchMethodName);
				miGetDatas = piDatas.GetGetMethod ();
				if (miGetDatas == null)
					throw new Exception ("Read only property for fetching data in ItemTemplate: " + fetchMethodName);
			}

			il.Emit (OpCodes.Callvirt, miGetDatas);
		}
	}
}

