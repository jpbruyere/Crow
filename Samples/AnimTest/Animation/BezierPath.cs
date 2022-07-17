// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Numerics;

namespace vke
{
	public class BezierPath : Path
	{
		public Vector3 ControlPointStart;
		public Vector3 ControlPointEnd;

		public BezierPath (Vector3 startPos, Vector3 controlPointStart,
			Vector3 controlPointEnd, Vector3 endPos)
			:base(startPos, endPos)
		{
			ControlPointStart = controlPointStart;
			ControlPointEnd = controlPointEnd;
		}
		public BezierPath (Vector3 startPos, Vector3 endPos, Vector3 vUp)
			:base(startPos, endPos)
		{
			ControlPointStart = startPos + vUp;
			ControlPointEnd = endPos + vUp;
		}
		public override Vector3 GetStep (float pos)
		{
			return Path.CalculateBezierPoint (pos, Start, ControlPointStart, ControlPointEnd, End);
		}
	}
}

