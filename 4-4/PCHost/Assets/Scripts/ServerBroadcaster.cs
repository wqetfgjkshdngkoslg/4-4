using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

/// <summary>
/// PC 서버에서 UDP 브로드캐스트로 자신의 IP를 주기적으로 송신
/// WaitingScene 오브젝트에 부착
/// </summary>
public class ServerBroadcaster : MonoBehaviour
{
    [Header("브로드캐스트 설정")]
    public int broadcastPort = 47777;       // 브로드캐스트 전용 포트 (7777과 다르게)
    public float broadcastInterval = 1f;    // 송신 간격 (초)

    private UdpClient _udpClient;
    private bool _isBroadcasting = false;

    void Start()
    {
        StartBroadcast();
    }

    public void StartBroadcast()
    {
        if (_isBroadcasting) return;
        _isBroadcasting = true;
        StartCoroutine(BroadcastLoop());
        Debug.Log("ServerBroadcaster: 브로드캐스트 시작");
    }

    public void StopBroadcast()
    {
        _isBroadcasting = false;
        _udpClient?.Close();
        _udpClient = null;
        Debug.Log("ServerBroadcaster: 브로드캐스트 중지");
    }

    IEnumerator BroadcastLoop()
    {
        _udpClient = new UdpClient();
        _udpClient.EnableBroadcast = true;

        IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
        string myIP = GetLocalIP();
        byte[] data = Encoding.UTF8.GetBytes($"POLICE_GAME:{myIP}");

        while (_isBroadcasting)
        {
            try
            {
                _udpClient.Send(data, data.Length, endPoint);
                Debug.Log($"브로드캐스트 송신: {myIP}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"브로드캐스트 오류: {e.Message}");
            }

            yield return new WaitForSeconds(broadcastInterval);
        }
    }

    string GetLocalIP()
    {
        try
        {
            using (var socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                return (socket.LocalEndPoint as IPEndPoint).Address.ToString();
            }
        }
        catch { return "127.0.0.1"; }
    }

    void OnDestroy()
    {
        StopBroadcast();
    }
}
