using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Reflection;

namespace AspnetCore.ResponseCompression.Providers
{
    /// <summary>
    /// This class is inspired by the oficial ResponseProvider on:
    /// https://github.com/aspnet/BasicMiddleware/blob/dev/src/Microsoft.AspNetCore.ResponseCompression/ResponseCompressionProvider.cs
    /// Current problem is that if you have more than one provider, gzip will always take place since browsers do not send priority
    /// I'm changing the logic here to match by the order providers are in the list.
    /// </summary>
    public class OrderedResponseCompressionProvider : ResponseCompressionProvider
    {
        protected readonly ICompressionProvider[] _providers;

        public OrderedResponseCompressionProvider(
            IServiceProvider services,
            IOptions<ResponseCompressionOptions> options
        ) : base(services, options)
        {
            // On base class _providers is private
            _providers = (ICompressionProvider[])typeof(ResponseCompressionProvider)
                .GetField("_providers", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(this);
        }

        public override ICompressionProvider GetCompressionProvider(HttpContext context)
        {
            // e.g. Accept-Encoding: gzip, deflate, sdch
            var accept = context.Request.Headers[HeaderNames.AcceptEncoding];
            if (!StringValues.IsNullOrEmpty(accept)
                && StringWithQualityHeaderValue.TryParseList(accept, out var unsorted)
                && unsorted != null && unsorted.Count > 0)
            {
                foreach (var provider in _providers)
                {
                    if (unsorted.Any(
                        x => StringSegment.Equals(provider.EncodingName, x.Value, StringComparison.Ordinal)))
                    {
                        return provider;
                    }
                }
            }

            return base.GetCompressionProvider(context);
        }
    }
}
