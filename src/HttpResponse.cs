using System.Text;

class HttpResponse
{
    private string _body;
    public int StatusCode { get; set; }
    public string StatusMessage { get; set; }
    public Dictionary<string, string> Headers { get; set; }

    //set content length when body is set
    public string Body
    {
        get => _body;
        set
        {
            _body = value;
            Headers["Content-Length"] = Encoding.UTF8.GetByteCount(value).ToString();
        }
    }

    public HttpResponse()
    {
        Body = string.Empty;
        _body = string.Empty;
        StatusCode = 200;
        StatusMessage = "OK";
        Headers = new Dictionary<string, string>();
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

    public HttpResponse(int statusCode, string statusMessage, string body, Dictionary<string, string> headers) : this(statusCode, statusMessage, body)
    {
        Headers = headers;
    }

    public void AddHeader(string key, string value)
    {
        Headers[key] = value;
    }
    public override string ToString()
    {
        StringBuilder response = new StringBuilder();
        response.Append($"HTTP/1.1 {StatusCode} {StatusMessage}\r\n");
        foreach (var header in Headers)
        {
            response.Append($"{header.Key}: {header.Value}\r\n");
        }
        response.Append($"\r\n{Body}");
        return response.ToString();
    }

}
