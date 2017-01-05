//
//  PerformanceMeasure.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
			if (cptMeasures == 0)
				return;
			NotifyValueChanged("minimum", minimum);
			NotifyValueChanged("maximum", maximum);
			NotifyValueChanged("current", current);
			//			NotifyValueChanged("total", total);
			//			NotifyValueChanged("cptMeasures", cptMeasures);
			NotifyValueChanged("mean", total / cptMeasures);
		}

		void computeStats(){			
			current = timer.ElapsedTicks;
			if (current < cancelLimit)
				return;
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
			ResetStats();
		}
	}
}

