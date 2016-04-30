//
//  GLCrow.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using GLC;
using Pencil.Gaming;
using System.Collections.Generic;
using Crow;
using System.Linq;
using System.IO;

namespace Tests
{
	public class GLCrow : GLC.Window
	{
		int idx = 0;
		string [] testFiles;

		public int intValue = 25;

		public int IntValue {
			get {
				return intValue;
			}
			set {
				intValue = value;
				NotifyValueChanged ("IntValue", intValue);
			}
		}
		void onSpinnerValueChange(object sender, ValueChangeEventArgs e){
			if (e.MemberName != "Value")
				return;
			intValue = Convert.ToInt32(e.NewValue);
		}
		void change_alignment(object sender, EventArgs e){
			RadioButton rb = sender as RadioButton;
			if (rb == null)
				return;
			NotifyValueChanged ("alignment", Enum.Parse(typeof(Alignment), rb.Caption));
		}
		public IList<String> List2 = new List<string>(new string[]
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
		IList<Color> testList = Color.ColorDic.ToList();
		public IList<Color> TestList {
			set{
				testList = value;
				NotifyValueChanged ("TestList", testList);
			}
			get { return testList; }
		}

		void OnClear (object sender, MouseButtonEventArgs e) => TestList = null;

		void OnLoadList (object sender, MouseButtonEventArgs e) => TestList = Color.ColorDic.ToList();


		[STAThread]
		public static void Main (string [] args)
		{
			using (GLCrow w = new GLCrow ()) {				
				w.Run (30);
			}
		}

		public override void OnLoad ()
		{
			testFiles = Directory.GetFiles(@"Interfaces/Expandable", "*.crow").ToArray();
			testFiles = Directory.GetFiles(@"Interfaces/GraphicObject", "*.crow").Concat(testFiles).ToArray();
			testFiles = Directory.GetFiles(@"Interfaces/Container", "*.crow").Concat (testFiles).ToArray();
			testFiles = Directory.GetFiles(@"Interfaces/Group", "*.crow").Concat (testFiles).ToArray();
			testFiles = Directory.GetFiles(@"Interfaces/Stack", "*.crow").Concat (testFiles).ToArray();
			testFiles = Directory.GetFiles(@"Interfaces/basicTests", "*.crow").Concat (testFiles).ToArray();
			testFiles = Directory.GetFiles (@"Interfaces/Divers", "*.crow").Concat (testFiles).ToArray ();

			//testFiles = Directory.GetFiles(@"Interfaces", "*.crow").Concat(testFiles).ToArray();
			this.Title = testFiles [idx];
			CrowInterface.LoadInterface(testFiles[idx]).DataSource = this;
						
			//CrowInterface.LoadInterface ("#Tests.ui.fps.crow").DataSource = this;			
		}
		public GLCrow(int _width = 800, int _height=600)
			:base(_width, _height, 32, 24, 0, 8, "TestWin")
		{
			
		}

		protected override void OnKeyEvent (GlfwWindowPtr wnd, Pencil.Gaming.Key key, int scanCode, KeyAction action, Pencil.Gaming.KeyModifiers mods)
		{
			base.OnKeyEvent (wnd, key, scanCode, action, mods);
			switch (action) {
			case KeyAction.Release:
				
				break;
			case KeyAction.Press:
				if (key == Pencil.Gaming.Key.Space) {
					CrowInterface.ClearInterface ();
					idx++;
					if (idx == testFiles.Length)
						idx = 0;
					this.Title = testFiles [idx];
					CrowInterface.LoadInterface(testFiles[idx]).DataSource = this;

				}
				break;
			}
		}
	}
}

