using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using RazorEngine;
using RazorEngine.Compilation.ImpromptuInterface.InvokeExt;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace Gos.Aspnet.RazorPages.Middlewares
{
    public class RazorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _env;

        public RazorMiddleware(RequestDelegate next, IHostingEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            var fileInfo = _env.WebRootFileProvider.GetFileInfo(context.Request.Path.Value);

            if (fileInfo.Exists && CanHandle(fileInfo))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";

                var dynamicViewBag = new DynamicViewBag();
                dynamicViewBag.AddValue("IsHttps", context.Request.IsHttps);

                var config = new TemplateServiceConfiguration();
                // .. configure your instance

                var service = RazorEngineService.Create(config);
                service.WithContext(new RazorPageContext {HttpContext = context });
                Engine.Razor = service;

                string result;
                if (Engine.Razor.IsTemplateCached(fileInfo.PhysicalPath, null))
                {
                    result = Engine.Razor.Run(fileInfo.PhysicalPath, null, null, dynamicViewBag);
                }
                else
                {
                    using (var stream = fileInfo.CreateReadStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var razor = await reader.ReadToEndAsync();
                            result = Engine.Razor.RunCompile(razor, fileInfo.PhysicalPath, null, null, dynamicViewBag);
                        }

                    }
                }

                await context.Response.WriteAsync(result);

                return;
            }

            await _next(context);
        }

        private bool CanHandle(IFileInfo fileInfo)
        {
            return fileInfo.Name.EndsWith(".cshtml");
        }
    }

    public class RazorPageContext
    {
        public HttpContext HttpContext { get; set; }
    }
}
