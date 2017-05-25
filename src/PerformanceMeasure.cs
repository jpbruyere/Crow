//
// PerformanceMeasure.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
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

		public Stopwatch timer = new Stopwatch ();
		public string Name;
		public long current, minimum, maximum, total, cptMeasures;
		public long cancelLimit;

		public PerformanceMeasure(string name = "unamed", long _cancelLimit = 0){
			Name = name;
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
			current = timer.ElapsedTicks;
//			if (current < cancelLimit)
//				return;
			cptMeasures++;
			total += timer.ElapsedTicks;
			if (timer.ElapsedTicks < minimum)
				minimum = timer.ElapsedTicks;
			if (timer.ElapsedTicks > maximum)
				maximum = timer.ElapsedTicks;			
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

