// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace vke
{
	public class FloatAnimation2 : Animation
	{

		public float TargetValue;
		protected float initialValue;
		int stepCount, currentStep;

		#region CTOR
		public FloatAnimation2(Object instance, string _propertyName, float Target, int _stepCount = 20)
			: base(instance, _propertyName)
		{

			TargetValue = Target;

			float value = getValue();
			initialValue = value;

			stepCount = _stepCount;
		}
		#endregion

		bool smooth = true;

		float smoothedStep (float step) => (-MathF.Cos (step * MathF.PI) + 1)/2.0f;

		public override void Process()
		{

			currentStep++;

			float t = (float)currentStep / (float)stepCount;
			if (smooth)
				t = smoothedStep (t);

			setValue (initialValue + t * (TargetValue - initialValue));

			if (currentStep < stepCount)
				return;

			setValue(TargetValue);
			AnimationList.Remove(this);
			RaiseAnimationFinishedEvent ();
		}

		public override string ToString ()
		{
			return string.Format ("{0}:->{1}:{2}",base.ToString(),TargetValue,stepCount);
		}
	}

}

