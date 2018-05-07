//
// CrowApplication.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.Threading;
using vkglfw;

namespace Crow
{
	public class CrowApplication : IDisposable
	{
		public static VkEngine vke;
		public static vkvg.Device dev;

		Interface iFace;
		vkvg.Surface surf;

		public CrowApplication (int width, int height)
		{
			vke = new VkEngine (width, height);
			dev = new vkvg.Device (vke);
			surf = new vkvg.Surface (dev, width, height);

			using (vkvg.Context ctx = new vkvg.Context (surf)) {
				ctx.SetSource (0.1, 0.1, 0.1);
				ctx.Paint ();
			}

				
			iFace = new Interface (surf);
			iFace.Init ();

			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();			
		}
		~CrowApplication ()
		{
			Dispose (false);
		}

		public void Run () {
			vke.Run (surf);
		}
		public GraphicObject Load (string path){			
			return iFace.AddWidget (path);
		}
		void interfaceThread()
		{
			while (iFace.ClientRectangle.Size.Width == 0)
				Thread.Sleep (5);

			while (true) {
				iFace.Update ();
				Thread.Sleep (2);
			}
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing)
				return;

			surf.Dispose ();
			dev.Dispose ();
			vke.Dispose ();
		}
		#endregion

	}
}

