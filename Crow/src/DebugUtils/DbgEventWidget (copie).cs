// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using Crow.Cairo;

namespace Crow
{
	/*
	/// <summary>
	/// Compressed event list without gaps
	/// </summary>
	public class DbgEventListWidget : Widget
	{
		public DbgEventListWidget (){}

		DbgWidgetRecord wRec;
		public DbgWidgetRecord WidgetRecord {
			get => wRec;
			set {
				if (wRec == value)
					return;
				wRec = value;
				pixelPerTick = WidgetRecord == null ? 0 : (double)ClientRectangle.Width / WidgetRecord.Duration;
				NotifyValueChangedAuto (wRec);
				RegisterForRedraw ();
			}
		}

		double pixelPerTick;

		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			if (layoutType == LayoutingType.Width)
				pixelPerTick = WidgetRecord == null ? 0 : (double)ClientRectangle.Width / WidgetRecord.Duration;
			base.OnLayoutChanges (layoutType);
		}

		protected override void onDraw (Context gr)
		{
			if (WidgetRecord == null || WidgetRecord.Duration == 0) {
				base.onDraw (gr);
				return;
			}

			Rectangle cb = ClientRectangle;

			drawEvent (gr, cb.Height, WidgetRecord);
		}
		void drawEvent (Context ctx, int h, DbgEvent dbge)
		{
			double w = dbge.Duration == 0 ? 1.0 : dbge.Duration * pixelPerTick;
			double x = (dbge.begin - WidgetRecord.begin) * pixelPerTick;

			ctx.Rectangle (x, Margin, w, h);
			ctx.SetSource (dbge.Color);
			ctx.Fill ();
			if (dbge.Events == null)
				return;
			foreach (DbgEvent e in dbge.Events)
				drawEvent (ctx, h, e);
		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			if (WidgetRecord != null) {
				Point m = ScreenPointToLocal (e.Position);
				long curTick = (long)(m.X / pixelPerTick) + WidgetRecord.begin;
				NotifyValueChanged ("HoverEvent", hoverEvent (WidgetRecord, curTick));
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
	}*/
}
