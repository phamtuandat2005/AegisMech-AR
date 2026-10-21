using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using MechChess.Chess3D;
using MechChess.ProceduralMech;
using MechChess.UI;

namespace MechChess.Multiplayer
{
    [Serializable]
    public class OnlinePlayerInfo
    {
        public string username;
        public int elo;
        public string status; // "idle", "queue", "in_match"
    }

    [Serializable]
    public class OnlinePlayerListPacket
    {
        public string type;
        public OnlinePlayerInfo[] players;
    }

    [Serializable]
    public class NetworkPacket
    {
        public string type;
        public string room_id;
        public string role;
        public string name;
        public string white;
        public string black;
        public string from;
        public string to;
        public string notation;
        public string active_turn;
        public string message;
        public string by_role;
        public int player_count;

        // Matchmaking & Friend Duel extensions
        public string opponent;
        public int opponent_elo;
        public string time_control;
        public string invite_id;
        public string from_username;
        public int from_elo;
        public string target_username;
        public bool accepted;
    }

    /// <summary>
    /// Real-time Online 2-Player Chess Controller:
    /// Supports:
    /// 1. Real-time random matchmaking queue with auto pairing by server.
    /// 2. Direct friend invitation & challenges by username lookup.
    /// 3. In-game bidirectional move synchronization and turn enforcement.
    /// </summary>
    public class OnlineMultiplayerManager : MonoBehaviour
    {
        public static OnlineMultiplayerManager Instance { get; private set; }

        [Header("Connection Settings")]
        public string serverAddress = "ws://localhost:8000";
        public string currentRoomId = "ROOM-101";
        public string playerName = "CyberCommander";

        [Header("State")]
        public bool IsOnlineMatch { get; private set; } = false;
        public PieceColor MyRole { get; private set; } = PieceColor.White;
        public bool HasBothPlayers { get; private set; } = false;
        public string OpponentName { get; private set; } = "Opponent";
        public int OpponentElo { get; private set; } = 1200;
        public bool IsSearchingMatch { get; private set; } = false;

        // WebSocket Connections
        private ClientWebSocket _gameWebSocket;
        private CancellationTokenSource _gameCts;

        private ClientWebSocket _lobbyWebSocket;
        private CancellationTokenSource _lobbyCts;

        private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        private readonly object _queueLock = new object();

        public bool IsGameConnected => _gameWebSocket != null && _gameWebSocket.State == WebSocketState.Open;
        public bool IsLobbyConnected => _lobbyWebSocket != null && _lobbyWebSocket.State == WebSocketState.Open;

        // Public Events
        public event Action<string, PieceColor, string, int> OnMatchFoundEvent; // roomId, role, opponentName, opponentElo
        public event Action<string, int, string, string> OnFriendInviteReceivedEvent; // fromUser, fromElo, inviteId, roomId
        public event Action<string> OnFriendInviteDeclinedEvent;
        public event Action<string> OnInviteErrorEvent;
        public event Action OnMatchmakingSearchingEvent;
        public event Action OnMatchmakingCancelledEvent;
        public event Action<List<OnlinePlayerInfo>> OnOnlinePlayersUpdatedEvent;

        public List<OnlinePlayerInfo> CachedOnlinePlayers { get; private set; } = new List<OnlinePlayerInfo>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn)
            {
                playerName = UserAccountManager.Instance.CurrentUsername;
                ConnectToLobby();
            }
        }

        private void Update()
        {
            lock (_queueLock)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    _mainThreadQueue.Dequeue()?.Invoke();
                }
            }
        }

        private void OnDestroy()
        {
            DisconnectLobby();
            Disconnect();
        }

        public void EnqueueMainThread(Action action)
        {
            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        // =========================================================================
        // 1. LOBBY & MATCHMAKING WEBSOCKET
        // =========================================================================
        public async void ConnectToLobby()
        {
            if (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn)
            {
                playerName = UserAccountManager.Instance.CurrentUsername;
            }

            if (string.IsNullOrEmpty(playerName)) return;

            DisconnectLobby();

            _lobbyCts = new CancellationTokenSource();
            _lobbyWebSocket = new ClientWebSocket();

            string cleanHost = serverAddress.TrimEnd('/');
            string uriString = $"{cleanHost}/ws/lobby/{Uri.EscapeDataString(playerName)}";

            try
            {
                Uri uri = new Uri(uriString);
                await _lobbyWebSocket.ConnectAsync(uri, _lobbyCts.Token);
                Debug.Log($"[OnlineMultiplayer] Connected to Lobby WebSocket as '{playerName}'");
                _ = ReceiveLobbyLoop(_lobbyWebSocket, _lobbyCts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnlineMultiplayer] Lobby connection failed: {ex.Message}");
            }
        }

        public async void DisconnectLobby()
        {
            if (_lobbyCts != null)
            {
                _lobbyCts.Cancel();
                _lobbyCts.Dispose();
                _lobbyCts = null;
            }

            if (_lobbyWebSocket != null)
            {
                try
                {
                    if (_lobbyWebSocket.State == WebSocketState.Open)
                    {
                        await _lobbyWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Lobby close", CancellationToken.None);
                    }
                }
                catch { }
                _lobbyWebSocket.Dispose();
                _lobbyWebSocket = null;
            }
        }

        public async void StartRandomMatchmaking(string timeControl = "10 + 0")
        {
            IsSearchingMatch = true;
            OnMatchmakingSearchingEvent?.Invoke();

            if (!IsLobbyConnected)
            {
                ConnectToLobby();
                // Wait briefly for connection
                await Task.Delay(300);
            }

            string payload = $"{{\"type\":\"find_match\",\"time_control\":\"{timeControl}\"}}";
            await SendRawLobbyJson(payload);

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove($"[GHÉP TRẬN] Đang tìm đối thủ ngẫu nhiên online... Thể thức: {timeControl}");
            }
        }

        public async void CancelRandomMatchmaking()
        {
            IsSearchingMatch = false;
            OnMatchmakingCancelledEvent?.Invoke();

            if (IsLobbyConnected)
            {
                string payload = "{\"type\":\"cancel_match\"}";
                await SendRawLobbyJson(payload);
            }

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove("[GHÉP TRẬN] Đã hủy tìm trận đấu ngẫu nhiên.");
            }
        }

        public async void SendFriendInvite(string targetUsername)
        {
            if (string.IsNullOrEmpty(targetUsername)) return;

            if (!IsLobbyConnected)
            {
                ConnectToLobby();
                await Task.Delay(300);
            }

            string cleanTarget = targetUsername.Trim();
            string payload = $"{{\"type\":\"invite_friend\",\"target_username\":\"{cleanTarget}\"}}";
            await SendRawLobbyJson(payload);

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove($"[MỜI BẠN] Đã gửi lời mời thách đấu tới người chơi: {cleanTarget}...");
            }
        }

        public async void RespondToFriendInvite(string inviteId, string fromUsername, string roomId, bool accept)
        {
            if (!IsLobbyConnected) return;

            string payload = $"{{\"type\":\"respond_invite\",\"invite_id\":\"{inviteId}\",\"from_username\":\"{fromUsername}\",\"room_id\":\"{roomId}\",\"accepted\":{(accept ? "true" : "false")}}}";
            await SendRawLobbyJson(payload);
        }

        private async Task ReceiveLobbyLoop(ClientWebSocket ws, CancellationToken ct)
        {
            byte[] buffer = new byte[8192];

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleLobbyMessage(json);
                }
                catch
                {
                    break;
                }
            }
        }

        private void HandleLobbyMessage(string json)
        {
            try
            {
                NetworkPacket packet = JsonUtility.FromJson<NetworkPacket>(json);
                if (packet == null) return;

                EnqueueMainThread(() =>
                {
                    switch (packet.type)
                    {
                        case "match_found":
                        case "duel_start":
                            IsSearchingMatch = false;
                            PieceColor assignedRole = (packet.role == "white") ? PieceColor.White : PieceColor.Black;
                            OpponentName = packet.opponent;
                            OpponentElo = packet.opponent_elo > 0 ? packet.opponent_elo : 1200;
                            currentRoomId = packet.room_id;

                            OnMatchFoundEvent?.Invoke(packet.room_id, assignedRole, OpponentName, OpponentElo);

                            // Automatically connect to game match room
                            ConnectAndJoinRoom(packet.room_id);
                            break;

                        case "match_cancelled":
                            IsSearchingMatch = false;
                            OnMatchmakingCancelledEvent?.Invoke();
                            break;

                        case "friend_invite_received":
                            OnFriendInviteReceivedEvent?.Invoke(packet.from_username, packet.from_elo, packet.invite_id, packet.room_id);
                            break;

                        case "invite_declined":
                            OnFriendInviteDeclinedEvent?.Invoke(packet.message);
                            if (UIManager2D.Instance != null)
                            {
                                UIManager2D.Instance.RecordMove($"[TỪ CHỐI] {packet.message}");
                            }
                            break;

                        case "invite_error":
                            OnInviteErrorEvent?.Invoke(packet.message);
                            if (UIManager2D.Instance != null)
                            {
                                UIManager2D.Instance.RecordMove($"[LỖI MỜI ĐẤU] {packet.message}");
                            }
                            break;

                        case "online_players_list":
                            try
                            {
                                OnlinePlayerListPacket listPacket = JsonUtility.FromJson<OnlinePlayerListPacket>(json);
                                if (listPacket != null && listPacket.players != null)
                                {
                                    CachedOnlinePlayers.Clear();
                                    CachedOnlinePlayers.AddRange(listPacket.players);
                                    OnOnlinePlayersUpdatedEvent?.Invoke(CachedOnlinePlayers);
                                }
                            }
                            catch { }
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnlineMultiplayer] Lobby packet parse error: {ex.Message}");
            }
        }

        private async Task SendRawLobbyJson(string json)
        {
            if (_lobbyWebSocket != null && _lobbyWebSocket.State == WebSocketState.Open)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await _lobbyWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        // =========================================================================
        // 2. IN-GAME 1V1 CHESS MATCH WEBSOCKET (/ws/chess/{room_id})
        // =========================================================================
        public async void ConnectAndJoinRoom(string roomId = null, string serverUrl = null)
        {
            if (!string.IsNullOrEmpty(roomId)) currentRoomId = roomId.Trim().ToUpper();
            if (!string.IsNullOrEmpty(serverUrl)) serverAddress = serverUrl.Trim();

            Disconnect();

            _gameCts = new CancellationTokenSource();
            _gameWebSocket = new ClientWebSocket();

            string cleanHost = serverAddress.TrimEnd('/');
            string uriString = $"{cleanHost}/ws/chess/{currentRoomId}?name={Uri.EscapeDataString(playerName)}";

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove($"[ONLINE] Đang kết nối tới phòng: {currentRoomId}...");
            }

            try
            {
                Uri serverUri = new Uri(uriString);
                await _gameWebSocket.ConnectAsync(serverUri, _gameCts.Token);

                EnqueueMainThread(() =>
                {
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.RecordMove($"[ONLINE] Đã kết nối vào phòng trận đấu: {currentRoomId}!");
                    }
                });

                _ = ReceiveGameLoop(_gameWebSocket, _gameCts.Token);
            }
            catch (Exception ex)
            {
                EnqueueMainThread(() =>
                {
                    Debug.LogWarning($"[OnlineMultiplayer] Match connection failed: {ex.Message}");
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.RecordMove($"[ONLINE LỖI] Không thể kết nối tới server. Vui lòng kiểm tra Docker!");
                    }
                });
            }
        }

        public async void Disconnect()
        {
            IsOnlineMatch = false;
            HasBothPlayers = false;

            if (_gameCts != null)
            {
                _gameCts.Cancel();
                _gameCts.Dispose();
                _gameCts = null;
            }

            if (_gameWebSocket != null)
            {
                try
                {
                    if (_gameWebSocket.State == WebSocketState.Open)
                    {
                        await _gameWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client leaving", CancellationToken.None);
                    }
                }
                catch { }
                _gameWebSocket.Dispose();
                _gameWebSocket = null;
            }
        }

        private async Task ReceiveGameLoop(ClientWebSocket ws, CancellationToken ct)
        {
            byte[] buffer = new byte[8192];

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleGameMessage(json);
                }
                catch
                {
                    break;
                }
            }

            EnqueueMainThread(() =>
            {
                if (IsOnlineMatch)
                {
                    IsOnlineMatch = false;
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.RecordMove("[ONLINE] Đã ngắt kết nối phòng thi đấu.");
                    }
                }
            });
        }

        private void HandleGameMessage(string json)
        {
            try
            {
                NetworkPacket packet = JsonUtility.FromJson<NetworkPacket>(json);
                if (packet == null) return;

                EnqueueMainThread(() =>
                {
                    switch (packet.type)
                    {
                        case "room_joined":
                            MyRole = (packet.role == "white") ? PieceColor.White : PieceColor.Black;
                            string roleName = (MyRole == PieceColor.White) ? "QUÂN TRẮNG (Cyber Alliance)" : "QUÂN ĐEN (Dark Syndicate)";
                            if (UIManager2D.Instance != null)
                            {
                                UIManager2D.Instance.RecordMove($"[ONLINE] Bạn đã vào phòng: {packet.room_id}");
                                UIManager2D.Instance.RecordMove($"[ONLINE] VAI TRÒ CỦA BẠN: {roleName}");
                                if (packet.player_count < 2)
                                {
                                    UIManager2D.Instance.RecordMove("[ONLINE] Đang chờ đối thủ tham gia...");
                                }
                            }

                            if (MyRole == PieceColor.Black)
                            {
                                AlignCameraForBlackPlayer();
                            }
                            break;

                        case "match_start":
                            IsOnlineMatch = true;
                            HasBothPlayers = true;
                            OpponentName = (MyRole == PieceColor.White) ? packet.black : packet.white;

                            if (UIManager2D.Instance != null)
                            {
                                UIManager2D.Instance.ResetGameTimers();
                                UIManager2D.Instance.RecordMove("=================================");
                                UIManager2D.Instance.RecordMove($"[BẮT ĐẦU TRẬN ĐẤU ONLINE]");
                                UIManager2D.Instance.RecordMove($"Trắng: {packet.white} vs Đen: {packet.black}");
                                UIManager2D.Instance.RecordMove($"Bạn cầm: {(MyRole == PieceColor.White ? "TRẮNG (Đi trước)" : "ĐEN (Chờ đối thủ)")}");
                                UIManager2D.Instance.RecordMove("=================================");
                            }
                            break;

                        case "opponent_move":
                            if (ChessBoard3DManager.Instance != null)
                            {
                                ChessBoard3DManager.Instance.ExecuteRemoteMove(packet.from, packet.to);
                            }
                            break;

                        case "match_reset":
                            if (ChessBoard3DManager.Instance != null)
                            {
                                ChessBoard3DManager.Instance.ResetBoard();
                            }
                            if (UIManager2D.Instance != null)
                            {
                                UIManager2D.Instance.RecordMove($"[ONLINE] Trận đấu đã được đặt lại bởi {packet.by_role.ToUpper()}!");
                            }
                            break;

                        case "opponent_disconnected":
                            HasBothPlayers = false;
                            if (UIManager2D.Instance != null)
                            {
                                UIManager2D.Instance.RecordMove($"[ONLINE] {packet.message}");
                                UIManager2D.Instance.RecordMove("Đối thủ đã rời phòng. Bạn là người chiến thắng!");
                            }
                            break;

                        case "error":
                            if (UIManager2D.Instance != null)
                            {
                                UIManager2D.Instance.RecordMove($"[ONLINE THÔNG BÁO] {packet.message}");
                            }
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnlineMultiplayer] Error parsing game packet: {ex.Message}");
            }
        }

        private void AlignCameraForBlackPlayer()
        {
            var touch = Camera.main != null ? Camera.main.GetComponent<MechChess.Mobile.MobileTouchInput>() : null;
            if (touch != null)
            {
                touch.SetOrientation(180f, 45f);
            }
        }

        public async void SendMove(string fromSquare, string toSquare, string moveNotation)
        {
            if (!IsGameConnected || !IsOnlineMatch) return;

            string payload = $"{{\"type\":\"move\",\"from\":\"{fromSquare}\",\"to\":\"{toSquare}\",\"notation\":\"{moveNotation}\"}}";
            await SendRawGameJson(payload);
        }

        public async void SendReset()
        {
            if (!IsGameConnected || !IsOnlineMatch) return;
            string payload = "{\"type\":\"reset\"}";
            await SendRawGameJson(payload);
        }

        private async Task SendRawGameJson(string json)
        {
            if (_gameWebSocket != null && _gameWebSocket.State == WebSocketState.Open)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await _gameWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
