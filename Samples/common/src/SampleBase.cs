using Crow;
using Glfw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Crow.Drawing;

namespace Samples
{
	public class SampleBase : Interface
	{
#if NETCOREAPP
		static IntPtr resolveUnmanaged(Assembly assembly, String libraryName)
		{

			switch (libraryName)
			{
				case "glfw3":
					return NativeLibrary.Load("glfw", assembly, null);
				case "rsvg-2.40":
					return NativeLibrary.Load("rsvg-2", assembly, null);
			}
			Console.WriteLine($"[UNRESOLVE] {assembly} {libraryName}");
			return IntPtr.Zero;
		}

		static SampleBase()
		{
			System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).ResolvingUnmanagedDll += resolveUnmanaged;
		}
#endif
		public SampleBase(IntPtr hWin) : base(800, 600, hWin) { }
		public SampleBase() : base(800, 600, true, true) { }
		public SampleBase(int width, int height, bool startUIThread, bool createSurface) :
			base(width, height, startUIThread, createSurface)
		{

		}

		public Version CrowVersion => Assembly.GetAssembly(typeof(Widget)).GetName().Version;

		static void showMsgBox (object sender) {
			Widget w = sender as Widget;
			ActionCommand cmd = w.DataSource as ActionCommand;
			MessageBox.ShowModal(w.IFace, MessageBox.Type.Information, $"{cmd?.Caption} CLICKED");
		}

		#region Test values for Binding
		public CommandGroup Commands, AllCommands;
		public CommandGroup EditCommands = new CommandGroup("Edit Commands",
			new ActionCommand("Edit command 1", (sender) => showMsgBox (sender)),
			new ActionCommand("Edit command 2 a bit longer", (sender) => showMsgBox (sender), null, false),
			new ActionCommand("Edit command three", (sender) => showMsgBox (sender)),
			new CommandGroup("Subedit menu",
				new ActionCommand("Subedit command 1", (sender) => showMsgBox (sender)),
				new ActionCommand("Subedit command 2 a bit longer", (sender) => showMsgBox (sender), null, false),
				new ActionCommand("Subedit command three", (sender) => showMsgBox (sender))
			)
		);
		public CommandGroup FileCommands = new CommandGroup("File Commands",
			new ActionCommand("File command 1", (sender) => showMsgBox (sender), "#Icons.gavel.svg"),
			new ActionCommand("File command 2 a bit longer", (sender) => showMsgBox (sender)),
			new ActionCommand("File command three", (sender) => showMsgBox (sender))
		);
		public ActionCommand SingleCommand => new ActionCommand("Single command 1", (sender) => showMsgBox (sender), "#Icons.gavel.svg");
		public ToggleCommand CMDToggleBoolVal, CMDToggleBoolValField;

		public ActionCommand CMDHosted;

		void initCommands()
		{
			CMDToggleBoolVal = new ToggleCommand (this, "Toggle", new Binding<bool> ("BoolVal"), "#Icons.gavel.svg", null, true);
			CMDToggleBoolValField = new ToggleCommand (this, "ToggleField", new Binding<bool> ("boolVal"), "#Icons.gavel.svg", null, true);
			CMDHosted = new ActionCommand (this, "Hosted command",
				() => MessageBox.ShowModal(this, MessageBox.Type.Information, "hosted command triggered"), "#Icons.binding.svg",
				new KeyBinding (Key.F1, Modifier.Super),
				new Binding<bool> ("CanExecute"));
			Commands = new CommandGroup("commands msg boxes",
				new ActionCommand("Action 1", () => MessageBox.ShowModal(this, MessageBox.Type.Information, "context menu 1 clicked")),
				new ActionCommand("Action two", () => MessageBox.ShowModal(this, MessageBox.Type.Information, "context menu 2 clicked"), null, false),
				new ActionCommand("Action three", () => MessageBox.ShowModal(this, MessageBox.Type.Information, "context menu 3 clicked"))
			);
			AllCommands = new CommandGroup ("All Commands",
				FileCommands,
				EditCommands,
				new CommandGroup ("Combined commands", FileCommands, EditCommands),
				new ActionCommand("Action A", () => MessageBox.ShowModal(this, MessageBox.Type.Information, "context menu A clicked"))
			);
		}
		DeviceEventType deviceEventTypeEnum;
		public DeviceEventType DeviceEventTypeEnum {
			get => deviceEventTypeEnum;
			set {
				if (deviceEventTypeEnum == value)
					return;
				deviceEventTypeEnum = value;
				NotifyValueChanged ("DeviceEventTypeEnum", deviceEventTypeEnum);
			}

		}
		public int intValue = 500;
		VerticalAlignment currentVAlign;

		DirectoryInfo curDir = new DirectoryInfo(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
		public FileSystemInfo[] CurDirectory => curDir.GetFileSystemInfos();
		public string MultilineText =
			$"Lorem ipsum dolor sit amet,\nconsectetur adipiscing elit. Sed non risus.\n\nSuspendisse lectus tortor,\nLorem ipsum dolor sit amet,\nconsectetur adipiscing elit. Sed non risus.\n\nSuspendisse lectus tortor,";
		//public string MultilineText = $"a\n";
		TextAlignment textAlignment = TextAlignment.Left;
		public TextAlignment TextAlignment
		{
			get => textAlignment;
			set
			{
				if (textAlignment == value)
					return;
				textAlignment = value;
				NotifyValueChanged(textAlignment);
			}
		}

		public int IntValue
		{
			get => intValue;
			set
			{
				if (IntValue == value)
					return;
				intValue = value;
				NotifyValueChanged("IntValue", intValue);
			}
		}
		public VerticalAlignment CurrentVAlign
		{
			get => currentVAlign;
			set
			{
				if (currentVAlign == value)
					return;
				currentVAlign = value;
				NotifyValueChanged("CurrentVAlign", currentVAlign);
			}
		}
		void onSpinnerValueChange(object sender, ValueChangeEventArgs e)
		{
			if (e.MemberName != "Value")
				return;
			intValue = Convert.ToInt32(e.NewValue);
		}
		void change_alignment(object sender, EventArgs e)
		{
			RadioButton rb = sender as RadioButton;
			if (rb == null)
				return;
			NotifyValueChanged("alignment", Enum.Parse(typeof(Alignment), rb.Caption));
		}
		public IEnumerable<String> List2 = new List<string>(new string[]
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
		public IEnumerable<String> TestList2
		{
			set
			{
				List2 = value;
				NotifyValueChanged("TestList2", testList);
			}
			get { return List2; }
		}
		public class TestClass
		{
			public string Prop1 { get; set; }
			public string Prop2 { get; set; }

			public override string ToString()
				=> $"{Prop1}, {Prop2}";

			public void OnValidateCommand(Object sender, ValidateEventArgs e)
			{
				Console.WriteLine($"Validation: {e.ValidatedText}");
			}
		}
		public class TestClassVC : IValueChange
		{
			public event EventHandler<ValueChangeEventArgs> ValueChanged;
			public void NotifyValueChanged(object _value, [CallerMemberName] string caller = null)
				=> ValueChanged.Raise(this, new ValueChangeEventArgs(caller, _value));
			string prop1, prop2;
			public string Prop1
			{
				get => prop1;
				set
				{
					if (prop1 == value)
						return;
					prop1 = value;
					NotifyValueChanged(prop1);
				}
			}
			public string Prop2
			{
				get => prop2;
				set
				{
					if (prop2 == value)
						return;
					prop2 = value;
					NotifyValueChanged(prop2);
				}



			}

			public override string ToString()
					=> $"{Prop1}, {Prop2}";

		}
		TestClass tcInstance = new TestClass() { Prop1 = "instance 0 prop1 value", Prop2 = "instance 0 prop2 value" };
		TestClassVC tcVCInstance;// = new TestClassVC () { Prop1 = "instance 0 prop1 value", Prop2 = "instance 0 prop2 value" };
		TestClass tcInstance1 = new TestClass() { Prop1 = "instance 1 prop1 value", Prop2 = "instance 1 prop2 value" };
		TestClassVC tcVCInstance1 = new TestClassVC() { Prop1 = "instance 1 prop1 value", Prop2 = "instance 1 prop2 value" };
		TestClass tcInstance2 = new TestClass() { Prop1 = "instance 2 prop1 value", Prop2 = "instance 2 prop2 value" };
		TestClassVC tcVCInstance2 = new TestClassVC() { Prop1 = "instance 2 prop1 value", Prop2 = "instance 2 prop2 value" };

		public TestClass TcInstance
		{
			get => tcInstance;
			set
			{
				if (tcInstance == value)
					return;
				tcInstance = value;
				NotifyValueChanged(tcInstance);
			}
		}
		public TestClassVC TcVCInstance
		{
			get => tcVCInstance;
			set
			{
				if (tcVCInstance == value)
					return;
				tcVCInstance = value;
				NotifyValueChanged(tcVCInstance);
			}
		}

		void tcInstance_ChangeProperties_MouseClick(object sender, MouseButtonEventArgs e)
		{
		}
		public void tcInstance_ChangeInstance_MouseClick(object sender, MouseButtonEventArgs e)
		{
			if (TcInstance == tcInstance1)
				TcInstance = tcInstance2;
			else
				TcInstance = tcInstance1;
		}
		void tcVCInstance_ChangeInstance_MouseClick(object sender, MouseButtonEventArgs e)
		{
			TcVCInstance = new TestClassVC() { Prop1 = "prop1 value changed", Prop2 = "prop2 value changed" };
		}

		public IEnumerable<TestClass> List3 = new List<TestClass>(new TestClass[]
			{
				new TestClass { Prop1 = "string1", Prop2="prop2-1" },
				new TestClass { Prop1 = "string2", Prop2="prop2-2" },
				new TestClass { Prop1 = "string3", Prop2="prop2-3" },
			}
		);
		public IEnumerable<string> TestList3Props1 => List3.Select(sc => sc.Prop1).ToList();
		public IEnumerable<TestClass> TestList3
		{
			set
			{
				List3 = value;
				NotifyValueChanged("TestList3", testList);
			}
			get { return List3; }
		}
		string testString;
		public string TestString
		{
			get => testString;
			set
			{
				if (testString == value)
					return;
				testString = value;

				NotifyValueChanged(testString);
			}
		}
		string prop1;
		public string TestList3SelProp1
		{
			get => prop1;
			set
			{
				if (prop1 == value)
					return;
				prop1 = value;

				NotifyValueChanged(prop1);
			}
		}

		string selString;
		public string TestList2SelectedString
		{
			get => selString;
			set
			{
				if (selString == value)
					return;
				selString = value;
				NotifyValueChanged(selString);
			}
		}


		IList<Colors> testList = (IList<Colors>)EnumsNET.Enums.GetValues<Colors>().ToList();//.ColorDic.Values//.OrderBy(c=>c.Hue)
																									  //.ThenBy(c=>c.Value).ThenBy(c=>c.Saturation)
																									  //.ToList ();
		public IList<Colors> TestList
		{
			set
			{
				testList = value;
				NotifyValueChanged("TestList", testList);
			}
			get { return testList; }
		}
		void OnClear(object sender, MouseButtonEventArgs e) => TestList = null;
		void OnLoadList(object sender, MouseButtonEventArgs e) => TestList = (IList<Colors>)EnumsNET.Enums.GetValues<Colors>().ToList();


		string curSources = "";
		public bool boolVal = true, canExecute;
		public string CurSources
		{
			get => curSources;
			set	{
				if (value == curSources)
					return;
				curSources = value;
				NotifyValueChanged(curSources);
			}
		}
		public bool BoolVal
		{
			get => boolVal;
			set	{
				if (boolVal == value)
					return;
				boolVal = value;
				NotifyValueChanged(boolVal);
			}
		}
		public bool CanExecute
		{
			get => canExecute;
			set	{
				if (canExecute == value)
					return;
				canExecute = value;
				NotifyValueChanged(canExecute);
			}
		}
		public Color AllWidgetBackground {
			get => Configuration.Global.Get<Color> (nameof(AllWidgetBackground));
			set {
				if (value == AllWidgetBackground)
					return;
				Configuration.Global.Set (nameof(AllWidgetBackground), value);
				NotifyValueChanged (value);
			}
		}





		#endregion

		protected override void OnInitialized()
		{
			initCommands();
			base.OnInitialized();
		}
		protected override void processDrawing(Context ctx)
		{
			base.processDrawing(ctx);
		}

		public override bool OnKeyDown(KeyEventArgs e)
		{

			switch (e.Key)
			{
				case Key.F5:
					Load("Interfaces/Divers/testFileDialog.crow").DataSource = this;
					return true;
				case Key.F6:
					Load("Interfaces/Divers/0.crow").DataSource = this;
					return true;
				case Key.F7:
					Load("Interfaces/Divers/perfMeasures.crow").DataSource = this;
					return true;
				case Key.F2:
					if (IsKeyDown(Key.LeftShift))
						DbgLogger.Reset();
					DbgLogger.Save(this);
					return true;
			}
			return base.OnKeyDown(e);
		}
	}
}