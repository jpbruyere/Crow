using System;
using OpenTK.Graphics.OpenGL;

namespace go.GLBackend
{
	public class TexturedShader : Shader
	{
		public TexturedShader ()
		{
			vertSource = @"
				#version 130

				precision highp float;

				uniform mat4 Projection;
				uniform mat4 ModelView;
				uniform mat4 Model;
				uniform mat4 Normal;

				in vec2 in_position;
				in vec2 in_tex;

				out vec2 texCoord;
				

				void main(void)
				{
					texCoord = in_tex;
					gl_Position = Projection * ModelView * Model * vec4(in_position, 0, 1);
				}";
			fragSource = @"
				#version 130
				precision highp float;

				uniform sampler2D tex;

				in vec2 texCoord;
				out vec4 out_frag_color;

				void main(void)
				{
					out_frag_color = texture( tex, texCoord);
				}";

			Compile ();

		}

		protected override void BindSamplesSlots ()
		{
			base.BindSamplesSlots ();

			GL.Uniform1(GL.GetUniformLocation (pgmId, "stencil"),1);
		}
		public override void Enable ()
		{
			base.Enable ();

		}
	}
}

