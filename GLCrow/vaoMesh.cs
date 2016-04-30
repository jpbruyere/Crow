using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using Pencil.Gaming.MathUtils;
using Pencil.Gaming.Graphics;

namespace GGL
{
	public class vaoMesh : IDisposable
	{
		public int vaoHandle,
		positionVboHandle,
		normalsVboHandle,
		texVboHandle,
		matVboHandle,
		eboHandle;

		public Vector3[] positions;
		public Vector3[] normals;
		public Vector2[] texCoords;
		public Matrix[] modelMats;
		public int[] indices;

		public string Name = "Unamed";

		public vaoMesh()
		{
		}

		public vaoMesh (Vector3[] _positions, Vector2[] _texCoord, int[] _indices)
		{
			positions = _positions;
			texCoords = _texCoord;
			indices = _indices;

			CreateVBOs ();
			CreateVAOs ();
		}

		public vaoMesh (Vector3[] _positions, Vector2[] _texCoord, Vector3[] _normales, int[] _indices, Matrix[] _modelMats = null)
		{
			positions = _positions;
			texCoords = _texCoord;
			normals = _normales;
			indices = _indices;
			modelMats = _modelMats;


			CreateVBOs ();
			CreateVAOs ();
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
			normals = new Vector3[] {
				Vector3.UnitZ,
				Vector3.UnitZ,
				Vector3.UnitZ,
				Vector3.UnitZ
			};
			indices = new int[] { 0, 1, 2, 3 };

			CreateVBOs ();
			CreateVAOs ();
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

			if (normals != null) {
				normalsVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, normalsVboHandle);
				GL.BufferData<Vector3> (BufferTarget.ArrayBuffer,
					new IntPtr (normals.Length * Vector3.SizeInBytes),
					normals, BufferUsageHint.StaticDraw);
			}

			if (texCoords != null) {
				texVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, texVboHandle);
				GL.BufferData<Vector2> (BufferTarget.ArrayBuffer,
					new IntPtr (texCoords.Length * Vector2.SizeInBytes),
					texCoords, BufferUsageHint.StaticDraw);
			}

			if (modelMats != null) {
				matVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, matVboHandle);
				GL.BufferData<Matrix> (BufferTarget.ArrayBuffer,
					new IntPtr (modelMats.Length * Vector4.SizeInBytes * 4),
					modelMats, BufferUsageHint.DynamicDraw);
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
			if (normals != null) {
				GL.EnableVertexAttribArray (2);
				GL.BindBuffer (BufferTarget.ArrayBuffer, normalsVboHandle);
				GL.VertexAttribPointer (2, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
			}
//			if (modelMats != null) {
//				GL.VertexAttribDivisor (4, 1);
//				for (int i = 0; i < 4; i++) {
//					GL.EnableVertexAttribArray (4 + i);
//					GL.VertexAttribBinding (4+i, 4);
//					GL.VertexAttribFormat(4+i, 4, VertexAttribType.Float, false, Vector4.SizeInBytes * i);
//				}
//				GL.BindVertexBuffer (4, matVboHandle, IntPtr.Zero, Vector4.SizeInBytes*4);
//			}

			if (indices != null)
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
			
			GL.BindVertexArray(0);
		}

		public void Render( BeginMode _primitiveType){
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

		public static vaoMesh operator +(vaoMesh m1, vaoMesh m2){
			if (m1 == null)
				return m2;
			if (m2 == null)
				return m1;

			vaoMesh res = new vaoMesh ();

			m1.Dispose ();
			m2.Dispose ();

			if (m1.positions == null) {
				res.positions = m2.positions;
				res.texCoords = m2.texCoords;
				res.normals = m2.normals;
				res.indices = m2.indices;
			} else {
				int offset = m1.positions.Length;

				res.positions = new Vector3[m1.positions.Length + m2.positions.Length];
				m1.positions.CopyTo (res.positions, 0);
				m2.positions.CopyTo (res.positions, m1.positions.Length);

				if (m1.texCoords != null) {
					res.texCoords = new Vector2[m1.texCoords.Length + m2.texCoords.Length];
					m1.texCoords.CopyTo (res.texCoords, 0);
					m2.texCoords.CopyTo (res.texCoords, m1.texCoords.Length);
				}

				if (m1.normals != null) {
					res.normals = new Vector3[m1.normals.Length + m2.normals.Length];
					m1.normals.CopyTo (res.normals, 0);
					m2.normals.CopyTo (res.normals, m1.normals.Length);
				}

				res.indices = new int[m1.indices.Length + m2.indices.Length];
				m1.indices.CopyTo (res.indices, 0);
				for (int i = 0; i < m2.indices.Length; i++) {
					if (m2.indices [i] == int.MaxValue)
						res.indices [i + m1.indices.Length] = int.MaxValue;
					else
						res.indices [i + m1.indices.Length] = m2.indices [i] + offset;
				}
			}
			return res;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (normalsVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteBuffer (matVboHandle);
			GL.DeleteBuffer (eboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}
		#endregion

		public static vaoMesh CreateGrid(int gridSize)
		{
			const float z = 0.0f;
			const int IdxPrimitiveRestart = int.MaxValue;

			Vector3[] positionVboData;
			int[] indicesVboData;
			Vector2[] texVboData;

			positionVboData = new Vector3[gridSize * gridSize];
			texVboData = new Vector2[gridSize * gridSize];
			indicesVboData = new int[(gridSize * 2 + 1) * gridSize];

			for (int y = 0; y < gridSize; y++) {
				for (int x = 0; x < gridSize; x++) {
					positionVboData [gridSize * y + x] = new Vector3 (x, y, z);
					texVboData [gridSize * y + x] = new Vector2 ((float)x*0.5f, (float)y*0.5f);

					if (y < gridSize-1) {
						indicesVboData [(gridSize * 2 + 1) * y + x*2] = gridSize * y + x;
						indicesVboData [(gridSize * 2 + 1) * y + x*2 + 1] = gridSize * (y+1) + x;
					}

					if (x == gridSize-1) {
						indicesVboData [(gridSize * 2 + 1) * y + x*2 + 2] = IdxPrimitiveRestart;
					}
				}
			}
			return new vaoMesh (positionVboData, texVboData, null,indicesVboData);
//			vaoMesh tmp = new vaoMesh (positionVboData, texVboData, null);
//			tmp.indices = indicesVboData;
//			return tmp;
		}

		static List<Vector3> objPositions;
		static List<Vector3> objNormals;
		static List<Vector2> objTexCoords;
		static List<Vector3> lPositions;
		static List<Vector3> lNormals;
		static List<Vector2> lTexCoords;
		static List<int> lIndices;

		public static vaoMesh Load(string fileName)
		{
			objPositions = new List<Vector3>();
			objNormals = new List<Vector3>();
			objTexCoords = new List<Vector2>();
			lPositions = new List<Vector3>();
			lNormals = new List<Vector3>();
			lTexCoords = new List<Vector2>();
			lIndices = new List<int> ();

			string name = "unamed";
			using (StreamReader Reader = new StreamReader(fileName))
			{
				System.Globalization.CultureInfo savedCulture = Thread.CurrentThread.CurrentCulture;
				Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

				string line;
				while ((line = Reader.ReadLine()) != null)
				{
					line = line.Trim(splitCharacters);
					line = line.Replace("  ", " ");

					string[] parameters = line.Split(splitCharacters);

					switch (parameters[0])
					{
					case "o":
						name = parameters[1];
						break;
					case "p": // Point
						break;
					case "v": // Vertex
						float x = float.Parse(parameters[1]);
						float y = float.Parse(parameters[2]);
						float z = float.Parse(parameters[3]);

						objPositions.Add(new Vector3(x, y, z));
						break;
					case "vt": // TexCoord
						float u = float.Parse(parameters[1]);
						float v = float.Parse(parameters[2]);
						objTexCoords.Add(new Vector2(u, v));
						break;

					case "vn": // Normal
						float nx = float.Parse(parameters[1]);
						float ny = float.Parse(parameters[2]);
						float nz = float.Parse(parameters[3]);
						objNormals.Add(new Vector3(nx, ny, nz));
						break;

					case "f":
						switch (parameters.Length)
						{
						case 4:

							lIndices.Add(ParseFaceParameter(parameters[1]));
							lIndices.Add(ParseFaceParameter(parameters[2]));
							lIndices.Add(ParseFaceParameter(parameters[3]));
							break;

						case 5:
							lIndices.Add(ParseFaceParameter(parameters[1]));
							lIndices.Add(ParseFaceParameter(parameters[2]));
							lIndices.Add(ParseFaceParameter(parameters[3]));
							lIndices.Add(ParseFaceParameter(parameters[4]));
							break;
						}
						break;

					case "usemtl":
						Debug.WriteLine ("usemtl: {0}", parameters [1]);
//						if (parameters.Length > 1)
//							name = parameters[1];
//
//						currentFaceGroup.material = model.materials.Find(
//							delegate(Material m)
//							{
//								return m.Name == name;
//							});

						break;
					case "mtllib":
						Debug.WriteLine ("usemtl: {0}", parameters [1]);
//							model.mtllib = parameters[1];
//							string mtlPath = System.IO.Path.GetDirectoryName(fileName)
//								+ System.IO.Path.DirectorySeparatorChar
//								+ model.mtllib;
//
//							if (System.IO.File.Exists(mtlPath))
//							{
//								model.materials = ObjMeshLoader.LoadMtl(mtlPath);
//
//								//mesh.materials[0].InitMaterial();
//							}
						break;
					case "#":
//						if (parameters.Length > 1)
//						{
//							if (parameters[1] == "object")
//							{
//								if (currentMesh != null)
//								{
//									if (currentFaceGroup != null)
//									{
//										currentFaceGroup.Triangles = triangles.ToArray();
//										currentFaceGroup.Quads = quads.ToArray();
//
//										currentMesh.Faces.Add(currentFaceGroup);
//									}
//
//									currentMesh.Vertices = objVertices.ToArray();
//									objVertices.Clear();
//
//									faces.Add(currentMesh);
//								}
//								currentMesh = new Mesh();
//								currentMesh.name = parameters[2];
//							}
//						}
						break;
					}
				}

//				if (currentFaceGroup != null)
//				{
//					currentFaceGroup.Triangles = triangles.ToArray();
//					currentFaceGroup.Quads = quads.ToArray();
//					currentMesh.Faces.Add(currentFaceGroup);
//				}
//				if (currentMesh != null)
//				{
//					currentMesh.Vertices = objVertices.ToArray();
//					faces.Add(currentMesh);
//				}
//				model.meshes.Add(faces.ToArray());
				Thread.CurrentThread.CurrentCulture = savedCulture;
			}

			vaoMesh tmp = new vaoMesh(lPositions.ToArray (),lTexCoords.ToArray (),
				lNormals.ToArray (),lIndices.ToArray ());

			tmp.Name = name;

			objPositions.Clear();
			objNormals.Clear();
			objTexCoords.Clear();
			lPositions.Clear();
			lNormals.Clear();
			lTexCoords.Clear();
			lIndices.Clear();

			return tmp;
		}

//		public static List<Material> LoadMtl(string fileName)
//		{
//			using (StreamReader streamReader = new StreamReader(fileName))
//			{
//				return LoadMtl(streamReader);
//			}
//		}

		static char[] splitCharacters = new char[] { ' ' };



		static int ParseFaceParameter(string faceParameter)
		{
			Vector3 vertex = new Vector3();
			Vector2 texCoord = new Vector2();
			Vector3 normal = new Vector3();

			string[] parameters = faceParameter.Split(faceParamaterSplitter);

			int vertexIndex = int.Parse(parameters[0]);
			if (vertexIndex < 0) vertexIndex = objPositions.Count + vertexIndex;
			else vertexIndex = vertexIndex - 1;
			vertex = objPositions[vertexIndex];

			if (parameters.Length > 1)
			{
				int texCoordIndex;
				if (int.TryParse(parameters[1], out texCoordIndex))
				{
					if (texCoordIndex < 0) texCoordIndex = objTexCoords.Count + texCoordIndex;
					else texCoordIndex = texCoordIndex - 1;
					texCoord = objTexCoords[texCoordIndex];
				}
			}

			if (parameters.Length > 2)
			{
				int normalIndex;
				if (int.TryParse(parameters[2], out normalIndex))
				{
					if (normalIndex < 0) normalIndex = objNormals.Count + normalIndex;
					else normalIndex = normalIndex - 1;
					normal = objNormals[normalIndex];
				}
			}


			lPositions.Add(vertex);
			lTexCoords.Add(texCoord);
			lNormals.Add(normal);


			int index = lPositions.Count-1;
			return index;

			//if (objVerticesIndexDictionary.TryGetValue(newObjVertex, out index))
			//{
			//    return index;
			//}
			//else
			//{
			//    objVertices.Add(newObjVertex);
			//    objVerticesIndexDictionary[newObjVertex] = objVertices.Count - 1;
			//    return objVertices.Count - 1;
			//}
		}
//		static List<Material> LoadMtl(TextReader textReader)
//		{
//			Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
//			List<Material> Materials = new List<Material>();
//			Material currentMat = null;
//
//			string line;
//			while ((line = textReader.ReadLine()) != null)
//			{
//				line = line.Trim(splitCharacters);
//				line = line.Replace("  ", " ");
//
//				string[] parameters = line.Split(splitCharacters);
//
//				switch (parameters[0])
//				{
//				case "newmtl":
//					if (currentMat != null)
//						Materials.Add(currentMat);
//					currentMat = new Material();
//					if (parameters.Length > 1)
//						currentMat.Name = parameters[1];
//					break;
//				case "Ka":
//					currentMat.Ambient = new Color (
//						float.Parse(parameters[1]),
//						float.Parse(parameters[2]),
//						float.Parse(parameters[3]),1.0f
//					);
//					break;
//				case "Kd":
//					currentMat.Diffuse = new Color (
//						float.Parse(parameters[1]),
//						float.Parse(parameters[2]),
//						float.Parse(parameters[3]),1.0f
//					);
//					break;
//				case "Ks":
//					currentMat.Specular = new Color (
//						float.Parse(parameters[1]),
//						float.Parse(parameters[2]),
//						float.Parse(parameters[3]),1.0f
//					);
//					break;
//				case "d":
//				case "Tr":
//					currentMat.Transparency = float.Parse(parameters[1]);
//					break;
//				case "map_Ka":
//					currentMat.AmbientMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_Kd":
//					currentMat.DiffuseMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_Ks":
//					currentMat.SpecularMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_Ns":
//					currentMat.SpecularHighlightMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_d":
//					currentMat.AlphaMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_bump":
//				case "bump":
//					currentMat.BumpMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "disp":
//					currentMat.DisplacementMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "decal":
//					currentMat.StencilDecalMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				}
//
//			}
//
//			if (currentMat != null)
//				Materials.Add(currentMat);
//
//			return Materials;
//		}
//

		static char[] faceParamaterSplitter = new char[] { '/' };

	}

}
