// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace vke
{
	public class AngleAnimation : FloatAnimation2
	{
		#region CTOR
		public AngleAnimation(Object instance, string PropertyName, float Target, int stepCount = 20) :
			base (instance,PropertyName,MathHelper.NormalizeAngle(Target),stepCount) {
				initialValue = MathHelper.NormalizeAngle (initialValue);
			}
		#endregion
	}
}

