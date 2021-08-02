// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Globalization;
using Crow;
using NUnit.Framework;

namespace unitTests
{
	public class TestInterface : Interface
	{
		public TestInterface (int width = 800, int height = 600)
			: base (width, height, false, false) {
			surf = CreateSurface (ClientRectangle.Width, ClientRectangle.Height);
		}
		bool isRunning;
		public bool IsRunning {
			get => isRunning;
			set => isRunning = value; 
		}

	}
	[TestFixture]
	public class TestBackend
	{
		TestInterface iFace;

		[OneTimeSetUp]
		public void Init ()
		{
			iFace = new TestInterface (800, 600);
			iFace.Init ();
		}

		//[SetUp] public void InitTest (){}
		//[TearDown] public void CleanupTest (){}

		[Test]
		public void Widget ()
			=> Assert.DoesNotThrow (()
				=> { iFace.LoadIMLFragment (@"<Widget/>"); iFace.Update (); }
				, "iFace load IML fragment failed");
		[Test]
		public void Label ()
			=> Assert.DoesNotThrow (()
				=> { iFace.LoadIMLFragment (@"<Label Text='this is a test string'/>"); iFace.Update (); }
				, "iFace load IML fragment failed");

		[Test]
		public void TemplatedControl ()
			=> Assert.DoesNotThrow (()
				=> { iFace.LoadIMLFragment (@"<CheckBox IsChecked='true'/>"); iFace.Update (); }
				, "iFace load IML fragment failed");
	}
}
