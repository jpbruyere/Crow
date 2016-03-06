using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Crow
{
	public class VertexArrayObject : IDisposable
	{
		public int vaoHandle,
		positionVboHandle,
		texVboHandle,
		eboHandle;

		Vector2[] positionVboData;
		public int[] indicesVboData;
		Vector2[] texVboData;

		public VertexArrayObject (Vector2[] _positions, Vector2[] _texCoord, int[] _indices)
		{
			positionVboData = _positions;
			texVboData = _texCoord;
			indicesVboData = _indices;

			CreateVBOs ();
			CreateVAOs ();
		}

		void deleteVAOs()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteBuffer (eboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}

		void CreateVBOs()
		{
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
				new IntPtr(positionVboData.Length * Vector2.SizeInBytes),
				positionVboData, BufferUsageHint.StaticDraw);

			texVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, texVboHandle);
			GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
				new IntPtr(texVboData.Length * Vector2.SizeInBytes),
				texVboData, BufferUsageHint.StaticDraw);
			//
			eboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer,
				new IntPtr(sizeof(uint) * indicesVboData.Length),
				indicesVboData, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		void CreateVAOs()
		{
			vaoHandle = GL.GenVertexArray();
			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);

			GL.EnableVertexAttribArray(1);
			GL.BindBuffer(BufferTarget.ArrayBuffer, texVboHandle);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

			GL.BindVertexArray(0);
		}

		public void Render(PrimitiveType _primitiveType){
			GL.BindVertexArray(vaoHandle);
			GL.DrawElements(_primitiveType, indicesVboData.Length,
				DrawElementsType.UnsignedInt, IntPtr.Zero);
			GL.BindVertexArray (0);
		}


		#region IDisposable implementation
		public void Dispose ()
		{
			deleteVAOs ();
		}
		#endregion
	}
}
