using iText.Html2pdf;
using iText.Html2pdf.Resolver.Font;
using iText.IO.Source;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Html2Pdf.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConvertController : ControllerBase
    {
        private readonly Dictionary<string, string> _clientKeys;

        public ConvertController(IConfiguration configuration)
        {
            _clientKeys = new Dictionary<string, string>();
            foreach (var confParam in configuration.AsEnumerable())
            {
                if (confParam.Key.EndsWith("ApiKey"))
                {
                    _clientKeys.Add(confParam.Key.Substring(0, confParam.Key.IndexOf("ApiKey")), confParam.Value);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Convert()
        {
            StringValues clientParam;
            StringValues keyParam;
            var hasClient = Request.Query.TryGetValue("client", out clientParam);
            var hasKey = Request.Query.TryGetValue("key", out keyParam);
            var client = hasClient && clientParam.Count > 0 ? clientParam[0] : "";
            var key = hasKey && keyParam.Count > 0 ? keyParam[0] : "";

            if (!_clientKeys.ContainsKey(client) || _clientKeys[client] != key)
            {
                return new NotFoundResult();
            }

            var formData = HttpContext.Request.Form;
            var files = formData.Files;
            var docFile = files.Where(f => f.FileName == "doc.html").FirstOrDefault();

            IActionResult response = null;

            if (docFile != null)
            {
                var tempFolder = $"{System.IO.Path.GetTempPath()}{Guid.NewGuid()}";
                Directory.CreateDirectory(tempFolder);

                foreach (var file in files)
                {
                    if (file.FileName != "doc.html")
                        await System.IO.File.WriteAllBytesAsync($"{tempFolder}/{file.FileName}", ReadAllBytes(file.OpenReadStream()));
                }

                try
                {

                    using (var htmlSource = docFile.OpenReadStream())
                    using (var pdfDest = new ByteArrayOutputStream())
                    {
                        var writer = new PdfWriter(pdfDest);
                        var pdfDoc = new PdfDocument(writer);
                        pdfDoc.SetTagged();
                        pdfDoc.SetDefaultPageSize(PageSize.A4);
                    
                        var converterProperties = new ConverterProperties();
                        
                        var fp = new DefaultFontProvider();
                        fp.AddDirectory(tempFolder);
                        converterProperties.SetFontProvider(fp);
                        
                        converterProperties.SetImmediateFlush(true);
                        converterProperties.SetBaseUri(new Uri(tempFolder).AbsoluteUri);
                        HtmlConverter.ConvertToPdf(htmlSource, pdfDoc, converterProperties);
                        var bytes = pdfDest.ToArray();
                        response = new FileContentResult(bytes, "application/pdf");
                    }
                }
                catch (Exception ex)
                {
                    response = Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
                }

                Directory.Delete(tempFolder, true);

            } else
            {
                response = Ok(new { files = files.Select(f => new { name = f.Name, fileName = f.FileName }) });
            }

            return response;
        }

        private static byte[] ReadAllBytes(Stream str)
        {
            using (var memoryStream = new MemoryStream())
            {
                str.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
    
}
