using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;
using ScoreCast.Shared.Extensions;
using ScoreCast.Shared.Types;

namespace ScoreCast.Shared.Helpers.ObjectHelper;

public static class ObjectConverter
{
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        MaxDepth = 100,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };


    private static readonly JsonSerializerOptions SerializeOptionsNoCamel = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true,
        IncludeFields = false,
        MaxDepth = 100,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static string ReadContentSync(HttpContent? content)
    {
        if (content is null) return string.Empty;
        using var stream = content.ReadAsStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string ToJsonString(this object? objInput)
    {
        if (objInput is null) return string.Empty;
        try
        {
            return objInput switch
            {
                string => Convert.ToString(objInput!),
                Exception exp => exp.Message + ":" + exp.GetBaseException().Message,
                HttpRequestMessage httpReq => JsonSerializer.Serialize(new
                {
                    RequestUrl = httpReq.RequestUri?.ToString(),
                    RequestMethod = httpReq.Method.ToString(),
                    RequestHeaderValues = string.Join(" | ",
                        httpReq.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    ContentHeaderValues = string.Join(" | ",
                        httpReq.Content is null
                            ? []
                            : httpReq.Content.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    RequestBody = httpReq.Content is not null
                        ? ReadContentSync(httpReq.Content)
                        : string.Empty
                }, SerializeOptions),
                HttpResponseMessage httpResp => JsonSerializer.Serialize(new
                {
                    Message = httpResp.ReasonPhrase,
                    RequestUri = httpResp.RequestMessage?.RequestUri?.ToString(),
                    StatusCode = httpResp.StatusCode.ToString(),
                    ResponseString = ReadContentSync(httpResp.Content),
                    ResponseHeaderValues = string.Join(" | ",
                        httpResp.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    ContentHeaderValues = string.Join(" | ",
                        httpResp.Content?.Headers is null
                            ? []
                            : httpResp.Content.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}")))
                }, SerializeOptions) ?? string.Empty,
                _ => JsonSerializer.Serialize(objInput, SerializeOptions) ?? string.Empty
            } ?? string.Empty;
        }
        catch (Exception exp)
        {
            return $"{objInput} - {exp.GetBaseException().Message}";
        }
    }

    private static string ToXml<T>(T? obj)
    {
        switch (obj)
        {
            case null:
                return string.Empty;
            case string:
                return obj.ToString() ?? string.Empty;
            case Exception ex:
                return $"{ex.Message}:{ex.GetBaseException().Message}";
        }

        using var sw = new XmlUtf8Writer();
        var serializer = new XmlSerializer(obj.GetType());
        serializer.Serialize(sw, obj);
        return sw?.ToString() ?? string.Empty;
    }


    public static string ToXmlString<T>(this T? objInput)
    {
        if (objInput is null) return string.Empty;
        try
        {
            return objInput switch
            {
                string => Convert.ToString(objInput!),
                Exception exp => exp.GetBaseException().Message,
                HttpRequestMessage httpReq => ToXml(new
                {
                    RequestUrl = httpReq.RequestUri?.ToString(),
                    RequestMethod = httpReq.Method.ToString(),
                    RequestHeaderValues = string.Join(" | ",
                        httpReq.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    ContentHeaderValues = string.Join(" | ",
                        httpReq.Content is null
                            ? []
                            : httpReq.Content.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    RequestBody = httpReq.Content is not null
                        ? ReadContentSync(httpReq.Content)
                        : string.Empty
                }),
                HttpResponseMessage httpResp => ToXml(new
                {
                    Message = httpResp.ReasonPhrase,
                    RequestUri = httpResp.RequestMessage?.RequestUri?.ToString(),
                    StatusCode = httpResp.StatusCode.ToString(),
                    ResponseString = ReadContentSync(httpResp.Content),
                    ResponseHeaderValues = string.Join(" | ",
                        httpResp.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    ContentHeaderValues = string.Join(" | ",
                        httpResp.Content?.Headers is null
                            ? []
                            : httpResp.Content.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}")))
                }),
                _ => ToXml(objInput)
            } ?? string.Empty;
        }
        catch (InvalidCastException invalidCastException)
        {
            return $"Unable to cast/convert to Xml : {invalidCastException.GetBaseException().Message}";
        }
        catch (Exception exp)
        {
            return $"{objInput} - {exp.GetBaseException().Message}";
        }
    }

    public static string? SerializeNoCamelCase(object? obj)
    {
        try
        {
            return obj switch
            {
                null => string.Empty,
                string => Convert.ToString(obj!),
                Exception exp => exp.GetBaseException().Message,
                HttpRequestMessage httpReq => JsonSerializer.Serialize(new
                {
                    RequestUrl = httpReq.RequestUri?.ToString(),
                    RequestMethod = httpReq.Method.ToString(),
                    RequestHeaderValues = string.Join(" | ",
                        httpReq.Headers is null
                            ? []
                            : httpReq.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    ContentHeaderValues = string.Join(" | ",
                        httpReq.Content is null
                            ? []
                            : httpReq.Content.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    RequestBody = httpReq.Content is not null
                        ? ReadContentSync(httpReq.Content)
                        : string.Empty
                }, SerializeOptions),
                HttpResponseMessage httpResp => JsonSerializer.Serialize(new
                {
                    Message = httpResp.ReasonPhrase,
                    RequestUri = httpResp.RequestMessage?.RequestUri?.ToString(),
                    StatusCode = httpResp.StatusCode.ToString(),
                    ResponseString = httpResp.Content is not null
                        ? ReadContentSync(httpResp.Content)
                        : string.Empty,
                    ResponseHeaderValues = string.Join(" | ",
                        httpResp.Headers is null
                            ? []
                            : httpResp.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}"))),
                    ContentHeaderValues = string.Join(" | ",
                        httpResp.Content?.Headers is null
                            ? []
                            : httpResp.Content.Headers.SelectMany(hdr => hdr.Value.Select(v => $"{hdr.Key}:{v}")))
                }, SerializeOptions),
                _ => JsonSerializer.Serialize(obj, SerializeOptionsNoCamel)
            };
        }
        catch (Exception exp)
        {
            return $"{obj} - {exp.GetBaseException().Message}";
        }
    }


    public static T? DeSerializeJson<T>(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return default;
        }

        if (typeof(T) == typeof(string))
        {
            return (T?)Convert.ChangeType(jsonContent, typeof(T?));
        }

        try
        {
            return jsonContent.IsJsonContent()
                ? JsonSerializer.Deserialize<T>(jsonContent, SerializeOptions)
                : default;
        }
        catch (Exception exp)
        {
            var message = exp.Message;
            return (T?)Convert.ChangeType(jsonContent, typeof(T?));
        }
    }

    public static T? DeSerializeXml<T>(string? xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent)) return default;

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        var serializer = new XmlSerializer(typeof(T));

        using var reader = new StringReader(xmlContent);

        using var xmlReader = XmlReader.Create(reader, settings);

        return (T?)serializer.Deserialize(xmlReader);
    }

    public static string EncodeToBase64(string? contentToEncode)
    {
        return string.IsNullOrWhiteSpace(contentToEncode)
            ? string.Empty
            : Convert.ToBase64String(Encoding.UTF8.GetBytes(contentToEncode));
    }

    public static string EncodeToBase64(byte[]? byteContent)
    {
        return Convert.ToBase64String(byteContent ?? []);
    }

    public static string DecodeFromBase64(string? contentToEncode)
    {
        if (string.IsNullOrWhiteSpace(contentToEncode))
            return string.Empty;

        var encodedBytes = Convert.FromBase64String(contentToEncode);

        return encodedBytes is null ? string.Empty : Encoding.UTF8.GetString(encodedBytes);
    }

    public static byte[] ToUtf8Bytes(string? content)
    {
        return Encoding.UTF8.GetBytes(content ?? "empty string");
    }
}
