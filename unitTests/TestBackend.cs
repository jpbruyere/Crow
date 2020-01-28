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
		public TestInterface (int width = 800, int height = 600, IBackend _backend = null)
			: base (width, height, _backend, false) {
			backend.Init (this);
		}
		public bool IsRunning {
			get => running;
			set { running = value; } 
		}
	}
	[TestFixture]
	public class TestBackend : IBackend
	{
		TestInterface iFace;

		[OneTimeSetUp]
		public void Init ()
		{
			iFace = new TestInterface (800, 600, this);
			iFace.Init ();
			iFace.IsRunning = true;
		}

		[OneTimeTearDown]
		public void Cleanup ()
		{
			iFace.IsRunning = false;
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

		#region IBackend implementation
		public MouseCursor Cursor { set { } }

		public bool Shift => false;

		public bool Ctrl => false;

		public bool Alt => false;

		public void CleanUp () {}

		public void Flush () {}

		public void Init (Interface iFace)
		{
			iFace.surf = new Crow.Cairo.ImageSurface (Crow.Cairo.Format.Argb32, iFace.ClientRectangle.Width, iFace.ClientRectangle.Height);
		}

		public bool IsDown (Key key) => false;

		public void ProcessEvents ()
		{

		}
		#endregion
	}
}
