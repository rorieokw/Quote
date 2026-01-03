using Microsoft.AspNetCore.SignalR.Client;
using Quote.Shared.DTOs;

namespace Quote.Mobile.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly AuthService _authService;
    private bool _isConnecting;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

#if DEBUG
    // For Android emulator: use 10.0.2.2, for iOS simulator: use localhost
    private const string HubUrl = "http://10.0.2.2:5102/hubs/messaging";
#else
    private const string HubUrl = "https://your-production-api.com/hubs/messaging";
#endif

    public event Action<MessageDto>? OnMessageReceived;
    public event Action<Guid, Guid>? OnMessagesRead;
    public event Action<Guid, Guid, bool>? OnUserTyping;
    public event Action<bool>? OnConnectionStateChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public SignalRService(AuthService authService)
    {
        _authService = authService;
    }

    public async Task ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                return;
            }

            if (_isConnecting)
            {
                return;
            }

            _isConnecting = true;

            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _isConnecting = false;
                return;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(HubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            RegisterHandlers();

            _hubConnection.Reconnecting += _ =>
            {
                OnConnectionStateChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += _ =>
            {
                OnConnectionStateChanged?.Invoke(true);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += _ =>
            {
                OnConnectionStateChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            OnConnectionStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR connection failed: {ex.Message}");
            OnConnectionStateChanged?.Invoke(false);
        }
        finally
        {
            _isConnecting = false;
            _connectionLock.Release();
        }
    }

    private void RegisterHandlers()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<MessageDto>("ReceiveMessage", message =>
        {
            OnMessageReceived?.Invoke(message);
        });

        _hubConnection.On<Guid, Guid>("MessagesRead", (conversationId, userId) =>
        {
            OnMessagesRead?.Invoke(conversationId, userId);
        });

        _hubConnection.On<Guid, Guid, bool>("UserTyping", (conversationId, userId, isTyping) =>
        {
            OnUserTyping?.Invoke(conversationId, userId, isTyping);
        });
    }

    public async Task JoinConversationAsync(Guid conversationId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinConversation", conversationId);
        }
    }

    public async Task LeaveConversationAsync(Guid conversationId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveConversation", conversationId);
        }
    }

    public async Task SendMessageAsync(Guid conversationId, string content, string? mediaUrl = null, string? mediaType = null, string? fileName = null)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendMessage", conversationId, content, mediaUrl, mediaType, fileName);
        }
    }

    public async Task MarkAsReadAsync(Guid conversationId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("MarkAsRead", conversationId);
        }
    }

    public async Task StartTypingAsync(Guid conversationId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("StartTyping", conversationId);
        }
    }

    public async Task StopTypingAsync(Guid conversationId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("StopTyping", conversationId);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectionLock.Dispose();
    }
}
