// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Diagnostics;

namespace Crow
{
	public class PerformanceMeasure : IValueChange {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			if (ValueChanged != null)				
				ValueChanged.Invoke(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion
		public enum Kind
        {
			Update,
			Clipping,
			Layouting,
			Drawing
        }
		public static PerformanceMeasure[] Measures;		

		[Conditional("MEASURE_TIME")]
		public static void InitMeasures () {
			Measures = new PerformanceMeasure[4];
			Measures[(int)Kind.Update] = new PerformanceMeasure (Kind.Update, 1);
			Measures[(int)Kind.Clipping] = new PerformanceMeasure (Kind.Clipping, 1);
			Measures[(int)Kind.Layouting] = new PerformanceMeasure (Kind.Layouting, 1);
			Measures[(int)Kind.Drawing] = new PerformanceMeasure (Kind.Drawing, 1);
		}
		[Conditional ("MEASURE_TIME")]
		public static void Notify () {
            for (int i = 0; i < 4; i++)
				Measures[i].NotifyChanges ();            
		}
		[Conditional ("MEASURE_TIME")]
		public static void Begin (Kind kind) {
			Measures[(int)kind].StartCycle ();
		}
		[Conditional ("MEASURE_TIME")]
		public static void End (Kind kind) {
			Measures[(int)kind].StopCycle ();
		}


		public Stopwatch timer = new Stopwatch ();
		public readonly Kind MeasureKind;
		public long current, minimum, maximum, total, cptMeasures;
		public long cancelLimit;
		public string Name => MeasureKind.ToString ();

		public PerformanceMeasure (Kind measureKind, long _cancelLimit = 0){
			MeasureKind = measureKind;
			cancelLimit = _cancelLimit;
			ResetStats ();
		}

		public void StartCycle(){
			timer.Restart();
		}
		public void StopCycle(){
			timer.Stop();
			computeStats ();
		}
		public void NotifyChanges(){
			lock(this){
				if (cptMeasures == 0)
					return;
				NotifyValueChanged("minimum", minimum);
				NotifyValueChanged("maximum", maximum);
				NotifyValueChanged("current", current);
				//			NotifyValueChanged("total", total);
				//			NotifyValueChanged("cptMeasures", cptMeasures);
				NotifyValueChanged("mean", total / cptMeasures);
			}
		}

		void computeStats(){			
			current = timer.ElapsedMilliseconds;
			if (current < cancelLimit)
				return;
			cptMeasures++;
			total += timer.ElapsedMilliseconds;
			if (timer.ElapsedMilliseconds < minimum)
				minimum = timer.ElapsedMilliseconds;
			if (timer.ElapsedMilliseconds > maximum)
				maximum = timer.ElapsedMilliseconds;			
		}
		void ResetStats(){
			Debug.WriteLine("reset measure cpt:{0}",cptMeasures);
			cptMeasures = total = current = maximum = 0;
			minimum = long.MaxValue;
		}
		void onResetClick(object sender, MouseButtonEventArgs e){
			lock(this)
				ResetStats();
		}
	}
}

