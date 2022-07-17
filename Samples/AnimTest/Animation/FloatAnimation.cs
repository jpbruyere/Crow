// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace vke
{
	public class FloatAnimation : Animation
	{

		public float TargetValue;
		float initialValue;
		public float Step;
		public bool Cycle;

		#region CTOR
		public FloatAnimation(Object instance, string _propertyName, float Target, float step = 0.2f)
			: base(instance, _propertyName)
		{

			TargetValue = Target;

			float value = getValue();
			initialValue = value;

			Step = step;

			if (value < TargetValue)
			{
				if (Step < 0)
					Step = -Step;
			}
			else if (Step > 0)
				Step = -Step;
		}
		#endregion

		public override void Process()
		{
			float value = getValue();

			//Debug.WriteLine ("Anim: {0} <= {1}", value, this.ToString ());

			if (Step > 0f)
			{
				value += Step;
				setValue(value);
				//Debug.WriteLine(value);
				if (TargetValue > value)
					return;
			}
			else
			{
				value += Step;
				setValue(value);

				if (TargetValue < value)
					return;
			}

			if (Cycle) {
				Step = -Step;
				TargetValue = initialValue;
				Cycle = false;
				return;
			}

			setValue(TargetValue);
			lock(AnimationList)
				AnimationList.Remove(this);

			RaiseAnimationFinishedEvent ();
		}

		public override string ToString ()
		{
			return string.Format ("{0}:->{1}:{2}",base.ToString(),TargetValue,Step);
		}
	}

}

