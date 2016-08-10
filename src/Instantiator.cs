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

namespace Crow
{
	public class Instantiator
	{
		public Type RootType;
		Interface.LoaderInvoker loader;

		#region CTOR
		public Instantiator (string path){
			System.Globalization.CultureInfo savedCulture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

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
				throw new Exception ("Error loading <" + path + ">:", ex);
			}

			#if DEBUG_LOAD
			loadingTime.Stop ();
			Debug.WriteLine ("IML Instantiator creation '{2}' : {0} ticks, {1} ms",
			loadingTime.ElapsedTicks, loadingTime.ElapsedMilliseconds, path);
			#endif

			Thread.CurrentThread.CurrentCulture = savedCulture;			
		}
		public Instantiator (Type _root, Interface.LoaderInvoker _loader)
		{
			RootType = _root;
			loader = _loader;
		}
		#endregion

		public GraphicObject CreateInstance(){
			GraphicObject tmp = (GraphicObject)Activator.CreateInstance(RootType);
			loader (tmp);
			return tmp;
		}
	}
}

