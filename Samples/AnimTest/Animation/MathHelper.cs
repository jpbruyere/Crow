// Copyright (c) 2019-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
namespace vke {
    public static class MathHelper {
        public const float Pi = (float)Math.PI;
        public const float TwoPi = (float)Math.PI * 2;
		public static float NormalizeAngle (float a) {
			float tmp = a % MathHelper.TwoPi;
			if (tmp < 0)
				tmp += MathHelper.TwoPi;
			return tmp;
		}
    }
}
