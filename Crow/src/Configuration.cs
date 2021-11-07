// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

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
				if (type.IsEnum) {
					curVal = Enum.Parse (typeof(T), (string)curVal);
				}else{
					MethodInfo miParse = type.GetMethod ("Parse", new Type[] {typeof(string)});
					if (miParse != null)
						curVal = miParse.Invoke (null, new object[]{ curVal });
				}
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
	/// configuration files are automatically stored in **_user/.config/appname/appname.config_** on close and at interval defined by
	/// the static field `Configuration.AUTO_SAVE_INTERVAL` but only if some items have changed.
	/// New items are automaticaly added on first use. Configuration class expose one templated Get and one Templated Set, so
	/// creating, storing and retrieving config items is simple as:
	///
	/// ```csharp\n
	///     //storing\n
	///     Configuration.Global.Set ("Option1", 42);\n
	///     //loading\n
	///     int op1 = Configuration.Global.Get<int> ("Option1");\n
	/// ```\n
	/// </summary>
	///
	/// **.config**  file are simple text files with per line, a key/value pair of the form `option=value`. Keys have to be unique
	/// in the application scope.
	///
	/// When running the application for the first time, some default options may be necessary. Their can be defined
	/// in a special embedded resource text file with the key '**appname.default.config**'
	///
	/// You may also provide a default value when you fetch an item:
	/// ```csharp\n
	///     int op1 = Configuration.Global.Get<int> ("Option1", 10);\n
	/// ```\n
	///
	public class Configuration
	{
		volatile bool isDirty = false;
		string configPath;
		Dictionary<string, ConfigItem> items = new Dictionary<string, ConfigItem> ();
		static Configuration  globalConfig;
		/// <summary>
		/// Interval in milliseconds between configuration file saving. Save is done only if an item has changed.
		/// Default value is 200.
		/// </summary>
		public static int AUTO_SAVE_INTERVAL = 200;
		/// <summary>
		/// Default application wide store for configuration items. It's created and updated automaticaly when needed.
		/// It's path is: **_user/.config/appname/appname.config_**.
		/// </summary>
		public static Configuration Global => globalConfig;
		/// <summary>
		/// Create a custom configuration store with the provided path.
		/// </summary>
		/// <param name="path">the full path where to save this configuration store.</param>
		/// <param name="defaultConf">an optional text stream with default values.</param>
		public Configuration (string path, Stream defaultConf = null) {
			configPath = path;
			if (File.Exists (configPath)) {
				using (Stream s = new FileStream (configPath, FileMode.Open))
					load (s);

			} else if (defaultConf != null) {
				load (defaultConf);
			}
			startSavingThread ();
		}
		/// <summary>
		/// Create a readonly configuration
		/// </summary>
		/// <param name="defaultConf"></param>
		public Configuration (Stream defaultConf = null) {
			load (defaultConf);
		}
		/// <summary>
		/// Get the application configuration directory full path.
		/// </summary>
		/// <returns>application configuration directory full path</returns>
		public static string AppConfigPath => Path.Combine (
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".config") ,
			Assembly.GetEntryAssembly ().GetName().Name);

		static Configuration ()
		{
			string configRoot =
				Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".config");

			Assembly a = Assembly.GetEntryAssembly ();
			string appName = a.GetName().Name;
			string globalConfigPath = Path.Combine (configRoot, appName);

			if (!Directory.Exists (globalConfigPath))
				Directory.CreateDirectory (globalConfigPath);

			globalConfigPath = Path.Combine (globalConfigPath, "global.config");

			string defaultConfigResID = appName + ".default.config";
			foreach (string resIds in a.GetManifestResourceNames()) {
				if (string.Equals (defaultConfigResID, resIds, StringComparison.OrdinalIgnoreCase)) {
					using (Stream s = a.GetManifestResourceStream (defaultConfigResID))
						globalConfig = new Configuration (globalConfigPath, s);
					return;
				}
			}
			globalConfig = new Configuration (globalConfigPath);
		}

		/// <summary>
		/// Get all the configuration item names currently present in this configuration store.
		/// </summary>
		/// <value>configuration item names</value>
		public string[] Names {
			get {
				return items.Keys.ToArray ();
			}
		}

		void startSavingThread(){
			Thread t = new Thread (savingThread);
			t.IsBackground = true;
			t.Start ();
		}
		void savingThread(){
			while(true){
				if (isDirty)
					Save ();
				Thread.Sleep (100);
			}
		}
		/// <summary>
		/// Try to fetch a configuration item by name.
		/// </summary>
		/// <param name="key">the configuration item name</param>
		/// <param name="result">the current value, or the type default if not found in store.</param>
		/// <typeparam name="T">The type of the configuration item</typeparam>
		/// <returns>true if the item was present, false otherwise</returns>
		public bool TryGet<T> (string key, out T result) {
			if (items.ContainsKey (key)){
				result = items [key].GetValue<T> ();
				return true;
			}
			result = default(T);
			return false;
		}
		/// <summary>
		/// Retrieve an item from this configuration store identified by the key given in parameter
		/// </summary>
		/// <param name="key">configuration item name</param>
		/// <typeparam name="T">the type of the configuration item</typeparam>
		/// <returns>The current value or the default one if not yet defined.</returns>
		public T Get<T>(string key)
		{
			return !items.ContainsKey (key) ? default(T) : items [key].GetValue<T> ();
		}
		/// <summary>
		/// Retrieve an item from this configuration store identified by the key given in parameter, or
		/// return the default value provided as parameter.
		/// </summary>
		/// <param name="key">configuration item name</param>
		/// <param name="defaultValue">a default value in case this item is not yet present in the configuration store</param>
		/// <typeparam name="T">the type of the configuration item</typeparam>
		/// <returns>The current value or the default one if not yet defined.</returns>
		public T Get<T>(string key, T defaultValue)
		{
			return !items.ContainsKey (key) ? defaultValue : items [key].GetValue<T> ();
		}
		/// <summary>
		/// store the value of the configuration key given in parameter
		/// </summary>
		/// <param name="key">option name</param>
		/// <param name="value">value for that option</param>
		public void Set<T>(string key, T value)
		{
			if (!items.ContainsKey (key)) {
				lock(items)
					items[key] = new ConfigItem (value);
			}else
				items[key].Set (value);
			isDirty = true;
		}
		/// <summary>
		/// Save this configuration store with the path provided on creation. This is done automaticaly normaly.
		/// </summary>
		public void Save(){
			using (Stream s = new FileStream(configPath,FileMode.Create)){
				using (StreamWriter sw = new StreamWriter (s)) {
					lock (items) {
						foreach (string key in items.Keys) {
							if (items [key].curVal != null)
								sw.WriteLine (key + "=" + (string)items [key].curVal.ToString ());
						}
					}
				}
			}
			isDirty = false;
		}
		void load(Stream s){
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

