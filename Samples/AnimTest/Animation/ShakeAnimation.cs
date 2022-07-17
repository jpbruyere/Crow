// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace vke
{
	public class ShakeAnimation : Animation
	{
		const float stepMin = 0.001f, stepMax = 0.005f;
		bool rising = true;

		public float LowBound;
		public float HighBound;

		#region CTOR
		public ShakeAnimation(
			Object instance,
			string _propertyName,
			float lowBound, float highBound)
			: base(instance, _propertyName)
		{

			LowBound = Math.Min (lowBound, highBound);
			HighBound = Math.Max (lowBound, highBound);

			float value = getValue ();

			if (value > HighBound)
				rising = false;
		}
		#endregion

		public override void Process ()
		{
			float value = getValue ();
			float step = stepMin + (float)random.NextDouble () * stepMax;

			if (rising) {
				value += step;
				if (value > HighBound) {
					value = HighBound;
					rising = false;
				}
			} else {
				value -= step;
				if (value < LowBound) {
					value = LowBound;
					rising = true;
				} else if (value > HighBound)
					value -= step * 10f;
			}
			setValue (value);
		}

	}

}

