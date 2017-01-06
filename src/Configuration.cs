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
using System.Threading;

namespace Crow
{
	public class ConfigItem {
		Type type;
		internal object curVal;
		bool parsingNeeded = false;
		public ConfigItem(object newVal){
			curVal = newVal;
		}
		public ConfigItem(string newVal){
			curVal = newVal;
			parsingNeeded = true;
		}
		public T GetValue<T>(){
			if (type == null)
				type = typeof(T);
			if (parsingNeeded) {
				MethodInfo miParse = type.GetMethod ("Parse", new Type[] {typeof(string)});
				if (miParse != null)
					curVal = miParse.Invoke (null, new object[]{ curVal });
				parsingNeeded = false;
			}
			return (T)Convert.ChangeType (curVal, type);
		}
		public void Set(object value){
			curVal = value;
		}
	}
	/// <summary>
	/// Application wide Configuration utility
	/// </summary>
	public static class Configuration
	{
		volatile static bool isDirty = false;
		static string configPath, configFileName = "app.config";
		static Dictionary<string, ConfigItem> items;

		static Configuration ()
		{
			items = new Dictionary<string, ConfigItem> ();
			string configRoot =
				Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".config");

			Assembly a = Assembly.GetEntryAssembly ();
			string appName = a.GetName().Name;

			configPath = Path.Combine (configRoot, appName);

			if (!Directory.Exists(configPath))
				Directory.CreateDirectory (configPath);

			string path = Path.Combine(configPath, configFileName);

			if (File.Exists (path)) {
				using (Stream s = new FileStream (path, FileMode.Open))
					load (s);
			} else {
				string defaultConfigResID = appName + ".default.config";
				bool found = false;
				foreach (string resIds in a.GetManifestResourceNames()) {
					if (string.Equals (defaultConfigResID, resIds, StringComparison.OrdinalIgnoreCase)) {
						using (Stream s = a.GetManifestResourceStream (defaultConfigResID))
							load (s);
						found = true;
						break;
					}
				}
				if (!found)
					Console.WriteLine ("No Default Config found. ({0})", defaultConfigResID);
			}
			startSavingThread ();
		}
		static void startSavingThread(){
			Thread t = new Thread (savingThread);
			t.IsBackground = true;
			t.Start ();
		}
		static void savingThread(){
			while(true){
				if (isDirty) {
					save ();
					isDirty = false;
				}
				Thread.Sleep (1000);
			}
		}
		public static T Get<T>(string key)
		{
			return !items.ContainsKey (key) ? default(T) : items [key].GetValue<T> ();
		}
		public static void Set<T>(string key, T value)
		{
			if (!items.ContainsKey (key)) {
				lock(items)
					items[key] = new ConfigItem (value);
			}else
				items[key].Set (value);
			isDirty = true;
		}
		static void save(){
			using (Stream s = new FileStream(Path.Combine(configPath, configFileName),FileMode.Create)){
				using (StreamWriter sw = new StreamWriter (s)) {
					lock (items) {
						foreach (string key in items.Keys) {
							sw.WriteLine (key + "=" + (string)items [key].curVal.ToString ());
						}
					}
				}
			}
		}
		static void load(Stream s){
			using (StreamReader sr = new StreamReader (s)) {
				while (!sr.EndOfStream) {
					string l = sr.ReadLine();
					if (string.IsNullOrEmpty (l.Trim ()))
						continue;
					string[] tmp = l.Trim ().Split ('=');
					if (tmp.Length != 2)
						continue;
					items[tmp [0].Trim ()] = new ConfigItem(tmp [1].Trim ());
				}
			}
		}
	}
}

