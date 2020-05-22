using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Crow;

namespace Crow
{
	public class SampleBase : Interface {	
		public Version CrowVersion => Assembly.GetAssembly (typeof (Widget)).GetName ().Version;

		#region Test values for Binding
		public List<Crow.Command> Commands;
		public int intValue = 500;
		VerticalAlignment currentVAlign;

		DirectoryInfo curDir = new DirectoryInfo (Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location));
		public FileSystemInfo[] CurDirectory => curDir.GetFileSystemInfos (); 
		public int IntValue {
			get => intValue;
			set {
				if (IntValue == value)
					return;
				intValue = value;
				NotifyValueChanged ("IntValue", intValue);
			}
		}
		public VerticalAlignment CurrentVAlign {
			get => currentVAlign;
			set {
				if (currentVAlign == value)
					return;
				currentVAlign = value;
				NotifyValueChanged ("CurrentVAlign", currentVAlign);
			}
		}
		void onSpinnerValueChange (object sender, ValueChangeEventArgs e) {
			if (e.MemberName != "Value")
				return;
			intValue = Convert.ToInt32 (e.NewValue);
		}
		void change_alignment (object sender, EventArgs e) {
			RadioButton rb = sender as RadioButton;
			if (rb == null)
				return;
			NotifyValueChanged ("alignment", Enum.Parse (typeof (Alignment), rb.Caption));
		}
		public IEnumerable<String> List2 = new List<string> (new string[]
			{
				"string1",
				"string2",
				"string3",
				"string4",
				"string5",
				"string6",
				"string7",
				"string8",
				"string8",
				"string8",
				"string8",
				"string8",
				"string8",
				"string9"
			}
		);
		public IEnumerable<String> TestList2 {
			set {
				List2 = value;
				NotifyValueChanged ("TestList2", testList);
			}
			get { return List2; }
		}
		public class TestClass
		{
			public string Prop1 { get; set; }
			public string Prop2 { get; set; }

			public override string ToString ()
				=> $"{Prop1}, {Prop2}";

		}
		public IEnumerable<TestClass> List3 = new List<TestClass> (new TestClass[]
			{
				new TestClass { Prop1 = "string1", Prop2="prop2-1" },
				new TestClass { Prop1 = "string2", Prop2="prop2-2" },
				new TestClass { Prop1 = "string3", Prop2="prop2-3" },
			}
		);
		public IEnumerable<string> TestList3Props1 => List3.Select (sc => sc.Prop1).ToList ();
		public IEnumerable<TestClass> TestList3 {
			set {
				List3 = value;
				NotifyValueChanged ("TestList3", testList);
			}
			get { return List3; }
		}
		string prop1;
		public string TestList3SelProp1 {
			get => prop1;
			set {
				if (prop1 == value)
					return;
				prop1 = value;

				NotifyValueChanged ("TestList3SelProp1", prop1);
			}
		}

		string selString;
		public string TestList2SelectedString {
			get => selString;
			set {
				if (selString == value)
					return;
				selString = value;
				NotifyValueChanged ("TestList2SelectedString", selString);
			}
		}


		IList<Colors> testList = (IList<Colors>)FastEnumUtility.FastEnum.GetValues<Colors> ().ToList ();//.ColorDic.Values//.OrderBy(c=>c.Hue)
																										//.ThenBy(c=>c.Value).ThenBy(c=>c.Saturation)
																										//.ToList ();
		public IList<Colors> TestList {
			set {
				testList = value;
				NotifyValueChanged ("TestList", testList);
			}
			get { return testList; }
		}
		void OnClear (object sender, MouseButtonEventArgs e) => TestList = null;
		void OnLoadList (object sender, MouseButtonEventArgs e) => TestList = (IList<Colors>)FastEnumUtility.FastEnum.GetValues<Colors> ().ToList ();


		string curSources = "";
		public string CurSources {
			get { return curSources; }
			set {
				if (value == curSources)
					return;
				curSources = value;
				NotifyValueChanged ("CurSources", curSources);
			}
		}
		bool boolVal = true;
		public bool BoolVal {
			get { return boolVal; }
			set {
				if (boolVal == value)
					return;
				boolVal = value;
				NotifyValueChanged ("BoolVal", boolVal);
			}
		}

		#endregion



	}
}