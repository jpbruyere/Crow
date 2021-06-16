// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Crow.Cairo;
using Crow.DebugLogger;
using DebugLogAnalyzer;

namespace Crow
{
	public class DbgEventView : TemplatedContainer {
		DbgEvent evt;
		public DbgEvent Event {
			get => evt;
			set {
				if (evt == value)
					return;
				evt = value;
				NotifyValueChangedAuto (evt);
			}
		}
	}
	public class DbgEventWidget : Widget
	{
		public DbgEventWidget (){}

		DbgEvent evt, hoverEvt;
		long ticksPerPixel;
		double pixelPerTick;

		object dataMutex = new object();

		public DbgEvent Event {
			get => evt;
			set {
				if (evt == value)
					return;
				lock (dataMutex)
					evt = value;
				updatePixelPerTicks ();
				NotifyValueChangedAuto (evt);
				RegisterForRedraw ();
			}
		}
		public DbgEvent HoverEvent {
			get => hoverEvt;
			private set {
				if (hoverEvt == value)
					return;
				lock (dataMutex)
					evt = value;
				hoverEvt = value;
				NotifyValueChangedAuto (hoverEvt);
			}
		}

		[DefaultValue ("1000")]
		public long TicksPerPixel {
			get => ticksPerPixel;
			set {
				if (ticksPerPixel == value)
					return;
				ticksPerPixel = value;
				NotifyValueChangedAuto (ticksPerPixel);
				if (Width == Measure.Fit)
					RegisterForLayouting (LayoutingType.Width);
			}
		}

		public override int measureRawSize (LayoutingType lt)
		{
			updatePixelPerTicks ();
			if (lt == LayoutingType.Width)
				contentSize.Width = Event == null ? 0 : (int)Math.Max(pixelPerTick * Event.Duration, 2);
			
			return base.measureRawSize (lt);
		}

		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			if (layoutType == LayoutingType.Width)
				updatePixelPerTicks ();

			base.OnLayoutChanges (layoutType);
		}

		protected override void onDraw (Context gr)
		{
			lock (dataMutex) {

				if (Event == null) {
					base.onDraw (gr);
					return;
				}

				gr.LineWidth = 1;
				gr.SetDash (new double [] { 1.0, 3.0 }, 0);

				Rectangle cb = ClientRectangle;

				if (Event.Duration == 0) {
					gr.SetSource (Event.Color);
					gr.Rectangle (cb);
					gr.Fill ();
					return;
				}

				drawEvent (gr, cb.Height, Event);
			}
		}
		void drawEvent (Context ctx, int h, DbgEvent dbge)
		{
			double w = Math.Max(dbge.Duration * pixelPerTick, 2.0);
			double x = (dbge.begin - Event.begin) * pixelPerTick;

			ctx.Rectangle (x, 0, w, h);
			ctx.SetSource (dbge.Color);
			/*if (dbge.IsSelected) {
				ctx.FillPreserve ();
				ctx.SetSource (1, 1, 1);
				ctx.Stroke ();
			}else*/
				ctx.Fill ();

			if (dbge.Events == null)
				return;
			foreach (DbgEvent e in dbge.Events)
				drawEvent (ctx, h, e);
		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			if (Event != null) {
				Point m = ScreenPointToLocal (e.Position);
				long curTick = (long)(m.X / pixelPerTick) + Event.begin;
				HoverEvent = hoverEvent (Event, curTick);

				e.Handled = true;
			}
			base.onMouseMove (sender, e);
		}

		DbgEvent hoverEvent (DbgEvent hevt, long curTick){
			if (hevt.Events != null) {
				foreach (DbgEvent e in hevt.Events) {
					if (curTick >= e.begin && curTick <= e.end)
						return hoverEvent (e, curTick);
				}
			}
			return hevt;
		}
		void updatePixelPerTicks ()
		{
			if (Width == Measure.Fit)
				pixelPerTick = 1.0 / ticksPerPixel;
			else
				pixelPerTick = Event == null ? 0 : (double)ClientRectangle.Width / Event.Duration;
		}
	}
}