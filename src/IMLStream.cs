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

namespace Crow
{
	public class IMLStream : MemoryStream {
		public string Path;
		public Type RootType;
		public IMLStream(string path) : base (){
			Path = path;
			using (Stream stream = Interface.GetStreamFromPath (path))
				stream.CopyTo (this);
			RootType = Interface.GetTopContainerOfXMLStream (this);
		}
		public IMLStream(Byte[] b) : base (b){			
			RootType = Interface.GetTopContainerOfXMLStream (this);
		}
	}
	public class ItemTemplate : IMLStream {		
		public EventHandler Expand;

		public ItemTemplate(string path)
			: base(path){}
		public ItemTemplate(Byte[] b)
			: base(b){}

		public void CreateExpandDelegate (string strDataType, string method){
			Type dataType = Type.GetType(strDataType);
			Type hostType = typeof(ItemTemplate);//not sure is the best place to put the dyn method
			Type evtType = typeof(EventHandler);
			Type listBoxType = typeof(ListBox);

			MethodInfo evtInvoke = evtType.GetMethod ("Invoke");
			ParameterInfo [] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;

			Type [] args = { typeof (object), typeof (object), handlerArgsType };
			DynamicMethod dm = new DynamicMethod ("dyn_expand_" + method,
				typeof (void),
				args,
				hostType);


			#region IL generation
			ILGenerator il = dm.GetILGenerator (256);

			il.Emit (OpCodes.Ldarg_1);

			MethodInfo miFindByName = typeof(GraphicObject).GetMethod("FindByName");
			il.Emit(OpCodes.Ldstr, "List");
			il.Emit (OpCodes.Callvirt, miFindByName);

			il.Emit (OpCodes.Ldarg_1);
			il.Emit (OpCodes.Callvirt, typeof(GraphicObject).GetProperty("DataSource").GetGetMethod ());

			MethodInfo miGetDatas = dataType.GetMethod (method, new Type[] {});
			il.Emit (OpCodes.Callvirt, miGetDatas);

			il.Emit (OpCodes.Callvirt, listBoxType.GetProperty("Data").GetSetMethod ());

			il.Emit (OpCodes.Ret);

			#endregion

			Expand = (EventHandler)dm.CreateDelegate (evtType, this);
		}
	}
}

