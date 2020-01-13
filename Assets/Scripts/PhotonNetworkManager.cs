using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;


public class PhotonNetworkManager : MonoBehaviourPunCallbacks, IOnEventCallback {

    #region
    public enum RaiseEventType{
        kDefault,
        kActionChoice,
        kStageDraw,
    }
    #endregion

    #region SerializeField
    [SerializeField, Tooltip("")]
    private GameObject _photonView;
    #endregion

    #region 
    private MainSceneManager _sceneManager;
    private int _playerID;
    #endregion

    public int PlayerID{
        get{return _playerID;}
    }

    public void Init(MainSceneManager sceneManager)
    {
        _sceneManager = sceneManager;
        PhotonNetwork.ConnectUsingSettings();
        // イベント登録
        //PhotonNetwork.OnEventCall += OnRaiseEvent;
    }

    // マスターサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnConnectedToMaster() {

        var roomOptions        = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        // "room"という名前のルームに参加する（ルームが無ければ作成してから参加する）        
        PhotonNetwork.JoinOrCreateRoom("room", roomOptions, TypedLobby.Default);
    }

    // マッチングが成功した時に呼ばれるコールバック
    public override void OnJoinedRoom()
    {
        Debug.Log("ルーム入室");
        // マッチング後、ランダムな位置に自分自身のネットワークオブジェクトを生成する
        //var v = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f));
        PhotonNetwork.Instantiate("Prefabs/PhotonView", Vector3.zero, Quaternion.identity);
        _sceneManager.GameStart();
        // プレイヤーID作成
        _playerID = PhotonNetwork.PlayerList.Length;
    }

    /// <summary>
    /// ルームに別プレイヤーが入室
    /// </summary>
    /// <param name="newPlayer"></param>
    public override void OnPlayerEnteredRoom (Player newPlayer)
    {
        // ゲーム開始？
        // ゲストにもイベントを送信

		// マスタクライアントから開始命令を実行
		if(PhotonNetwork.PlayerList.Length == 2){
			// ルームの募集をオフにして途中入室不可に
			PhotonNetwork.CurrentRoom.IsOpen    = false;
			PhotonNetwork.CurrentRoom.IsVisible = false;
		}


    }



    // [PunRPC]属性をつけると、RPCでの実行が有効になる
    [PunRPC]
    private void FireProjectile(float angle) {
        //var projectile = Instantiate(projectilePrefab);
        //projectile.Init(transform.position, angle);

        // テスト
        //PhotonNetwork.RaiseEvent( (byte)RaiseEventType.kActionChoice, "test", true, RaiseEventOptions.Default );
    }

    /// <summary>
    /// データ受け取り
    /// </summary>
    /// <param name="eventcode"></param>
    /// <param name="content"></param>
    /// <param name="senderid"></param>
    // private void OnRaiseEvent( byte eventcode, object content, int senderid )
    // {
    //     Debug.Log(eventcode);
    //     Debug.Log(content);
    //     var eventType = (RaiseEventType)eventcode;
    //     switch( eventType ){
    //     // 開始アニメーション開始
    //     case RaiseEventType.kActionChoice:
    //         var contentList  = content.ToString().Split(':');
    //         var enemyRate    = contentList[1];
    //         break;
    //     default:
    //         break;
    //     }
    // }

    public void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        Debug.Log("Get Event Code");
        var eventType  = (RaiseEventType)eventCode;
        object[] data;
        switch(eventType){
        // 行動選択完了
        case RaiseEventType.kActionChoice:
            data = (object[])photonEvent.CustomData;
            Debug.Log(data[0]);
            Debug.Log(data[1]);
            Debug.Log(data[2]);
            Debug.Log(data[3]);
            _sceneManager.ActionCheckManager.SetActionObject(data);
            break;
        case RaiseEventType.kStageDraw:
            data = (object[])photonEvent.CustomData;
            Debug.Log(data[0]);
            Debug.Log(data[1]);
            Debug.Log(data[2]);
            Debug.Log(data[3]);
            // ステージ反映処理
            _sceneManager.UpdatePanel((Vector2[])data[0], (Vector2[])data[1], (int[])data[2], (int[])data[3], (int)data[4]);
            break;

        }

        //if (eventCode == MoveUnitsToTargetPositionEvent)
        //{
            // object[] data = (object[])photonEvent.CustomData;

            // Vector3 targetPosition = (Vector3)data[0];

            // for (int index = 1; index < data.Length; ++index)
            // {
            //     int unitId = (int)data[index];

            //     //UnitList[unitId].TargetPosition = targetPosition;
            // }
        //}
    }

    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
