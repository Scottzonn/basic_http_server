using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

class HttpResponse
{
    private string _body;
    public int StatusCode { get; set; }
    public string StatusMessage { get; set; }
    public Dictionary<string, string> Headers { get; set; }

    // Set content length when body is set
    public string Body
    {
        get => _body;
        set
        {
            _body = value;
            if (Headers != null && !Headers.ContainsKey("Content-Encoding"))
            {
                Headers["Content-Length"] = Encoding.UTF8.GetByteCount(value).ToString();
            }
        }
    }

    public HttpResponse()
    {
        Headers = new Dictionary<string, string>();
        _body = string.Empty;
        StatusCode = 200;
        StatusMessage = "OK";
    }

    public HttpResponse(int statusCode, string statusMessage) : this()
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
    }

    public HttpResponse(int statusCode, string statusMessage, string body) : this(statusCode, statusMessage)
    {
        Body = body;
    }

    public HttpResponse(int statusCode, string statusMessage, string body, Dictionary<string, string> headers) : this(statusCode, statusMessage)
    {
        Headers = headers;
        Body = body; // Set Body after Headers to avoid null reference
    }

    public void AddHeader(string key, string value)
    {
        Headers[key] = value;
    }

    private byte[] GetCompressedBody()
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(_body);
        using (MemoryStream ms = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
            {
                gzip.Write(bodyBytes, 0, bodyBytes.Length);
            }
            return ms.ToArray();
        }
    }

    public byte[] ToByteArray(bool gzip = false)
    {
        if (gzip)
        {
            return GetCompressedBodyResponse();
        }
        return Encoding.UTF8.GetBytes(ToString());
    }

    private byte[] GetCompressedBodyResponse()
    {
        byte[] headerBytes = Encoding.UTF8.GetBytes(GetResponseHeadersWithCompressedBody());
        byte[] bodyBytes = GetCompressedBody();
        byte[] responseBytes = new byte[headerBytes.Length + bodyBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, responseBytes, 0, headerBytes.Length);
        Buffer.BlockCopy(bodyBytes, 0, responseBytes, headerBytes.Length, bodyBytes.Length);
        return responseBytes;
    }

    private string GetResponseHeadersWithCompressedBody()
    {
        StringBuilder response = new StringBuilder();
        response.Append($"HTTP/1.1 {StatusCode} {StatusMessage}\r\n");
        foreach (var header in Headers)
        {
            response.Append($"{header.Key}: {header.Value}\r\n");
        }
        response.Append($"Content-Length: {GetCompressedBody().Length}\r\n");
        response.Append("Content-Encoding: gzip\r\n");
        response.Append("\r\n");
        return response.ToString();
    }

    public override string ToString()
    {
        StringBuilder response = new StringBuilder();
        response.Append($"HTTP/1.1 {StatusCode} {StatusMessage}\r\n");
        foreach (var header in Headers)
        {
            response.Append($"{header.Key}: {header.Value}\r\n");
        }
        response.Append("\r\n");
        response.Append(_body);
        return response.ToString();
    }
}
