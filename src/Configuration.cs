//
// Configuration.cs
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
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Crow
{
	/// <summary>
	/// single element of configuration
	/// </summary>
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
	/// Application wide Configuration store utility
	/// 
	/// configuration files are automatically stored in **_user/.config/appname/app.config_** on close and every minutes
	/// if some items have changed.
	/// New items are automaticaly added on first use. Configuration class expose one templated Get and one Templated Set, so
	/// creating, storing and retrieving config items is simple as:
	/// 
	/// ```csharp\n
	///     //storing\n
	///     Configuration.Set ("Option1", 42);\n
	///     //loading\n
	///     int op1 = Configuration.Get<int> ("Option1");\n
	/// ```\n
	/// </summary>
	/// 
	/// **.config**  file are simple text files with per line, a key/value pair of the form `option=value`. Keys have to be unique
	/// in the application scope.
	/// 
	/// When running the application for the first time, some default options may be necessary. Their can be defined
	/// in a special embedded resource text file with the key '**appname.default.config**'
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
		/// <summary>
		/// retrive the value of the configuration key given in parameter
		/// </summary>
		/// <param name="key">option name</param>
		public static T Get<T>(string key)
		{
			return !items.ContainsKey (key) ? default(T) : items [key].GetValue<T> ();
		}
		/// <summary>
		/// store the value of the configuration key given in parameter
		/// </summary>
		/// <param name="key">option name</param>
		/// <param name="value">value for that option</param>
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

