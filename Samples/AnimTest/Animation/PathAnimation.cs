// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Numerics;

namespace vke
{
	public class PathAnimation : Animation<Vector3>
	{
		Path path;
		int stepCount, currentStep;

		#region CTOR
		public PathAnimation(Object instance, string _propertyName, Path _path, int _stepCount = 20)
			: base(instance, _propertyName)
		{
			path = _path;
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

			Vector3 pos = path.GetStep (t);
			setValue(pos);

			if (currentStep < stepCount)
				return;

			AnimationList.Remove(this);
			RaiseAnimationFinishedEvent ();
		}

		public override string ToString ()
		{
			return string.Format ("{0}:->{1}:{2}",base.ToString(),TargetValue,Step);
		}
	}
}

