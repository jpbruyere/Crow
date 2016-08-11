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

namespace Crow
{
	public class IMLStream : MemoryStream {
		public string Path;
		public Type RootType;
		public IMLStream(string path) : base (){
			Path = path;
			using (Stream stream = Interface.GetStreamFromPath (path))
				stream.CopyTo (this);
			//RootType = Interface.GetTopContainerOfXMLStream (this);
		}
		public IMLStream(Byte[] b) : base (b){			
			//RootType = Interface.GetTopContainerOfXMLStream (this);
		}
		/// <summary>
		/// Create a graphicObject instance from the this XML stream.
		/// </summary>
		public GraphicObject Instance {
			get {
				System.Globalization.CultureInfo savedCulture = Thread.CurrentThread.CurrentCulture;
				Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

				Seek (0, SeekOrigin.Begin);
				GraphicObject tmp = null;//Interface.Load (this, this.RootType);

				Thread.CurrentThread.CurrentCulture = savedCulture;
				return tmp;
			}
		}
		/// <summary>
		/// Gets the xml source code as a string
		/// </summary>
		public string Source {
			get {
				Seek (0, SeekOrigin.Begin);
				using (StreamReader sr = new StreamReader(this))
					return sr.ReadToEnd();
			}
		}
	}
	public class ItemTemplate : IMLStream {		
		public EventHandler Expand;

		public ItemTemplate(string path)
			: base(path){}
		public ItemTemplate(Byte[] b)
			: base(b){}

		public void CreateExpandDelegate (TemplatedControl host, string strDataType, string method){
			Type dataType = Type.GetType(strDataType);
			Type hostType = typeof(TemplatedControl);//not sure is the best place to put the dyn method
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
			il.DeclareLocal(typeof(GraphicObject));

			il.Emit (OpCodes.Ldarg_1);

			MethodInfo miFindByName = typeof(GraphicObject).GetMethod("FindByName");
			il.Emit(OpCodes.Ldstr, "List");
			il.Emit (OpCodes.Callvirt, miFindByName);
			il.Emit (OpCodes.Stloc_0);

			FieldInfo fiTemplates = typeof(TemplatedControl).GetField("ItemTemplates");
			il.Emit (OpCodes.Ldloc_0);
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, fiTemplates);
			il.Emit (OpCodes.Stfld, fiTemplates);

			il.Emit (OpCodes.Ldloc_0);
			il.Emit (OpCodes.Ldarg_1);
			il.Emit (OpCodes.Callvirt, typeof(GraphicObject).GetProperty("DataSource").GetGetMethod ());

			MethodInfo miGetDatas = dataType.GetMethod (method, new Type[] {});
			il.Emit (OpCodes.Callvirt, miGetDatas);

			il.Emit (OpCodes.Callvirt, listBoxType.GetProperty("Data").GetSetMethod ());

			il.Emit (OpCodes.Ret);

			#endregion

			Expand = (EventHandler)dm.CreateDelegate (evtType, host);
		}
	}
}

