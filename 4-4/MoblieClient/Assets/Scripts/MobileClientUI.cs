using FishNet;
using FishNet.Transporting.Tugboat;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class MobileClientUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Button connectButton;
    public TextMeshProUGUI statusText;

    private const string SERVER_IP = "220.81.195.207";
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
        if (_isConnected) return;

        DOTween.Restart("PunchFX");

        var tugboat = InstanceFinder.NetworkManager.GetComponent<Tugboat>();
        tugboat.SetClientAddress(SERVER_IP);
        tugboat.SetPort(7777);

        InstanceFinder.ClientManager.StartConnection();

        connectButton.interactable = false;
        statusText.text = "연결 시도 중...";
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
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= OnConnectionState;
    }
}