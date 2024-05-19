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
        Headers = new Dictionary<string, string>();
        Body = string.Empty;
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
        Body = body;
    }

    public void AddHeader(string key, string value)
    {
        Headers[key] = value;
    }
    public override string ToString()
    {
        try
        {
            StringBuilder response = new StringBuilder();
            response.Append($"HTTP/1.1 {StatusCode} {StatusMessage}\r\n");

            foreach (var header in Headers)
            {
                if (header.Key == null || header.Value == null)
                {
                    throw new FormatException("Header key or value is null.");
                }
                response.Append($"{header.Key}: {header.Value}\r\n");
            }

            response.Append("\r\n");
            if (Body != null)
            {
                response.Append(Body);
            }

            return response.ToString();
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"FormatException: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected Exception: {ex.Message}");
            throw;
        }
    }


}
