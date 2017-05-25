//
// defineToEnum.cs
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
using System.IO;
using System.Collections.Generic;

namespace testDrm
{
	public class defineToEnum
	{
		static void Main (){
			parseDefines("/usr/include/linux/input-event-codes.h");
		}
		public struct enumdef {
			public string name;
			public int value;
		}
		static void parseDefines (string path){
			Dictionary<string, List<enumdef>> defines = new Dictionary<string, List<enumdef>>();	


			using (Stream s = new FileStream (path, FileMode.Open,FileAccess.Read)) {
				using (StreamReader sr = new StreamReader (s)) {
					while (!sr.EndOfStream) {
						string l = sr.ReadLine ().Trim();
						if (!l.StartsWith ("#define"))
							continue;
						l = l.Substring (8);
						string[] ll = l.Split (new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
						string[] cn = ll [0].Split ('_');
						try {
							string constName = "";
							string enumName = cn [0].ToLowerInvariant ().Substring (1);
							enumName = char.ToUpperInvariant (cn [0][0]) + enumName;							 
							for (int i = 1; i < cn.Length; i++) {
								cn [i] = cn [i].ToLowerInvariant ();
								constName += char.ToUpperInvariant (cn [i] [0]) + cn [i].Substring (1);							 
							}
							if (char.IsDigit (constName[0]))
								constName = "_" + constName;

							int value = 0;
							if (ll [1].StartsWith ("0x")) {
								if (!int.TryParse (ll [1].Substring (2), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.CurrentCulture, out value)){
									Console.WriteLine ("parsing error: " + l);
									continue;
								}
							} else {
								if (!int.TryParse (ll[1], out value)){
									Console.WriteLine ("parsing error: " + l);
									continue;
								}
							}
							if (!defines.ContainsKey(enumName))
								defines[enumName] = new List<enumdef>();
							defines[enumName].Add (new enumdef() {name=constName,value=value});
						} catch (Exception ex) {
							Console.WriteLine ("failed: " + l);
						}
					}
				}
			}
			using (Stream f = new FileStream ("output.txt", FileMode.Create)) {
				using (StreamWriter sw = new StreamWriter (f)){
					foreach (string k in defines.Keys) {
						sw.WriteLine ("public enum {0}Type {{", k);
						foreach (enumdef ed in defines[k]) {
							sw.WriteLine ("\t{0,-20}= 0x{1:X4},", ed.name, ed.value);
						}
						sw.WriteLine ("}");				
					}
				}
			}
		}
	}
}

