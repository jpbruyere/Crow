using System;
using Crow;
using Crow.IML;
using NUnit.Framework;

namespace unitTests
{
	[TestFixture]
	public class Tests
	{


		void instanciate ()
		{
			Instantiator.CreateFromImlFragment (null, @"<Widget Background='Blue' Tag='{test}'/>");
		}
		


		[Test]
		public void InstanciatorTest ()
		{
			Assert.DoesNotThrow (instanciate, "test itor failed");
		}

	}
}
