using System;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK;

namespace Crow.GLBackend
{
	public class TestShader
	{
		string vertexShaderSource = @"
			#version 130

			//precision highp float;

			uniform float lw;
			uniform mat4 projection_matrix;
			uniform mat4 modelview_matrix;

			in vec2 in_position;
			in vec2 in_tex;

			out vec2 texcoord;

			void main(void)
			{
				texcoord = in_tex;
				gl_Position = projection_matrix * modelview_matrix * vec4(in_position,0, 1);
			}";

		string fragmentShaderSource = @"
			#version 130

			//precision highp float;
			uniform float lw;
			uniform vec4 color;

			in vec2 texcoord;
			out vec4 out_frag_color;

			void main(void)
			{
//				float smoothWidth = 1.0 / lineWidth;
//
//				if (texcoord.y < smoothWidth)			  					
//					out_frag_color = vec4(color,texcoord.y/smoothWidth);
//				else if (texcoord.y > 1.0 - smoothWidth)
//					out_frag_color = vec4(color,(1.0-texcoord.y)/smoothWidth);
//				else
//					out_frag_color = vec4(color,1.0);
				out_frag_color = color;
			}";

		int vertexShaderHandle,
			fragmentShaderHandle,
			shaderProgramHandle,
			modelviewMatrixLocation,
			projectionMatrixLocation,
			colorLocation,
			lineWidthLocation;

		Matrix4 projectionMatrix, 
				modelviewMatrix;

		double lineWidth = 1.0;

		public TestShader ()
		{
			vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
			fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);

			GL.ShaderSource(vertexShaderHandle, vertexShaderSource);
			GL.ShaderSource(fragmentShaderHandle, fragmentShaderSource);

			GL.CompileShader(vertexShaderHandle);
			GL.CompileShader(fragmentShaderHandle);

			Debug.WriteLine(GL.GetShaderInfoLog(vertexShaderHandle));
			Debug.WriteLine(GL.GetShaderInfoLog(fragmentShaderHandle));

			// Create program
			shaderProgramHandle = GL.CreateProgram();

			GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
			GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);

			GL.BindAttribLocation(shaderProgramHandle, 0, "in_position");
			GL.BindAttribLocation(shaderProgramHandle, 1, "in_tex");

			GL.LinkProgram(shaderProgramHandle);
			Debug.WriteLine(GL.GetProgramInfoLog(shaderProgramHandle));
			GL.UseProgram(shaderProgramHandle);


			lineWidthLocation = GL.GetUniformLocation (shaderProgramHandle, "lw");
			projectionMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "projection_matrix");
			modelviewMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "modelview_matrix");
			colorLocation = GL.GetUniformLocation (shaderProgramHandle, "color");

		}

		int savedPgm = 0;
		public virtual void Enable(){
			GL.GetInteger (GetPName.CurrentProgram, out savedPgm);
			GL.UseProgram (shaderProgramHandle);
		}
		public virtual void Disable(){
			GL.UseProgram (savedPgm);
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
		public double LineWidth {
			set {
				lineWidth = value;
				GL.Uniform1 (lineWidthLocation, (float)lineWidth);
			}
		}
		public Vector4 Color {
			set {
				GL.Uniform4 (colorLocation, value);
			}
		}
	}
}

