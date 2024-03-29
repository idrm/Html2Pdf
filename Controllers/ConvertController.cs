﻿using iText.Html2pdf;
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
using System.Net;
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
            StringValues orientationParam;
            StringValues pageSizeParam;
            var hasClient = Request.Query.TryGetValue("client", out clientParam);
            var hasKey = Request.Query.TryGetValue("key", out keyParam);
            var hasOrientation = Request.Query.TryGetValue("orientation", out orientationParam);
            var hasPageSize = Request.Query.TryGetValue("pageSize", out pageSizeParam);
            var client = hasClient && clientParam.Count > 0 ? clientParam[0] : "";
            var key = hasKey && keyParam.Count > 0 ? keyParam[0] : "";
            var orientation = hasOrientation && orientationParam.Count > 0 ? orientationParam[0] : "portrait";
            var pageSize = hasPageSize && pageSizeParam.Count > 0 ? pageSizeParam[0] : "A4";

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

                        PageSize ps = PageSize.A4;

                        if (pageSize == "A3")
                        {
                            ps = PageSize.A3;
                        }

                        if (orientation == "landscape")
                        {
                            ps = ps.Rotate();
                        }

                        pdfDoc.SetDefaultPageSize(ps);

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
                    response = StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
                }

                Directory.Delete(tempFolder, true);

            } else
            {
                response = StatusCode((int)HttpStatusCode.BadRequest, new { error = "No doc file provided" });
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
