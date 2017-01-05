//
//  Configuration.cs
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
using System.Reflection;
using System.IO;
using System.Collections.Generic;

namespace Crow
{
	//TODO:maybe create iterator
	public static class Configuration
	{
		static string configPath, configFileName = "app.config";
		static Dictionary<string, string> items;

		static Configuration ()
		{
			items = new Dictionary<string, string> ();
			string configRoot = 
				Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".config");

			Assembly a = Assembly.GetEntryAssembly ();
			string appName = a.GetName().Name;

			configPath = Path.Combine (configRoot, appName);

			if (!Directory.Exists(configPath))
				Directory.CreateDirectory (configPath);
			load ();
		}
		public static T Get<T>(string key)
		{
			if (!items.ContainsKey (key))
				return default(T);
			Type type = typeof(T);
			MethodInfo miParse = type.GetMethod ("Parse", new Type[] {typeof(string)});
			if (miParse == null)				
				return (T)Convert.ChangeType (items [key], typeof(T));

			return (T)Convert.ChangeType (miParse.Invoke (null, new object[]{ items [key] }), type);
		}
		public static void Set<T>(string key, T value)
		{
			items [key] = value.ToString();
			save ();
		}
		static void save(){
			using (Stream s = new FileStream(Path.Combine(configPath, configFileName),FileMode.Create)){
				using (StreamWriter sw = new StreamWriter (s)) {
					foreach (string key in items.Keys) {						
						sw.WriteLine(key + "=" + items[key]);
					}
				}
			}
		}
		static void load(){			
			string path = Path.Combine(configPath, configFileName);
			if (!File.Exists (path))
				return;
			using (Stream s = new FileStream(path, FileMode.Open)){
				using (StreamReader sr = new StreamReader (s)) {
					while (!sr.EndOfStream) {						
						string l = sr.ReadLine();
						if (string.IsNullOrEmpty (l.Trim ()))
							continue;
						string[] tmp = l.Trim ().Split ('=');
						if (tmp.Length != 2)
							continue;
						items[tmp [0].Trim ()] = tmp [1].Trim ();
					}
				}
			}
		}
	}
}

