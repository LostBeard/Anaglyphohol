using SpawnDev.BlazorJS.JSObjects;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Anaglyphohol.MultiviewMaker.Renderers
{
    public static class EmbeddedShaderLoader
    {
        static Regex IncludeRegex = new Regex(@"#include<(.+?)>", RegexOptions.Multiline | RegexOptions.Compiled);

        public static string? GetShaderString(string resourceName, bool useIncludes = true, Assembly? assembly = null)
        {
            try
            {
                assembly ??= Assembly.GetCallingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();
                var resourceMatch = resourceNames.FirstOrDefault(name => name.EndsWith(resourceName));
                if (resourceMatch == null)
                {
                    return null;
                }
                using var stream = assembly.GetManifestResourceStream(resourceMatch)!;
                using var reader = new StreamReader(stream);
                var ret = reader.ReadToEnd();
                if (useIncludes)
                {
                    // #include<multiviewBase>
                    var matches = IncludeRegex.Matches(ret);
                    foreach (Match match in matches)
                    {
                        var includeName = match.Groups[1].Value;
                        var includeShader = GetShaderString(includeName, true, assembly);
                        if (includeShader != null)
                        {
                            ret = ret.Replace(match.Value, includeShader);
                        }
                    }
                }
                return ret;
            }
            catch
            {
                return null;
            }
        }
        public static WebGLShader? CreateShader(WebGLRenderingContext gl, string resourceName, int shaderType, bool useIncludes = true, Assembly? assembly = null)
        {
            var shaderSource = EmbeddedShaderLoader.GetShaderString(resourceName, useIncludes, assembly);
            if (shaderSource == null)
            {
                return null;
            }
            var shader = gl.CreateShader(shaderType);
            gl.ShaderSource(shader, shaderSource);
            gl.CompileShader(shader);
            var compiled = gl.GetShaderParameter<bool>(shader, gl.COMPILE_STATUS);
            if (!compiled)
            {
                var lastError = gl.GetShaderInfoLog(shader);
                Console.WriteLine("Error compiling shader '" + shader + "':" + lastError);
                gl.DeleteShader(shader);
                return null;
            }
            return shader;
        }
    }
}
