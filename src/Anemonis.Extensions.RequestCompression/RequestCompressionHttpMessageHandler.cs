﻿// © Oleksandr Kozlenko. Licensed under the MIT license.

#pragma warning disable CS1591

using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Anemonis.Extensions.RequestCompression;

public sealed class RequestCompressionHttpMessageHandler : DelegatingHandler
{
    private readonly IRequestCompressionProvider _compressionProvider;
    private readonly CompressionLevel _compressionLevel;
    private readonly IEnumerable<string> _mimeTypes;
    private readonly ILogger? _logger;

    public RequestCompressionHttpMessageHandler(IRequestCompressionProvider compressionProvider, CompressionLevel compressionLevel, IEnumerable<string> mimeTypes, ILogger? logger)
    {
        _compressionProvider = compressionProvider;
        _compressionLevel = compressionLevel;
        _mimeTypes = mimeTypes;
        _logger = logger;
    }

    protected sealed override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request?.Content is { } originalContent)
        {
            if (_mimeTypes.Contains(originalContent.Headers.ContentType?.MediaType!))
            {
                request.Content = CreateCompressionStreamContent(originalContent);
                _logger?.CompressWith(_compressionProvider.EncodingName);
            }
        }

        return base.Send(request!, cancellationToken);
    }

    protected sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request?.Content is { } originalContent)
        {
            if (_mimeTypes.Contains(originalContent.Headers.ContentType?.MediaType!))
            {
                request.Content = CreateCompressionStreamContent(originalContent);
                _logger?.CompressWith(_compressionProvider.EncodingName);
            }
        }

        return base.SendAsync(request!, cancellationToken);
    }

    private HttpContent CreateCompressionStreamContent(HttpContent originalContent)
    {
        var compressionContent = new CompressionStreamContent(originalContent, _compressionProvider, _compressionLevel);

        foreach (var (name, values) in originalContent.Headers.NonValidated)
        {
            compressionContent.Headers.TryAddWithoutValidation(name, values);
        }

        compressionContent.Headers.ContentEncoding.Add(_compressionProvider.EncodingName);
        compressionContent.Headers.ContentLength = null;

        return compressionContent;
    }
}
