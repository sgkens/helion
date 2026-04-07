namespace Helion.Host.Middleware;

using Microsoft.AspNetCore.Http;
using Helion.Host.Gateway;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

public class GoReverseProxy
{
    private readonly HttpClient _httpClient;
    private readonly ProcessManager _processManager;

    public GoReverseProxy(ProcessManager processManager)
    {
        _processManager = processManager;
        
        // Correct approach for UDS in .NET 10
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.Unix, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Unspecified);
                await socket.ConnectAsync(new System.Net.Sockets.UnixDomainSocketEndPoint(_processManager.UdsPath!), cancellationToken);
                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
            }
        };

        _httpClient = new HttpClient(handler);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use a dummy host for the URI as the connection is handled by the ConnectCallback
        var targetUri = new Uri($"http://localhost{context.Request.Path}{context.Request.QueryString}");
        
        var requestMessage = new HttpRequestMessage();
        var requestMethod = context.Request.Method;
        if (!HttpMethods.IsGet(requestMethod) &&
            !HttpMethods.IsHead(requestMethod) &&
            !HttpMethods.IsDelete(requestMethod) &&
            !HttpMethods.IsTrace(requestMethod))
        {
            requestMessage.Content = new StreamContent(context.Request.Body);
        }

        foreach (var header in context.Request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
            {
                requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        requestMessage.Headers.Add("X-Helion-Ipc-Token", _processManager.IpcToken);
        requestMessage.Method = new HttpMethod(requestMethod);
        requestMessage.RequestUri = targetUri;

        try
        {
            using var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            context.Response.StatusCode = (int)responseMessage.StatusCode;
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            context.Response.Headers.Remove("transfer-encoding");
            await responseMessage.Content.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync($"Error proxying request to Go engine: {ex.Message}");
        }
    }
}
