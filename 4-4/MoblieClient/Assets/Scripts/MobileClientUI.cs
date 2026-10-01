using FishNet;
using FishNet.Transporting.Tugboat;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

public class MobileClientUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Button connectButton;
    public TextMeshProUGUI statusText;

    [Header("브로드캐스트 설정")]
    public int broadcastPort = 47777;       // ServerBroadcaster와 동일하게
    public float searchTimeout = 10f;       // 탐색 제한 시간 (초)

    private UdpClient _udpClient;
    private string _foundIP = "";
    private bool _isSearching = false;
    private bool _isConnected = false;

    void Start()
    {
        if (InstanceFinder.NetworkManager != null)
            DontDestroyOnLoad(InstanceFinder.NetworkManager.gameObject);

        connectButton.onClick.AddListener(OnConnectClicked);
        InstanceFinder.ClientManager.OnClientConnectionState += OnConnectionState;

        statusText.text = "연결 버튼을 눌러주세요";
    }

    void OnConnectClicked()
    {
        if (_isSearching || _isConnected) return;

        // 펀치 애니메이션
        DOTween.Restart("PunchFX");

        connectButton.interactable = false;
        statusText.text = "서버 탐색 중...";

        StartCoroutine(SearchAndConnect());
    }

    IEnumerator SearchAndConnect()
    {
        _isSearching = true;
        _foundIP = "";

        // UDP 수신 시작
        StartCoroutine(ListenForBroadcast());

        // 탐색 타임아웃
        float elapsed = 0f;
        while (_foundIP == "" && elapsed < searchTimeout)
        {
            elapsed += Time.deltaTime;
            // 점 애니메이션으로 탐색 중 표시
            int dots = (int)(elapsed % 3) + 1;
            statusText.text = "서버 탐색 중" + new string('.', dots);
            yield return null;
        }

        _isSearching = false;
        StopUDP();

        if (_foundIP == "")
        {
            // 탐색 실패
            statusText.text = "❌ 서버를 찾을 수 없습니다\n PC가 켜져있는지 확인해주세요";
            connectButton.interactable = true;
            yield break;
        }

        // IP 찾음 → 연결 시도
        statusText.text = $"서버 발견! 연결 중...";

        var tugboat = InstanceFinder.NetworkManager.GetComponent<Tugboat>();
        tugboat.SetClientAddress(_foundIP);
        tugboat.SetPort(7777);

        InstanceFinder.ClientManager.StartConnection();
    }

    IEnumerator ListenForBroadcast()
    {
        _udpClient = new UdpClient(broadcastPort);
        _udpClient.Client.ReceiveTimeout = 500;

        while (_isSearching)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                // "POLICE_GAME:192.168.0.xxx" 형식 확인
                if (message.StartsWith("POLICE_GAME:"))
                {
                    _foundIP = message.Replace("POLICE_GAME:", "");
                    Debug.Log($"서버 발견: {_foundIP}");
                }
            }
            catch { }

            yield return null;
        }
    }

    void StopUDP()
    {
        _udpClient?.Close();
        _udpClient = null;
    }

    void OnConnectionState(FishNet.Transporting.ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            _isConnected = true;
            statusText.text = "✅ 연결 성공!\n서버 응답 대기 중...";
        }
        else if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Stopped)
        {
            _isConnected = false;
            statusText.text = "❌ 연결 실패 또는 끊김";
            connectButton.interactable = true;
        }
    }

    void OnDestroy()
    {
        StopUDP();
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= OnConnectionState;
    }
}