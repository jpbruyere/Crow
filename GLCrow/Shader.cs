using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Crow;
using Pencil.Gaming.Graphics;
using Pencil.Gaming.MathUtils;

namespace Tetra
{
	public class Shader : IDisposable
	{
		static List<Shader> registeredShaders = new List<Shader>();

		public static List<Shader> RegisteredShaders {
			get { return registeredShaders; }
		}

		#region CTOR
		public Shader ()
		{
			Init ();
		}
		public Shader (string vertResPath, string fragResPath = null, string geomResPath = null)
		{
			VertSourcePath = vertResPath;
			FragSourcePath = fragResPath;
			GeomSourcePath = geomResPath;

			loadSourcesFiles ();

			Init ();
		}
		~Shader(){
			RegisteredShaders.Remove (this);
		}
		#endregion

		public string	VertSourcePath,
						FragSourcePath,
						GeomSourcePath;
		#region Sources
		protected string _vertSource = @"
			#version 330
			precision lowp float;

			uniform mat4 mvp;

			layout(location = 0) in vec3 in_position;
			layout(location = 1) in vec2 in_tex;

			out vec2 texCoord;

			void main(void)
			{
				texCoord = in_tex;
				gl_Position = mvp * vec4(in_position, 1.0);
			}";

		protected string _fragSource = @"
			#version 330
			precision lowp float;

			uniform sampler2D tex;

			in vec2 texCoord;
			out vec4 out_frag_color;

			void main(void)
			{
				out_frag_color = texture( tex, texCoord);
			}";
		string _geomSource = @"";
//			#version 330
//			layout(triangles) in;
//			layout(triangle_strip, max_vertices=3) out;
//			void main()
//			{
//				for(int i=0; i<3; i++)
//				{
//					gl_Position = gl_in[i].gl_Position;
//					EmitVertex();
//				}
//				EndPrimitive();
//			}";
		#endregion

		#region Private and protected fields
		public uint vsId, fsId, gsId, pgmId;
		public int mvpLocation;

		Matrix mvp = Matrix.Identity;
		#endregion


		#region Public properties
		public virtual string vertSource
		{
			get { return _vertSource;}
			set { _vertSource = value; }
		}
		public virtual string fragSource
		{
			get { return _fragSource;}
			set { _fragSource = value; }
		}
		public virtual string geomSource
		{
			get { return _geomSource; }
			set { _geomSource = value; }
		}

		public virtual Matrix MVP{
			set { mvp = value; }
			get { return mvp; }
		}
		#endregion

		#region Public functions
		/// <summary>
		/// configure sources and compile
		/// </summary>
		public virtual void Init()
		{
			RegisteredShaders.Add (this);			
			Compile ();
		}
		public void Reload(){
			loadSourcesFiles ();
			Compile ();
		}
		public void SetSource(ShaderType shaderType, string _source){
			switch (shaderType) {
			case ShaderType.FragmentShader:
				fragSource = _source;
				return;
			case ShaderType.VertexShader:
				vertSource = _source;
				return;
			case ShaderType.GeometryShader:
				geomSource = _source;
				return;
			}
		}
		public string GetSource(ShaderType shaderType){
			switch (shaderType) {
			case ShaderType.FragmentShader:
				return fragSource;
			case ShaderType.VertexShader:
				return vertSource;
			case ShaderType.GeometryShader:
				return geomSource;
			}
			return "";
		}
		public string GetSourcePath(ShaderType shaderType){
			switch (shaderType) {
			case ShaderType.FragmentShader:
				return FragSourcePath;
			case ShaderType.VertexShader:
				return VertSourcePath;
			case ShaderType.GeometryShader:
				return GeomSourcePath;
			}
			return "";
		}
		public virtual void Compile()
		{
			Dispose ();

			pgmId = GL.CreateProgram();

			if (!string.IsNullOrEmpty(vertSource))
			{
				vsId = GL.CreateShader(ShaderType.VertexShader);
				compileShader(vsId, vertSource);
			}
			if (!string.IsNullOrEmpty(fragSource))
			{
				fsId = GL.CreateShader(ShaderType.FragmentShader);
				compileShader(fsId, fragSource);

			}
			if (!string.IsNullOrEmpty(geomSource))
			{
				gsId = GL.CreateShader(ShaderType.GeometryShader);
				compileShader(gsId,geomSource);
			}

			if (vsId != 0)
				GL.AttachShader(pgmId, vsId);
			if (fsId != 0)
				GL.AttachShader(pgmId, fsId);
			if (gsId != 0)
				GL.AttachShader(pgmId, gsId);

			BindVertexAttributes ();

			string info;
			GL.LinkProgram(pgmId);
			GL.GetProgramInfoLog((int)pgmId, out info);

			if (!string.IsNullOrEmpty (info)) {
				Debug.WriteLine ("Linkage:");
				Debug.WriteLine (info);
			}

			info = null;

			GL.ValidateProgram(pgmId);
			GL.GetProgramInfoLog((int)pgmId, out info);
			if (!string.IsNullOrEmpty (info)) {
				Debug.WriteLine ("Validation:");
				Debug.WriteLine (info);
			}

			GL.UseProgram (pgmId);

			GetUniformLocations ();
			BindSamplesSlots ();

			Disable ();
		}

		protected virtual void BindVertexAttributes()
		{
			GL.BindAttribLocation(pgmId, 0, "in_position");
			GL.BindAttribLocation(pgmId, 1, "in_tex");
			GL.BindAttribLocation(pgmId, 4, "in_model");

		}
		protected virtual void GetUniformLocations()
		{
			mvpLocation = GL.GetUniformLocation(pgmId, "mvp");
		}
		protected virtual void BindSamplesSlots(){
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"), 0);
		}
		public void SetMVP(Matrix _mvp){
			GL.UniformMatrix4(mvpLocation, false, ref _mvp);
		}
		public virtual void Enable(){
			GL.UseProgram (pgmId);
		}
		public virtual void Disable(){
			GL.UseProgram (0);
		}
		public static void Enable(Shader s)
		{
			if (s == null)
				return;
			s.Enable ();
		}
		public static void Disable(Shader s)
		{
			if (s == null)
				return;
			s.Disable ();
		}
		#endregion

		void loadSourcesFiles(){
			Stream s;

			if (!string.IsNullOrEmpty (VertSourcePath)) {
				s = Interface.GetStreamFromPath (VertSourcePath);
				if (s != null) {
					using (StreamReader sr = new StreamReader (s)) {
						vertSource = sr.ReadToEnd ();
					}
				}
			}

			if (!string.IsNullOrEmpty (FragSourcePath)) {
				s = Interface.GetStreamFromPath (FragSourcePath);
				if (s != null) {
					using (StreamReader sr = new StreamReader (s)) {
						fragSource = sr.ReadToEnd ();
					}
				}
			}

			if (!string.IsNullOrEmpty (GeomSourcePath)) {
				s = Interface.GetStreamFromPath (GeomSourcePath);
				if (s != null) {
					using (StreamReader sr = new StreamReader (s)) {
						geomSource = sr.ReadToEnd ();
					}
				}
			}			
		}
		void compileShader(uint shader, string source)
		{
			GL.ShaderSource(shader, source);
			GL.CompileShader(shader);

			string info;
			GL.GetShaderInfoLog((int)shader, out info);
			Debug.WriteLine(info);

			int compileResult;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out compileResult);
			if (compileResult != 1)
			{
				Debug.WriteLine("Compile Error!");
				Debug.WriteLine(source);
			}
		}
		public override string ToString ()
		{
			return string.Format ("{0} {1} {2}", VertSourcePath, FragSourcePath, GeomSourcePath);
		}
		#region IDisposable implementation
		public virtual void Dispose ()
		{			
			if (GL.IsProgram (pgmId))
				GL.DeleteProgram (pgmId);

			if (GL.IsShader (vsId))
				GL.DeleteShader (vsId);
			if (GL.IsShader (fsId))
				GL.DeleteShader (fsId);
			if (GL.IsShader (gsId))
				GL.DeleteShader (gsId);
		}
		#endregion
	}
}

