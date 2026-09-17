using System.Security.Cryptography;

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace TixTapGo.Shared.Web.ETag;

public class ETagMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // For now support only GET + If-None-Match requests. The conflict resolution
        // flow (If-Match) is postponed until ranged requests appearance
        if (context.Request.Method != HttpMethods.Get ||
            context.GetEndpoint()?.Metadata.GetMetadata<SkipETagAttribute>() != null)
        {
            await next(context);
            return;
        }

        var originalResponseStream = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalResponseStream;
        }

        if (context.Response.StatusCode == StatusCodes.Status200OK)
        {
            buffer.Position = 0;
            byte[] hash = await SHA256.HashDataAsync(buffer);
            string newEtag = $"\"{Convert.ToBase64String(hash)}\"";

            context.Response.Headers.ETag = newEtag;

            if (EntityTagHeaderValue.TryParseStrictList(context.Request.Headers.IfNoneMatch, out var etags))
            {
                bool isETagMatched = etags.Any(e => e.Tag == newEtag || e.Tag == EntityTagHeaderValue.Any.Tag);
                if (isETagMatched)
                {
                    context.Response.StatusCode = StatusCodes.Status304NotModified;
                    context.Response.ContentLength = null;
                    return;
                }
            }
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(originalResponseStream);
    }
}
