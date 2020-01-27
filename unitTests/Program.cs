using System;
using Crow;
using Crow.IML;
using NUnit.Framework;

namespace unitTests
{
	[TestFixture]
	public class Instantiator
	{

		[Test]
		public void Widget ()
		{
			Assert.DoesNotThrow (()
				=> Crow.IML.Instantiator.CreateFromImlFragment (null, @"<Widget/>")
				, "test itor failed");
		}
		[Test]
		public void Label ()
		{
			Assert.DoesNotThrow (()
				=> Crow.IML.Instantiator.CreateFromImlFragment (null, @"<Label Text='this is a test'/>")
				, "test itor failed");
		}
		[Test]
		public void TemplatedControl ()
		{
			Assert.DoesNotThrow (()
				=> Crow.IML.Instantiator.CreateFromImlFragment (null, @"<CheckBox IsChecked='false'/>")
				, "test itor failed");
		}

		[Test]
		public void SimpleBinding ()
		{
			Assert.DoesNotThrow (()
				=> Crow.IML.Instantiator.CreateFromImlFragment (null, @"<Widget Background='Blue' Tag='{test}'/>")
				, "test itor failed");
		}

	}
}
