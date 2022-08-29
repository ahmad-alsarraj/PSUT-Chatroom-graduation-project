using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Server.Db.Entities;
using Server.Services.UserSystem;

namespace Server.Services.WebSockets;
//Send on user entity socket
//Send to all users entity sockets (can contain the first one since user can skip his notifications, maybe we can provide skip user)
public sealed class SemaphoreWaitHandle : IDisposable
{
    private SemaphoreSlim? _semaphore;
    private bool _isDisposed;
    public SemaphoreWaitHandle(SemaphoreSlim s)
    {
        _semaphore = s;
    }
    public void Dispose()
    {
        if (_isDisposed) { return; }
        _isDisposed = true;
        _semaphore!.Release();
        _semaphore = null;
    }
}
public class WebSocketHandler : IAsyncDisposable
{
    public WebSocket Socket { get; private set; }
    private SemaphoreSlim? _socketSemaphore = new(1);
    private TaskCompletionSource? _socketCompletionSource = new();
    public Task Wait()
    {
        return _socketCompletionSource.Task;
    }
    private static readonly byte[] HelloMessage = Encoding.Default.GetBytes("hello there");
    public WebSocketHandler(WebSocket socket)
    {
        Socket = socket;
        socket.SendAsync(HelloMessage, WebSocketMessageType.Text, false, default).Wait();
    }
    public async Task<SemaphoreWaitHandle> UseSocket()
    {
        await _socketSemaphore.WaitAsync().ConfigureAwait(false);
        return new(_socketSemaphore);
    }
    private bool _isDisposed;
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) { return; }
        _isDisposed = true;
        _socketSemaphore!.Dispose();
        _socketSemaphore = null;
        await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default).ConfigureAwait(false);
        Socket.Dispose();
        Socket = null;
        _socketCompletionSource.SetResult();
        _socketCompletionSource = null;
    }
}
public class WebSocketsContext
{
    public WebSocketsManager Manager { get; }
    public string Category { get; set; }
    public int? UserId { get; }
    public WebSocketsContext(WebSocketsManager man, string category, User? owner)
    {
        Manager = man;
        Category = category;
        UserId = owner?.Id;
    }
    public async Task SendToUsers<TData, TEvent>(int entityId, HashSet<int> usersIds, TEvent ev, TData? data = null) where TData : class
    {
        await Manager.Send(Category, uId => usersIds.Contains(uId), ev, data, entityId).ConfigureAwait(false);
    }
    public async Task SendToUsers<TData, TEvent>(HashSet<int> usersIds, TEvent ev, TData? data = null) where TData : class
    {
        await Manager.Send(Category, uId => usersIds.Contains(uId), ev, data, WebSocketsManager.NoEntityId).ConfigureAwait(false);
    }
}
public record NotificationDto<T>(string Event, T? Data) where T : class;
//TODO: create async ReaderWriterLockeSlim
public class WebSocketsManager
{
    public static int NoEntityId { get; } = -1;
    private readonly ReaderWriterLockSlim _socketsLock = new();
    //message category(conversation, ping, group), { entityId, userId, socket }
    //messages conv1 usr1 sokt
    //messages conv1 usr2 sokt
    //messages conv2 usr1 sokt
    public ConcurrentDictionary<string, ConcurrentDictionary<int, ConcurrentDictionary<int, WebSocketHandler>>> Sockets { get; } = new();
    private async Task SendOnSocket(ReadOnlyMemory<byte> notificationJson, WebSocketHandler socketHandler)
    {
        if (socketHandler.Socket.CloseStatus != null) { return; }
        using var waitHandle = await socketHandler.UseSocket().ConfigureAwait(false);
        try
        {
            await socketHandler.Socket.SendAsync(notificationJson, WebSocketMessageType.Text, false, default).ConfigureAwait(false);
        }
        catch { }
    }
    private async Task SendForUser(ReadOnlyMemory<byte> notificationJson, WebSocketHandler socketHandler)
    {
        await SendOnSocket(notificationJson, socketHandler).ConfigureAwait(false);
    }
    private async Task SendForEntity<TData, TEvent>(ConcurrentDictionary<int, WebSocketHandler> entitySockets, Func<int, bool> userPredicate, TEvent ev, TData? data = null) where TData : class
    {
        var notification = new NotificationDto<TData>(ev!.ToString()!, data);
        var notificationJson = Encoding.Default.GetBytes(JsonSerializer.Serialize(notification));
        var usersToNotify = entitySockets.Where(p => userPredicate(p.Key)).Select(p => p.Value).ToArray();
        await Parallel.ForEachAsync(usersToNotify, async (s, _) => await SendForUser(notificationJson, s).ConfigureAwait(false));
    }
    public async Task Send<TData, TEvent>(string messageCategory, Func<int, bool> userPredicate, TEvent ev, TData? data, int entityId) where TData : class
    {
        //_socketsLock.EnterReadLock();
        _logger.LogInformation("Entered read lock");
        try
        {
            if (!Sockets.TryGetValue(messageCategory, out var categorySockets)) { return; }

            if (!categorySockets.TryGetValue(entityId, out var userSockets)) { return; }
            await SendForEntity(userSockets, userPredicate, ev, data).ConfigureAwait(false);
        }
        finally
        {
            //_socketsLock.ExitReadLock();
            _logger.LogInformation("Exited read lock");
        }
    }
    public async Task<WebSocketHandler> CreateSocket(string messageCategory, int? entityId = null)
    {
        //_socketsLock.EnterWriteLock();
        _logger.LogInformation("Entered write lock");
        try
        {
            var solidEntityId = entityId.HasValue ? entityId.Value : NoEntityId;
            var user = _httpContext.HttpContext.GetUser();
            var socket = await _httpContext.HttpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var handler = new WebSocketHandler(socket);
            var categorySockets = Sockets.GetOrAdd(messageCategory, _ => new());
            var entitySockets = categorySockets.GetOrAdd(solidEntityId, _ => new());
            entitySockets.AddOrUpdate(user.Id, _ => handler, (_, oldSocket) =>
            {
                oldSocket.DisposeAsync().AsTask().Wait();
                return handler;
            });
            return handler;
        }
        finally
        {
            //_socketsLock.ExitWriteLock();
            _logger.LogInformation("Exited write lock");
        }
    }
    public WebSocketsContext CreateContext(string category)
    {
        return new(this, category, _httpContext.HttpContext.GetUser());
    }
    private readonly IHttpContextAccessor _httpContext;
    private readonly ReadOnlyMemory<byte> _isAliveJson;
    private readonly ILogger _logger;
    public WebSocketsManager(IHttpContextAccessor httpContext, ILogger<WebSocketsManager> logger)
    {
        _isAliveJson = Encoding.Default.GetBytes(JsonSerializer.Serialize(new NotificationDto<string>("IsAlive", null)));
        _httpContext = httpContext;
        _logger = logger;
    }
    private async Task PurgeIfDead(ConcurrentDictionary<int, WebSocketHandler> socketDic, int userId)
    {
        var socket = socketDic[userId];
        using var waitHandle = await socket.UseSocket().ConfigureAwait(false);
        await socket.Socket.SendAsync(_isAliveJson, WebSocketMessageType.Text, false, default);
        await Task.Delay(200).ConfigureAwait(false);
        using CancellationTokenSource signalWaitingCancellationTokenSrc = new();
        signalWaitingCancellationTokenSrc.CancelAfter(100);
        var buff = new byte[50];
        var signalInfo = await socket.Socket.ReceiveAsync(buff, signalWaitingCancellationTokenSrc.Token);
        if (signalInfo.Count == 0)
        {
            socketDic.Remove(userId, out var _);
            await socket.DisposeAsync().ConfigureAwait(false);
        }
    }
    public async Task PurgeDeadSockets()
    {
        _socketsLock.EnterWriteLock();
        try
        {
            var socketsDics = Sockets.SelectMany(s => s.Value).Select(s => s.Value);
            await Parallel.ForEachAsync(socketsDics, async (dic, _) =>
            {
                var usersIds = dic.Keys.ToArray();
                foreach (var userId in usersIds)
                {
                    await PurgeIfDead(dic, userId).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            _socketsLock.ExitWriteLock();
        }
    }
    public async Task CloseUserSockets()
    {
        var user = _httpContext.HttpContext.GetUser();
        if (user == null) { return; }
        _socketsLock.EnterWriteLock();
        try
        {
            foreach (var categorySockets in Sockets.Values)
            {
                foreach (var entitySockets in categorySockets.Values)
                {
                    if (!entitySockets.TryGetValue(user.Id, out var userSocket)) { continue; }
                    await userSocket.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _socketsLock.ExitWriteLock();
        }
    }
}