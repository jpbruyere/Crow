//
//  ImageViewer.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

// 
// HexEditorView.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Fonts;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using go;
using MonoDevelop.DesignerSupport;
using MonoDevelop.SourceEditor;

namespace MonoDevelop.GOLib
{
	class CustomVPaned : Gtk.VPaned, IPropertyPadProvider
	{
		#region IPropertyPadProvider implementation
		public object GetActiveComponent ()
		{
			return this.Child1 == null ? this as object: (this.Child1 as GOLibGtkHost).activeWidget as object;
		}
		public object GetProvider ()
		{
			return null;
			//throw new NotImplementedException ();
		}
		public void OnEndEditing (object obj)
		{
			//throw new NotImplementedException ();
		}
		public void OnChanged (object obj)
		{
			//throw new NotImplementedException ();
		}
		#endregion
		
	}
	class GOLibView : SourceEditorView
	{
		GOLibGtkHost gtkGoWidgetHost;
		CustomVPaned gtkGOMLWidget;


		double zoom = 1.0;
		
		public override Gtk.Widget Control {
			get {
				return gtkGOMLWidget;
			}
		}

		public GOLibView () : base()
		{			
			gtkGoWidgetHost = new GOLibGtkHost ();
			gtkGOMLWidget = new CustomVPaned ();
			gtkGOMLWidget.CanFocus = true;
			gtkGOMLWidget.Name = "vpaned1";
			gtkGOMLWidget.Add (gtkGoWidgetHost);
			gtkGOMLWidget.Add (base.Control);
			gtkGOMLWidget.SizeAllocated += GtkGOMLWidget_SizeAllocated;

			this.Document.DocumentUpdated += Document_DocumentUpdated;
			//this.DirtyChanged += GOLibView_DirtyChanged;
		}

		void Document_DocumentUpdated (object sender, EventArgs e)
		{
			reloadGOML ();
		}

		void GOLibView_DirtyChanged (object sender, EventArgs e)
		{

		}

		void reloadGOML()
		{
			using (MemoryStream stream = new MemoryStream ()) {
				using (StreamWriter writer = new StreamWriter (stream)) {
					writer.Write (this.Document.Text);
					writer.Flush ();

					stream.Position = 0;
					gtkGoWidgetHost.Load (stream);
				}
			}			
		}
			
		void GtkGOMLWidget_SizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			gtkGoWidgetHost.SetSizeRequest (-1, args.Allocation.Height / 2);
		}

		public override void Load (string fileName)
		{							
			gtkGoWidgetHost.Load (fileName);
			//ContentName = fileName;
			//this.IsDirty = false;
			gtkGOMLWidget.ShowAll ();
			gtkGOMLWidget.Show ();

			base.Load (fileName);
		}
//		public override bool CanReuseView (string fileName)
//		{
//			return base.CanReuseView (fileName);
//		}
//		public override void RedrawContent ()
//		{
//			base.RedrawContent ();
//		}

	}
}
