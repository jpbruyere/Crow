// Copyright (c) 2015-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Numerics;

namespace vke {
    public class Path 
	{
		public Vector3 Start;
		public Vector3 End;

		float length;
		Vector3 vDir;

		public Path(Vector3 startPos, Vector3 endPos){
			Start = startPos;
			End = endPos;
			initialComputations ();
		}
		protected void initialComputations(){
			Vector3 vPath = End - Start;
			vDir = Vector3.Normalize (vPath);
			length = vPath.Length();
		}
		/// <summary>
		/// Get single step on the path
		/// </summary>
		/// <returns>return position on the path</returns>
		/// <param name="pos">Position expressed as percentage of total length</param>
		public virtual Vector3 GetStep(float pos){
			return Start + vDir * length * pos;
		}

		public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			float u = 1 - t;
			float tt = t * t;
			float uu = u * u;
			float uuu = uu * u;
			float ttt = tt * t;

			Vector3 p = uuu * p0;
			p += 3 * uu * t * p1;
			p += 3 * u * tt * p2;
			p += ttt * p3;

			return p;
		}
    }

}
