//
//  vaoMesh.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
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
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Crow
{
	public class vaoMesh : IDisposable
	{
		public int vaoHandle,
		positionVboHandle,
		texVboHandle,
		eboHandle;

		public Vector3[] positions;
		public Vector2[] texCoords;
		public int[] indices;

		public vaoMesh()
		{
		}

		public vaoMesh (Vector3[] _positions, Vector2[] _texCoord, int[] _indices)
		{
			positions = _positions;
			texCoords = _texCoord;
			indices = _indices;

			CreateBuffers ();
		}

		public vaoMesh (float x, float y, float z, float width, float height, float TileX = 1f, float TileY = 1f)
		{
			positions =
				new Vector3[] {
				new Vector3 (x - width / 2, y + height / 2, z),
				new Vector3 (x - width / 2, y - height / 2, z),
				new Vector3 (x + width / 2, y + height / 2, z),
				new Vector3 (x + width / 2, y - height / 2, z)
			};
			texCoords =	new Vector2[] {
				new Vector2 (0, TileY),
				new Vector2 (0, 0),
				new Vector2 (TileX, TileY),
				new Vector2 (TileX, 0)
			};
			indices = new int[] { 0, 1, 2, 3 };

			CreateBuffers ();
		}
		public static vaoMesh CreateCube(){
			vaoMesh tmp = new vaoMesh ();
			tmp.positions = new Vector3[]
			{
				new Vector3(-1.0f, -1.0f,  -1.0f),
				new Vector3( -1.0f, -1.0f,  1.0f),
				new Vector3( 1.0f,  -1.0f,  -1.0f),
				new Vector3(1.0f,  -1.0f,  1.0f),
				new Vector3(1.0f, 1.0f, -1.0f),
				new Vector3( 1.0f, 1.0f, 1.0f), 
				new Vector3( -1.0f,  1.0f, -1.0f),
				new Vector3(-1.0f,  1.0f, 1.0f)
			};
			tmp.indices = new int[]
			{
				// front face
				0, 2, 1, 1, 2, 3,
				// top face
				2, 4, 3, 3, 4, 5,
				// back face
				4, 6, 5, 5, 6, 7,
				// left face
				6, 0, 7, 7, 0, 1,
				// bottom face
				1, 3, 7, 7, 3, 5,
				// right face
//				1, 5, 6, 6, 2, 1,
			};
			tmp.texCoords = new Vector2[]
			{
				new Vector2(0, 0),
				new Vector2(0, 1),
				new Vector2(1, 0),
				new Vector2(1, 1),
				new Vector2(0, 0),
				new Vector2(0, 1),
				new Vector2(1, 0),
				new Vector2(1, 1),
			};			
			tmp.CreateBuffers ();
			return tmp;
//				Normals = new Vector3[]
//				{
//					new Vector3(-1.0f, -1.0f,  1.0f),
//					new Vector3( 1.0f, -1.0f,  1.0f),
//					new Vector3( 1.0f,  1.0f,  1.0f),
//					new Vector3(-1.0f,  1.0f,  1.0f),
//					new Vector3(-1.0f, -1.0f, -1.0f),
//					new Vector3( 1.0f, -1.0f, -1.0f),
//					new Vector3( 1.0f,  1.0f, -1.0f),
//					new Vector3(-1.0f,  1.0f, -1.0f),
//				};
//
//				Colors = new int[]
//				{
//					Utilities.ColorToRgba32(Color.DarkRed),
//					Utilities.ColorToRgba32(Color.DarkRed),
//					Utilities.ColorToRgba32(Color.Gold),
//					Utilities.ColorToRgba32(Color.Gold),
//					Utilities.ColorToRgba32(Color.DarkRed),
//					Utilities.ColorToRgba32(Color.DarkRed),
//					Utilities.ColorToRgba32(Color.Gold),
//					Utilities.ColorToRgba32(Color.Gold),
//				};
		}
		public void CreateBuffers(){
			CreateVBOs ();
			CreateVAOs ();
		}
		protected void CreateVBOs()
		{
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
				new IntPtr(positions.Length * Vector3.SizeInBytes),
				positions, BufferUsageHint.StaticDraw);

			if (texCoords != null) {
				texVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, texVboHandle);
				GL.BufferData<Vector2> (BufferTarget.ArrayBuffer,
					new IntPtr (texCoords.Length * Vector2.SizeInBytes),
					texCoords, BufferUsageHint.StaticDraw);
			}

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			if (indices != null) {
				eboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ElementArrayBuffer, eboHandle);
				GL.BufferData (BufferTarget.ElementArrayBuffer,
					new IntPtr (sizeof(uint) * indices.Length),
					indices, BufferUsageHint.StaticDraw);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			}
		}
		protected void CreateVAOs()
		{
			vaoHandle = GL.GenVertexArray();
			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

			if (texCoords != null) {
				GL.EnableVertexAttribArray (1);
				GL.BindBuffer (BufferTarget.ArrayBuffer, texVboHandle);
				GL.VertexAttribPointer (1, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);
			}
			if (indices != null)
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

			GL.BindVertexArray(0);
		}

		public void Render(BeginMode _primitiveType){
			GL.BindVertexArray(vaoHandle);
			if (indices == null)
				GL.DrawArrays (_primitiveType, 0, positions.Length);
			else
				GL.DrawElements(_primitiveType, indices.Length,
					DrawElementsType.UnsignedInt, IntPtr.Zero);
			GL.BindVertexArray (0);
		}
		public void Render(BeginMode _primitiveType, int[] _customIndices){
			GL.BindVertexArray(vaoHandle);
			GL.DrawElements(_primitiveType, _customIndices.Length,
				DrawElementsType.UnsignedInt, _customIndices);
			GL.BindVertexArray (0);
		}
		public void Render(BeginMode _primitiveType, int instances){

			GL.BindVertexArray(vaoHandle);
			GL.DrawElementsInstanced(_primitiveType, indices.Length,
				DrawElementsType.UnsignedInt, IntPtr.Zero, instances);
			GL.BindVertexArray (0);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteBuffer (eboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}
		#endregion

	}
}