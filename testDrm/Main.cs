//
// Main.cs
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
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Cairo;
using Crow.Linux;
using Crow;

namespace testDrm
{
	
	public class TestApp : Application, IValueChange 
	{
		static void Main ()
		{
			try {
				using (TestApp crowApp = new TestApp ()) {
					crowApp.Run ();
				}
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}
		}

		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		public bool Running = true;

		public TestApp () : base () {

		}
		int frTime = 0;
		int frMin = int.MaxValue;
		int frMax = 0;

		public override void Run ()
		{
			Stopwatch frame = new Stopwatch ();
			Load ("#testDrm.ui.menu.crow").DataSource = this;
			Load ("#testDrm.ui.0.crow").DataSource = this;
			Load ("#testDrm.ui.0.crow").DataSource = this;
			Load ("#testDrm.ui.0.crow").DataSource = this;
			Load ("#testDrm.ui.0.crow").DataSource = this;

			while(Running){
				try {
					frame.Restart();
					base.Run ();
					frame.Stop();
					frTime = (int)frame.ElapsedTicks;
					NotifyValueChanged("frameTime", frTime);
					if (frTime > frMax){
						frMax = frTime;
						NotifyValueChanged("frameMax", frMax);	
					}
					if (frTime < frMin){
						frMin = frTime;
						NotifyValueChanged("frameMin", frMin);	
					}

				} catch (Exception ex) {
					Console.WriteLine (ex.ToString());
				}
			}
		}
		void onQuitClick(object send, Crow.MouseButtonEventArgs e)
		{
			Running = false;
		}
	}
}

