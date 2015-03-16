using System;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK;

namespace go.GLBackend
{
	public class Shader : IDisposable
	{
		#region CTOR
		public Shader ()
		{
			Compile ();
		}
		#endregion


		#region Sources
		protected string _vertSource = @"
			#version 130

			precision highp float;

			uniform mat4 projection_matrix;
			uniform mat4 modelview_matrix;

			in vec2 in_position;

			void main(void)
			{
				gl_Position = projection_matrix * modelview_matrix * vec4(in_position,0, 1);
			}";

		protected string _fragSource = @"
			#version 130
			precision highp float;

			uniform vec4 color;
			uniform bool stencilTest;
			uniform sampler2D stencil;
			uniform vec2 resolution;

			out vec4 out_frag_color;

			void main(void)
			{
				if (stencilTest)
				{
					vec2 uv = gl_FragCoord.xy/resolution;
					vec4 s = texture( stencil, uv);					
					if (s.r == 0.0)
						discard;
				}
				out_frag_color = color;
			}";
		string _geomSource = "";
		#endregion

		#region Private and protected fields
		protected int vsId, fsId, gsId, pgmId, savedPgmId = 0,
						modelviewMatrixLocation,
						projectionMatrixLocation,
						colorLocation,stencilTestLocation,resolutionLocation;

		Matrix4 projectionMatrix, 
				modelviewMatrix;
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

		public Matrix4 ProjectionMatrix{
			set { 
				projectionMatrix = value;
				GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);  
			}
		}
		public Matrix4 ModelViewMatrix {
			set { 
				modelviewMatrix = value;
				GL.UniformMatrix4 (modelviewMatrixLocation, false, ref modelviewMatrix); 
			}
		}

		public Vector4 Color {
			set {GL.Uniform4 (colorLocation, value);}
		}

		public bool StencilTest {
			set {
				if (value)
					GL.Uniform1 (stencilTestLocation, 1);
				else
					GL.Uniform1 (stencilTestLocation, 0);
			}
		}

		public Vector2 Resolution {
			set { GL.Uniform2 (resolutionLocation, value); }
		}

		#endregion

		#region Public functions
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

			GL.LinkProgram(pgmId);
			GL.ValidateProgram(pgmId);

			string info;
			GL.GetProgramInfoLog(pgmId, out info);
			Debug.WriteLine(info);

			Enable ();

			GetUniformLocations ();
			BindSamplesSlots ();

			Disable ();
		}
		protected virtual void BindVertexAttributes()
		{
			GL.BindAttribLocation(pgmId, 0, "in_position");
		}
		protected virtual void GetUniformLocations()
		{
			projectionMatrixLocation = GL.GetUniformLocation(pgmId, "projection_matrix");
			modelviewMatrixLocation = GL.GetUniformLocation(pgmId, "modelview_matrix");
			colorLocation = GL.GetUniformLocation (pgmId, "color");
			stencilTestLocation = GL.GetUniformLocation (pgmId, "stencilTest");
			resolutionLocation = GL.GetUniformLocation (pgmId, "resolution");
		}
		protected virtual void BindSamplesSlots(){
			GL.Uniform1(GL.GetUniformLocation (pgmId, "stencil"),0);
		}

		public virtual void Enable(){
			GL.GetInteger (GetPName.CurrentProgram, out savedPgmId);
			GL.UseProgram (pgmId);
		}
		public virtual void Disable(){
			GL.UseProgram (savedPgmId);
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

		void compileShader(int shader, string source)
		{
			GL.ShaderSource(shader, source);
			GL.CompileShader(shader);

			string info;
			GL.GetShaderInfoLog(shader, out info);
			Debug.WriteLine(info);

			int compileResult;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out compileResult);
			if (compileResult != 1)
			{
				Debug.WriteLine("Compile Error!");
				Debug.WriteLine(source);
			}
		}			
			
		#region IDisposable implementation
		public void Dispose ()
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

