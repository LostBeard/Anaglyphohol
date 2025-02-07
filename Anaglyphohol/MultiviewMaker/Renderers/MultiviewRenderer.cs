using Microsoft.JSInterop;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS;

namespace Anaglyphohol.MultiviewMaker.Renderers
{

    // below is polyfill until next release of blazorjs
    public class WebGLContextEvent : Event
    {
        /// <summary>
        /// Deserialization constructor
        /// </summary>
        /// <param name="_ref"></param>
        public WebGLContextEvent(IJSInProcessObjectReference _ref) : base(_ref) { }
        /// <summary>
        /// A read-only property containing additional information about the event.
        /// </summary>
        public string StatusMessage => JSRef!.Get<string>("statusMessage");
    }
    public abstract class MultiviewRenderer : IDisposable
    {
        public OffscreenCanvas? OffscreenCanvas { get; private set; }
        public HTMLCanvasElement? Canvas { get; private set; }
        public WebGLRenderingContext gl { get; private set; }
        public WebGLProgram program { get; protected set; }
        public float Level3D { get; set; } = 1f;
        public float sepMax { get; set; } = 0.020f;
        public float Focus3D { get; set; } = 0.5f;
        protected MultiviewRenderer()
        {
            //Console.WriteLine("MultiviewRenderer using offscreencanvas");
            OffscreenCanvas = new OffscreenCanvas(1, 1);
            gl = OffscreenCanvas.GetWebGLContext(new WebGLContextAttributes
            {
                PreserveDrawingBuffer = true,
            });
            // classes that implement this class will create he shader program
            OffscreenCanvas.AddEventListener<WebGLContextEvent>("webglcontextlost", Canvas_OnContextLost);
        }
        protected MultiviewRenderer(HTMLCanvasElement canvas)
        {
            //Console.WriteLine("MultiviewRenderer using canvas");
            this.Canvas = canvas;
            gl = canvas.GetWebGLContext(new WebGLContextAttributes
            {
                PreserveDrawingBuffer = true,
            });
            // classes that implement this class will create he shader program
            canvas.AddEventListener<WebGLContextEvent>("webglcontextlost", Canvas_OnContextLost);
        }
        // Use WebGLContextEvent instead of event
        void Canvas_OnContextLost(WebGLContextEvent ev)
        {
            Console.WriteLine("Canvas_OnContextLost: " + ev.StatusMessage);
        }
        void Canvas_OnContextRestored()
        {
            Console.WriteLine("Canvas_OnContextRestored");
        }
        public void Dispose()
        {
            if (OffscreenCanvas != null)
            {
                OffscreenCanvas.RemoveEventListener<WebGLContextEvent>("webglcontextlost", Canvas_OnContextLost);
                OffscreenCanvas.Dispose();
                OffscreenCanvas = null;
            }
            if (Canvas != null)
            {
                Canvas.RemoveEventListener<WebGLContextEvent>("webglcontextlost", Canvas_OnContextLost);
                Canvas.Dispose();
                Canvas = null;
            }
        }
        public virtual void ApplyEffect()
        {

        }
        WebGLBuffer? positionBuffer = null;
        WebGLBuffer? texcoordBuffer = null;
        int positionLocation = 0;
        int texcoordLocation = 0;
        void Init(int outWidth, int outHeight)
        {
            // look up where the vertex data needs to go.
            positionLocation = gl.GetAttribLocation(program, "a_position");
            texcoordLocation = gl.GetAttribLocation(program, "a_texCoord");

            // Create a buffer to put three 2d clip space points in
            positionBuffer ??= gl.CreateBuffer();

            // Bind it to ARRAY_BUFFER (think of it as ARRAY_BUFFER = positionBuffer)
            gl.BindBuffer(gl.ARRAY_BUFFER, positionBuffer);
            // Set a rectangle the same size as the image.
            WebGLUtilities.SetRectangle(gl, 0, 0, outWidth, outHeight);

            // provide texture coordinates for the rectangle.
            if (texcoordBuffer == null)
            {
                texcoordBuffer = gl.CreateBuffer();
                gl.BindBuffer(gl.ARRAY_BUFFER, texcoordBuffer);
                using var texCoordBuffer = new Float32Array([
                    0.0f,  0.0f,
                    1.0f,  0.0f,
                    0.0f,  1.0f,
                    0.0f,  1.0f,
                    1.0f,  0.0f,
                    1.0f,  1.0f,
                ]);
                gl.BufferData(gl.ARRAY_BUFFER, texCoordBuffer, gl.STATIC_DRAW);
            }

            // input texture
            videoSampler ??= CreateImageTexture();

            // overlay texture
            overlayTexture ??= CreateImageTexture();
        }
        public async Task<Blob?> ToBlob(string? type = null, float? quality = null)
        {
            if (string.IsNullOrEmpty(type))
            {
                type = "image/png";
            }
            if (Canvas != null)
            {
                if (quality != null)
                {
                    var blob = await Canvas.ToBlobAsync(type, quality.Value);
                    return blob;
                }
                else
                {
                    var blob = await Canvas.ToBlobAsync(type);
                    return blob;
                }
            }
            else if (OffscreenCanvas != null)
            {
                var blob = await OffscreenCanvas.ConvertToBlob(new ConvertToBlobOptions
                {
                    Type = type,
                    Quality = quality,
                });
                return blob;
            }
            return null;
        }
        public async Task<string?> ToObjectUrl(string? type = null, float? quality = null)
        {
            using var blob = await ToBlob(type, quality);
            var objectUrl = blob == null ? null : URL.CreateObjectURL(blob);
            return objectUrl;
        }
        public bool AutoSize { get; set; } = true;
        public int OutWidth
        {
            get => Canvas?.Width ?? OffscreenCanvas?.Width ?? 0;
            set
            {
                if (Canvas != null)
                {
                    Canvas.Width = value;
                }
                else if (OffscreenCanvas != null)
                {
                    OffscreenCanvas.Width = value;
                }
            }
        }
        public int OutHeight
        {
            get => Canvas?.Height ?? OffscreenCanvas?.Height ?? 0;
            set
            {
                if (Canvas != null)
                {
                    Canvas.Height = value;
                }
                else if (OffscreenCanvas != null)
                {
                    OffscreenCanvas.Height = value;
                }
            }
        }
        public void SetOutputSize(int width, int height)
        {
            if (Canvas != null)
            {
                Canvas.Width = width;
                Canvas.Height = height;
            }
            else if (OffscreenCanvas != null)
            {
                OffscreenCanvas.Width = width;
                OffscreenCanvas.Height = height;
            }
        }

        WebGLTexture? videoSampler = null;
        WebGLTexture? overlayTexture = null;

        string _InFormat = "2d";
        public string InFormat
        {
            get => _InFormat;
            set
            {
                _InFormat = value;
            }
        }
        public int FrameWidth { get; private set; }
        public int FrameHeight { get; private set; }
        public int OverlayWidth { get; private set; }
        public int OverlayHeight { get; private set; }
        public string? Source { get; private set; }
        public void SetInput(HTMLImageElement image, string inFormat)
        {
            if (string.IsNullOrEmpty(inFormat)) inFormat = "2d";
            if (Source == image.Src && InFormat == inFormat) return;
            Source = image.Src;
            if (FrameWidth != image.NaturalWidth || FrameHeight != image.NaturalHeight)
            {
                FrameWidth = image.NaturalWidth;
                FrameHeight = image.NaturalHeight;
                // input size changed
            }
            InFormat = inFormat;
            videoSampler ??= CreateImageTexture();
            // Upload the image into the texture.
            gl.ActiveTexture(gl.TEXTURE1);
            gl.BindTexture(gl.TEXTURE_2D, videoSampler);
            gl.TexImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
        }
        public void SetOverlay(HTMLImageElement image)
        {
            if (OverlayWidth != image.NaturalWidth || OverlayHeight != image.NaturalHeight)
            {
                OverlayWidth = image.NaturalWidth;
                OverlayHeight = image.NaturalHeight;
                // overlay size changed
            }
            overlayTexture ??= CreateImageTexture();
            // Upload the image into the texture.
            gl.ActiveTexture(gl.TEXTURE0);
            gl.BindTexture(gl.TEXTURE_2D, overlayTexture);
            gl.TexImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
        }
        public void Render()
        {
            int frameCols = 1;
            int frameRows = 1;
            bool views_index_invert = false;
            var primaryViewIndex = 0;
            var inputLayout = 0; // 0 = tiled views, 1 = 2D+Z
            switch (InFormat)
            {
                case "2dz":
                    frameCols = 2;
                    inputLayout = 1;
                    break;
                case "2d":
                default:
                    break;
            }
            var viewWidth = (int)Math.Round((float)FrameWidth / (float)frameCols);
            var viewHeight = (int)Math.Round((float)FrameHeight / (float)frameRows);
            var outHeight = 0;
            var outWidth = 0;
            if (AutoSize)
            {
                outWidth = viewWidth;
                outHeight = viewHeight;
                SetOutputSize(outWidth, outHeight);
            }
            else
            {
                outWidth = OutWidth;
                outHeight = OutHeight;
            }
            Init(outWidth, outHeight);
            var OutAspectRatio = (float)outHeight / (float)outWidth;
            var views_in_cnt = frameCols * frameRows;
            float viewAspectRatio = (float)viewHeight / (float)viewWidth;
            //if (frameCols == frameRows * 2f)
            //{
            //    // check for half width views
            //    if (viewAspectRatio > 0.8)
            //    {
            //        // appears to be half width so double the view width
            //        viewWidth *= 2;
            //        viewAspectRatio = (float)viewHeight / (float)viewWidth;
            //    }
            //}
            //else if (frameCols * 2f == frameRows)
            //{
            //    // check for half height views
            //    if (viewWidth > viewHeight * 2f)
            //    {
            //        viewHeight *= 2;
            //        viewAspectRatio = (float)viewHeight / (float)viewWidth;
            //    }
            //}
            var videoScaleU = 1f;
            var videoScaleV = 1f;
            var videoPaddingU = 0f;
            var videoPaddingV = 0f;
            if (viewAspectRatio > OutAspectRatio)
            {
                float scaleY = (float)viewHeight / (float)outHeight;
                float scaledWidth = (float)viewWidth / scaleY;
                videoScaleU = (float)outWidth / scaledWidth;
                videoPaddingU = (videoScaleU - 1f) / 2f;
            }
            else if (viewAspectRatio < OutAspectRatio)
            {
                float scaleX = (float)viewWidth / (float)outWidth;
                float scaledHeight = (float)viewHeight / scaleX;
                videoScaleV = (float)outHeight / scaledHeight;
                videoPaddingV = (videoScaleV - 1f) / 2f;
            }

            gl.UseProgram(program);

            //// overlay texture
            //uniform sampler2D textureSampler;
            // set textureSampler to use TEXTURE0
            Uniform1i("textureSampler", 0);
            // set TEXTURE0 active
            gl.ActiveTexture(gl.TEXTURE0);
            // attach overlayTexture texture to the active texture (gl.TEXTURE0 here)
            gl.BindTexture(gl.TEXTURE_2D, overlayTexture);

            //// 1 or more 2D views tiled, or 2D+Z
            //uniform sampler2D videoSampler;
            ////uniform sampler2D ui2Sampler;	// only needed if mouse needs its own texture
            // set videoSampler to use TEXTURE0
            Uniform1i("videoSampler", 1);
            // set TEXTURE1 active
            gl.ActiveTexture(gl.TEXTURE1);
            // attach videoSampler texture to the active texture (gl.TEXTURE1 here)
            gl.BindTexture(gl.TEXTURE_2D, videoSampler);

            //uniform vec2 screenSize;
            Uniform2f("screenSize", outWidth, outHeight);

            //uniform int inputLayout;    // 0 = tiled views, 1 = 2D+Z, 2 = 2D+ZD
            Uniform1i("inputLayout", inputLayout);

            // tiled input info (used by all input even single view)
            //uniform vec2 cols_rows_in; // = vec2(4.0, 2.0);
            Uniform2f("cols_rows_in", frameCols, frameRows);

            //uniform bool views_index_invert_x; // false 0x is left, true 0x is right
            Uniform1i("views_index_invert_x", views_index_invert ? 1 : 0);

            //uniform bool views_index_invert_y; // false 0y is top, true 0y is bottom
            Uniform1i("views_index_invert_y", views_index_invert ? 1 : 0);

            //uniform float views_in_cnt; // = cols_rows_in.x * cols_rows_in.y;
            Uniform1f("views_in_cnt", views_in_cnt);

            //uniform float views_in_max_index; // = views_in_cnt - 1.0;
            Uniform1f("views_in_max_index", views_in_cnt - 1);

            //uniform float primaryViewIndex;
            Uniform1f("primaryViewIndex", primaryViewIndex);

            //uniform vec2 view_size_in; // = 1.0 / cols_rows_in;
            Uniform2f("view_size_in", 1f / (float)frameCols, 1f / (float)frameRows);

            if (inputLayout == 1)
            {
                // if 2d+z below 2 uniforms must be set
                var outPixelWidth = 1.0f / (float)outWidth;
                // handle extra data needed for 2dz and 2dzd
                var sep_max_x = sepMax * Level3D; // (RenderManager.settings["3d_level_global"].value);
                var sep_max_modifier = 900f / (float)viewWidth;
                sep_max_x = sep_max_x * sep_max_modifier;
                //sep_max_x = sep_max_x - (sep_max_x % rC0[1]);
                int loop_cnt = (int)Math.Ceiling(sep_max_x / outPixelWidth) + 2;
                //uniform float rC0[4];
                Uniform1fv("rC0", [sep_max_x, outPixelWidth, outPixelWidth * 0.5f, Focus3D]);
                //uniform int rI0[1];
                Uniform1iv("rI0", [loop_cnt]);
            }

            //uniform vec2 uv_scale; // = vec2(1.0, 1.2);
            Uniform2f("uv_scale", videoScaleU, videoScaleV);

            //uniform vec2 uv_padding; // = vec2(0.0, 0.1);
            Uniform2f("uv_padding", videoPaddingU, videoPaddingV);

            // now apply implementing renderer's settings
            ApplyEffect();

            // Tell WebGL how to convert from clip space to pixels
            gl.Viewport(0, 0, outWidth, outHeight);

            // Clear the render target
            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(gl.COLOR_BUFFER_BIT);

            // Tell it to use our program (pair of shaders)
            gl.UseProgram(program);

            {
                // Turn on the position attribute
                gl.EnableVertexAttribArray(positionLocation);

                // Bind the position buffer.
                gl.BindBuffer(gl.ARRAY_BUFFER, positionBuffer);
                // Tell the position attribute how to get data out of positionBuffer (ARRAY_BUFFER)
                var size = 2;          // 2 components per iteration
                var type = gl.FLOAT;   // the data is 32bit floats
                var normalize = false; // don't normalize the data
                var stride = 0;        // 0 = move forward size * sizeof(type) each iteration to get the next position
                var offset = 0;        // start at the beginning of the buffer
                gl.VertexAttribPointer(positionLocation, size, type, normalize, stride, offset);
            }

            {
                // Turn on the texcoord attribute
                gl.EnableVertexAttribArray(texcoordLocation);

                // bind the texcoord buffer.
                gl.BindBuffer(gl.ARRAY_BUFFER, texcoordBuffer);

                // Tell the texcoord attribute how to get data out of texcoordBuffer (ARRAY_BUFFER)
                var size = 2;          // 2 components per iteration
                var type = gl.FLOAT;   // the data is 32bit floats
                var normalize = false; // don't normalize the data
                var stride = 0;        // 0 = move forward size * sizeof(type) each iteration to get the next position
                var offset = 0;        // start at the beginning of the buffer
                gl.VertexAttribPointer(texcoordLocation, size, type, normalize, stride, offset);
            }

            {
                // Draw the rectangle.
                var primitiveType = gl.TRIANGLES;
                var offset = 0;
                var count = 6;
                gl.DrawArrays(primitiveType, offset, count);
                gl.Flush();
            }
        }
        public WebGLTexture CreateImageTexture(HTMLImageElement image)
        {
            var texture = gl.CreateTexture();
            gl.BindTexture(gl.TEXTURE_2D, texture);
            // Set the parameters so we can render any size image.
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
            // Upload the image into the texture.
            gl.TexImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
            return texture;
        }
        public WebGLTexture CreateImageTexture()
        {
            var texture = gl.CreateTexture();
            gl.BindTexture(gl.TEXTURE_2D, texture);
            // Set the parameters so we can render any size image.
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
            return texture;
        }
        Dictionary<string, WebGLUniformLocation?> _uniforms = new Dictionary<string, WebGLUniformLocation?>();
        WebGLUniformLocation? GetUniformLocation(string name)
        {
            if (_uniforms.TryGetValue(name, out var uniform)) return uniform;
            uniform = gl.GetUniformLocation(program, name);
            _uniforms.Add(name, uniform);
            return uniform;
        }
        protected bool Uniform2f(string name, float x, float y)
        {
            var uniformLocation = GetUniformLocation(name);
            if (uniformLocation == null) return false;
            gl.Uniform2f(uniformLocation, x, y);
            return true;
        }
        protected bool Uniform1f(string name, float x)
        {
            var uniformLocation = GetUniformLocation(name);
            if (uniformLocation == null) return false;
            gl.Uniform1f(uniformLocation, x);
            return true;
        }
        protected bool Uniform1fv(string name, IEnumerable<float> v)
        {
            var uniformLocation = GetUniformLocation(name);
            if (uniformLocation == null) return false;
            gl.Uniform1fv(uniformLocation, v);
            return true;
        }
        protected bool Uniform1iv(string name, IEnumerable<int> v)
        {
            var uniformLocation = GetUniformLocation(name);
            if (uniformLocation == null) return false;
            gl.Uniform1iv(uniformLocation, v);
            return true;
        }
        protected bool Uniform1i(string name, int x)
        {
            var uniformLocation = GetUniformLocation(name);
            if (uniformLocation == null) return false;
            gl.Uniform1i(uniformLocation, x);
            return true;
        }
        protected WebGLProgram CreateProgram(string vertexShader, string fragmentShader)
        {
            //vertex shader
            using var vsShader = gl.CreateShader(gl.VERTEX_SHADER);
            gl.ShaderSource(vsShader, vertexShader);
            gl.CompileShader(vsShader);
            var vsShaderSucc = gl.GetShaderParameter<bool>(vsShader, gl.COMPILE_STATUS);
            if (!vsShaderSucc)
            {
                var compilationLog = gl.GetShaderInfoLog(vsShader);
                gl.DeleteShader(vsShader);
                throw new Exception($"Error compile vertex shader for WebGLProgram. {compilationLog}");
            }
            // fragment shader
            using var psShader = gl.CreateShader(gl.FRAGMENT_SHADER);
            gl.ShaderSource(psShader, fragmentShader);
            gl.CompileShader(psShader);
            var psShaderSucc = gl.GetShaderParameter<bool>(psShader, gl.COMPILE_STATUS);
            if (!psShaderSucc)
            {
                var compilationLog = gl.GetShaderInfoLog(psShader);
                gl.DeleteShader(vsShader);
                gl.DeleteShader(psShader);
                throw new Exception($"Error compile fragment shader for WebGLProgram. {compilationLog}");
            }
            // program
            var program = gl.CreateProgram();
            gl.AttachShader(program, vsShader);
            gl.AttachShader(program, psShader);
            gl.LinkProgram(program);
            var programSucc = gl.GetProgramParameter<bool>(program, gl.LINK_STATUS);
            gl.DeleteShader(vsShader);
            gl.DeleteShader(psShader);
            if (programSucc) return program;
            var lastError = gl.GetProgramInfoLog(program);
            Console.WriteLine("Error in program linking:" + lastError);
            gl.DeleteProgram(program);
            program.Dispose();
            throw new Exception("Error creating shader program for WebGLProgram");
        }
    }
}
