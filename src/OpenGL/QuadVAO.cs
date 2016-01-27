using System;
using OpenTK;

namespace Crow
{
	public class QuadVAO : VertexArrayObject
	{
		public QuadVAO (float x, float y, float width, float height):base(
			new Vector2[] {
				new Vector2 (x, y),
				new Vector2 (x, y + height),
				new Vector2 (x + width, y),
				new Vector2 (x + width, y + height)
			},
			new Vector2[] {
				new Vector2 (0, 1),
				new Vector2 (0, 0),
				new Vector2 (1, 1),
				new Vector2 (1, 0)
			},
			new int[] { 0, 1, 2, 3 })
		{

		}
		public QuadVAO (float x, float y, float width, float height, 
			float texX, float texY, float texW, float texH):base(
			new Vector2[] {
				new Vector2 (x, y),
				new Vector2 (x, y + height),
				new Vector2 (x + width, y),
				new Vector2 (x + width, y + height)
			},
			new Vector2[] {
					new Vector2 (texX, texY+texH),
					new Vector2 (texX, texY),
					new Vector2 (texX+texW, texY+texH),
					new Vector2 (texX+texW, texY)
			},
			new int[] { 0, 1, 2, 3 })
		{

		}
	}
}

