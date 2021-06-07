// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.CompilerServices;
using Crow;
using Samples;

namespace BindingTest
{
	class Program : SampleBase
	{

		static void Main ()
		{
			using (Program app = new Program ()) {
				app.Run ();
				//DbgLogger.save (app);
			}			
		}

		protected override void OnInitialized ()
		{
			Load ("#ui.test.crow").DataSource = this;
			TcVCInstance = new TestClassVC ();
		}


		void setDataSourceNull (object sender, MouseButtonEventArgs e)
		{
			TcVCInstance = null;
			Console.WriteLine ("set data source null");
		}
		void setDataSourceThis (object sender, MouseButtonEventArgs e)
		{
			TcVCInstance = new TestClassVC () { Prop1 = "instance 1 prop1 value", Prop2 = "instance 1 prop2 value" };
			Console.WriteLine ("set data source not null");
		}

	}
}
