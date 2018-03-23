//
// ProjectNodes.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;
using Crow;
using System.Threading;

namespace Crow.Coding
{
	public class ImlProjectItem : ProjectFile
	{
		#region CTOR
		public ImlProjectItem (ProjectItem pi) : base (pi){			
		}
		#endregion

		GraphicObject instance;
		Measure designWidth, designHeight;

		/// <summary>
		/// instance created with an instantiator from the source by a DesignInterface,
		/// for now, the one in ImlVisualEditor
		/// </summary>
		public GraphicObject Instance {
			get { return instance; }
			set {
				if (instance == value)
					return;
				instance = value;
				NotifyValueChanged ("Instance", instance);
			}
		}
			
		public Measure DesignWidth {
			get { return designWidth; }
			set { 
				if (designWidth == value)
					return;
				designWidth = value;
				NotifyValueChanged ("DesignWidth", designWidth);
			}
		}
		public Measure DesignHeight {
			get { return designHeight; }
			set {
				if (designHeight == value)
					return;
				designHeight = value;
				NotifyValueChanged ("DesignHeight", designHeight);
			}
		}


		public List<GraphicObject> GraphicTree { 
			get { return new List<GraphicObject> (new GraphicObject[] {instance}); }
		}

		public List<DebugEvent> DebugEvents {
			get { return Interface.DbgEvents; }
		}

		void GTView_SelectedItemChanged (object sender, SelectionChangeEventArgs e){
			SelectedItem = e.NewValue;
		}
	}
}

