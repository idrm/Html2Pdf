# Html2Pdf

An ASP.NET Core Web API that converts an HTML document to PDF with iText 7 pdfHTML.

## Endpoint

`POST /api/convert?client=<Name>&key=<key>[&orientation=landscape][&pageSize=A3]`

Multipart form, each part named by its file name:
- `doc.html`: the document.
- Any other parts (fonts, images) are written next to it, so the document can reference them by
  file name. Fonts among them are registered with the font provider.

The page is A4 portrait unless `orientation=landscape` or `pageSize=A3` says otherwise. A wrong client
or key gets an empty 404, a missing `doc.html` gets 400, and a conversion failure gets 500 with the
error message.

`GET /healthz` answers `ok`, and `GET /buildz` reports the commit the image was built from.

**A client is any configuration key ending in `ApiKey`.** The prefix is the client name and the value is
its key: `PdfEngineApiKey` admits `client=PdfEngine`.

## Building

```sh
docker build --build-arg BUILD_SHA=$(git rev-parse HEAD) -t html2pdf:local .
docker run --rm -p 8080:8080 -e ExampleApiKey='<key>' html2pdf:local
```

pdfHTML is pinned at 3.0.1 (iText 7.1.12), the version this service has run since 2021, because a newer
iText lays the same HTML out differently.

## License

AGPL
