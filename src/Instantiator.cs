//
//  Instantiator.cs
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
using System.Threading;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace Crow
{
	public delegate void InstanciatorInvoker(object instance, Interface iface);

	public class Instantiator
	{
		public Type RootType;
		InstanciatorInvoker loader;
		string imlPath;


		#region CTOR
		public Instantiator (string path){
			imlPath = path;

			#if DEBUG_LOAD
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif
			try {
				using (IMLReader itr = new IMLReader (path)){
					loader = itr.GetLoader ();
					RootType = itr.RootType;
				}
			} catch (Exception ex) {
				throw new Exception ("Error loading <" + path + ">:\n", ex);
			}

			#if DEBUG_LOAD
			loadingTime.Stop ();
			Debug.WriteLine ("IML Instantiator creation '{2}' : {0} ticks, {1} ms",
			loadingTime.ElapsedTicks, loadingTime.ElapsedMilliseconds, path);
			#endif
		}
		public static Instantiator CreateFromImlFragment(string fragment){
			try {
				using (Stream s = new MemoryStream(Encoding.UTF8.GetBytes(fragment))){
					return new Instantiator(s);
				}
			} catch (Exception ex) {
				throw new Exception ("Error loading fragment:\n" + fragment + "\n", ex);
			}
		}
		public Instantiator (Stream stream){
			#if DEBUG_LOAD
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif
			using (IMLReader itr = new IMLReader (stream)){
				loader = itr.GetLoader ();
				RootType = itr.RootType;
			}
			#if DEBUG_LOAD
			loadingTime.Stop ();
			Debug.WriteLine ("IML Instantiator creation '{2}' : {0} ticks, {1} ms",
				loadingTime.ElapsedTicks, loadingTime.ElapsedMilliseconds, imlPath);
			#endif
		}
		public Instantiator (Type _root, InstanciatorInvoker _loader)
		{
			RootType = _root;
			loader = _loader;
		}
		#endregion

		public GraphicObject CreateInstance(Interface iface){
			GraphicObject tmp = (GraphicObject)Activator.CreateInstance(RootType);
			loader (tmp, iface);
			return tmp;
		}
		public string GetImlSourcesCode(){
			try {
				using (StreamReader sr = new StreamReader (imlPath))
					return sr.ReadToEnd();
			} catch (Exception ex) {
				throw new Exception ("Error getting sources for <" + imlPath + ">:", ex);
			}
		}
	}
}

