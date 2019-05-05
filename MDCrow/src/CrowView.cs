//
// CrowView.cs
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
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using Xwt;

namespace MDCrow
{
	public class CrowView : ViewContent
	{
		CrowCanvas view;

		public override Control Control {
			get { return view; }
		}

		public CrowView () {
			view = new CrowCanvas ();

		}

		string src;

		public override async Task Load (FileOpenInformation fileOpenInformation)
		{
			var fileName = fileOpenInformation.FileName;
			using (Stream stream = File.OpenRead (fileName)) {
				using (StreamReader sr = new StreamReader (stream)) {
					src = await sr.ReadToEndAsync ();
				}
			}

			ContentName = fileName;
			this.IsDirty = false;
			view.SetFocus ();
		}
	}
}
