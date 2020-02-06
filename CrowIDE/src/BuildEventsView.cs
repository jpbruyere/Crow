// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using Crow.Cairo;
using Microsoft.Build.Framework;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Crow
{
	public class BuildEventsView : ScrollingObject
	{
		ObservableList<BuildEventArgs> events;
		List<uint> eventsDic = new List<uint>();

		bool scrollOnOutput;
		uint visibleLines = 1;
		uint lineCount = 0;
		FontExtents fe;

		[DefaultValue(true)]
		public virtual bool ScrollOnOutput {
			get { return scrollOnOutput; }
			set {
				if (scrollOnOutput == value)
					return;
				scrollOnOutput = value;
				NotifyValueChanged ("ScrollOnOutput", scrollOnOutput);

			}
		}
		public virtual ObservableList<BuildEventArgs> Events {
			get { return events; }
			set {
				if (events == value)
					return;
				if (events != null) {
					events.ListClear -= Messages_ListClear;
					events.ListAdd -= Lines_ListAdd;
					events.ListRemove -= Lines_ListRemove;
					reset ();
				}
				events = value;
				if (events != null) {
					events.ListClear += Messages_ListClear;
					events.ListAdd += Lines_ListAdd;
					events.ListRemove += Lines_ListRemove;
					lineCount = 0;
					lock (eventsDic) {
						foreach (BuildEventArgs e in events) {
							eventsDic.Add (lineCount);
							if (string.IsNullOrEmpty (e.Message))
								lineCount++;
							else
								lineCount += (uint)Regex.Split (e.Message, "\r\n|\r|\n|\\\\n").Length;
						}
					}
				}
				NotifyValueChanged ("Events", events);
				RegisterForGraphicUpdate ();
			}
		}

		void reset ()
		{
			lineCount = 0;
			lock(eventsDic)
				eventsDic.Clear ();
			ScrollY = ScrollX = 0;
			MaxScrollY = MaxScrollX = 0;
		}

		void Messages_ListClear (object sender, ListChangedEventArg e)
		{
			reset ();
			RegisterForGraphicUpdate ();
		}


		void Lines_ListAdd (object sender, ListChangedEventArg e)
		{
			BuildEventArgs bea = e.Element as BuildEventArgs;
			lock (eventsDic)
				eventsDic.Add (lineCount);
			string msg = bea.Message;
			lineCount += string.IsNullOrEmpty(msg) ? 1 : (uint)Regex.Split (msg, "\r\n|\r|\n|\\\\n").Length;
			MaxScrollY = (int)(lineCount - visibleLines);
			if (scrollOnOutput)
				ScrollY = MaxScrollY;
		}

		void Lines_ListRemove (object sender, ListChangedEventArg e)
		{
			BuildEventArgs bea = e.Element as BuildEventArgs;
			lock (eventsDic)
				eventsDic.RemoveAt (e.Index);
			string msg = (e.Element as BuildEventArgs).Message;
			lineCount -= string.IsNullOrEmpty (msg) ? 1 : (uint)Regex.Split (msg, "\r\n|\r|\n|\\\\n").Length;
			MaxScrollY = (int)(lineCount - visibleLines);
		}


		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height) {
				using (ImageSurface img = new ImageSurface (Format.Argb32, 10, 10)) {
					using (Context gr = new Context (img)) {
						//Cairo.FontFace cf = gr.GetContextFontFace ();

						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);

						fe = gr.FontExtents;
					}
				}
				visibleLines = (uint)(Math.Max(1, Math.Floor ((double)ClientRectangle.Height / fe.Height)));
				MaxScrollY = (int)(lineCount - visibleLines);
			}
		}
		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			if (events == null)
				return;

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);

			Rectangle r = ClientRectangle;

			double y = ClientRectangle.Y;
			double x = ClientRectangle.X;

			int spaces = 0;

			uint [] evts;
			lock (eventsDic)
				evts = eventsDic.ToArray ();

			int idx = Array.BinarySearch (evts, (uint)ScrollY);
			if (idx < 0) 
				idx = ~idx - 1;
			if (idx < 0)
				return;

			int diff = ScrollY - (int)evts [idx];

			int i = 0;
			while (i < visibleLines) {

				if (idx >= events.Count)
					break;
				//if ((lines [i + Scroll] as string).StartsWith ("error", StringComparison.OrdinalIgnoreCase)) {
				//	errorFill.SetAsSource (gr);
				//	gr.Rectangle (x, y, (double)r.Width, fe.Height);
				//	gr.Fill ();
				//	Foreground.SetAsSource (gr);
				//}

				BuildEventArgs evt = events[idx] as BuildEventArgs;

				if (evt is BuildMessageEventArgs) {
					BuildMessageEventArgs msg = evt as BuildMessageEventArgs;
					switch (msg.Importance) {
					case MessageImportance.High:
						gr.SetSourceColor (Color.White);
						break;
					case MessageImportance.Normal:
						gr.SetSourceColor (Color.Grey);
						break;
					case MessageImportance.Low:
						gr.SetSourceColor (Color.Jet);
						break;
					}
				} else if (evt is BuildStartedEventArgs)
					gr.SetSourceColor (Color.White);
				else if (evt is BuildFinishedEventArgs)
					gr.SetSourceColor (Color.White);
				else if (evt is BuildErrorEventArgs)
					gr.SetSourceColor (Color.Red);
				else if (evt is BuildEventArgs)
					gr.SetSourceColor (Color.Yellow);
				else if (evt is BuildStatusEventArgs)
					gr.SetSourceColor (Color.Green);										

				string[] lines = Regex.Split (evt.Message, "\r\n|\r|\n|\\\\n");

				for (int j = diff; j < lines.Length; j++) {
					gr.MoveTo (x, y + fe.Ascent);
					gr.ShowText (new string (' ', spaces) + lines[j]);
					y += fe.Height;
					i++;
					if (y > ClientRectangle.Bottom)
						break;
				}
				diff = 0;
				idx++;

				gr.Fill ();
			}
		}

	}
}

