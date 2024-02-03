﻿using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _To : ITo
            {
                private bool _tryOpen, _tryClose;
                private HttpListener _listener;

                public readonly Server m_server;
                public bool IsOpened => _listener != null && _listener.IsListening;
                public Uri Host { get; private set; } = new Uri("http://0.0.0.0:80/");

                public _To(Server server)
                {
                    this.m_server = server;
                }

                public void Open(Uri host)
                {
                    if (IsOpened || _tryClose || _tryOpen) return;
                    _tryOpen = true;

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            HttpListener server = new HttpListener();

                            string httpUrl = $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{host.Host}:{host.Port}/";

                            server.Prefixes.Add(httpUrl);

                            m_server._on.m_onModify?.Invoke(null, server);

                            server.Start();

                            Host = host;

                            _listener = server;

                            m_server._on.m_onOpen?.Invoke(null, null);

                            ReceiveRequests();
                        }
                        catch (Exception e)
                        {
                            m_server._on.m_onError?.Invoke(null, e);
                        }
                        finally
                        {
                            _tryOpen = false;
                        }
                    });
                }

                public void Close()
                {
                    if (_tryOpen || _tryClose || _listener == null) return;

                    _tryClose = true;

                    Task.Run(() =>
                    {
                        try
                        {
                            if (_listener != null)
                            {
                                _listener.Stop();
                                _listener.Abort();
                                _listener.Close();
                            }

                            // TODO: Close all http connection

                            // TODO: Close all websocket connection
                        }
                        catch
                        {
                            // Ignored
                        }
                        finally
                        {
                            _listener = null;
                            m_server._on.m_onClose?.Invoke(null, null);
                        }
                    });
                }

                private void ReceiveRequests()
                {
                    Task.Run(() =>
                    {
                        while (IsOpened)
                        {
                            HttpListenerContext context = null;

                            try
                            {
                                context = _listener.GetContext();
                            }
                            catch
                            {
                                context = null;
                            }
                            finally
                            {
                                if (context != null)
                                {
                                    Task.Run(async () => await HandleConnection(context));
                                }
                            }
                        }

                        Close();
                    });
                }

                private async Task HandleConnection(HttpListenerContext context)
                {
                    var request = new Request(context.Request);
                    var response = new Response(context.Response);
                    var notFoundMessage = $"{request.Method.Method.ToUpper()} {request.Path}";

                    var skipConnectionByMiddleware = false;

                    void RunMiddlewares(IMiddlewareContainer[] middlewares)
                    {
                        foreach (var middleware in middlewares)
                        {
                            if (skipConnectionByMiddleware) break;

                            bool @continue = middleware.Callback(request, response);

                            if (!@continue)
                            {
                                skipConnectionByMiddleware = true;
                                response.Send(500, "Internal Server Error");
                                break;
                            }
                        }
                    }

                    // GLOBAL MIDDLEWARES
                    var globalMiddlewares = m_server.Middleware.Middlewares.ToList()
                        .FindAll(x => x.Path == _Middleware.GLOBAL_PATH);
                    RunMiddlewares(globalMiddlewares.ToArray());

                    // LOCAL MIDDLEWARES
                    var localMiddlewares = m_server.Middleware.Middlewares.ToList()
                        .FindAll(x => x.Path != _Middleware.GLOBAL_PATH && Path.ComparePath(request.Path, x.Path));
                    RunMiddlewares(localMiddlewares.ToArray());

                    if (skipConnectionByMiddleware)
                    {
                        return;
                    }

                    if (request.IsWebSocket == false) // IS HTTP CONNECTION
                    {
                        var paths = m_server._map.m_mapList.FindAll(x =>
                                Path.ComparePath(request.Path, x.Path) && !x.IsWebsocket &&
                                (request.Method.Method.ToUpper() == x.Method.ToUpper() ||
                                 x.Method.ToUpper() == _Map.ALL_MEHOD.ToUpper()))
                            .ToArray();

                        if (paths.Length <= 0)
                        {
                            response.Send(404, notFoundMessage);
                            return;
                        }

                        foreach (var path in paths)
                        {
                            path.HttpCallback?.Invoke(request, response);

                            if (response.IsOpened)
                            {
                                response.Send(508, $"Loop Detected {path.Path}");
                                throw new NotImplementedException(
                                    $"NULL response detected on [path='{path.Path}']");
                            }
                        }
                    }
                    else // IS WEBSOCKET CONNECTION
                    {
                        var paths = m_server._map.m_mapList.FindAll(x =>
                            Path.ComparePath(x.Path, request.Path) && x.IsWebsocket);

                        if (paths.Count <= 0)
                        {
                            response.Send(404, notFoundMessage);
                            return;
                        }


                        var ws = await context.AcceptWebSocketAsync(subProtocol: null);

                        var websocket = new WebSocket(ws.WebSocket, request);

                        foreach (var path in paths)
                        {
                            path.WebsocketCallback?.Invoke(request, websocket);
                        }

                        websocket.InitWebSocketServerSide();
                    }
                }
            }
        }
    }
}