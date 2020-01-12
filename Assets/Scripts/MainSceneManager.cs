using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MainSceneManager : MonoBehaviour {

    #region const
    public enum ActionPattern{
        kNone,
        kMove1,
        kMove2,
        kMove3
    }

    public enum TurnPhaseState{
        kCardSelect,
        kMoveSelect,
        kWait,
        kStageUpdate
    }

    public const int kStageSize = 6;
    private const float kPanelSize = 0.6f;
    #endregion

    #region SerializeField
    [SerializeField, Tooltip("PUN動作管理")]
    private PhotonNetworkManager _punManager;
    [SerializeField, Tooltip("動作判定用")]
    private ActionCheckManager _actionCheckManager;
    [SerializeField, Tooltip("インゲーム親要素")]
    private GameObject _gameWorld;
    [SerializeField, Tooltip("すレージプレハブ")]
    private GameObject _stagePrefab;
    [SerializeField, Tooltip("カードマネージャ")]
    private CardManager _cardManager;
    #endregion

    #region
    private bool _connectFlg;
    private List<List<StagePanel>> _stagePanelList = new List<List<StagePanel>>();
    private int _nowPosX      = kStageSize / 2 - 1;
    private int _nowPosY      = 0;
    private int _enemyNowPosX = kStageSize / 2;
    private int _enemyNowPosY = kStageSize - 1;
    private int _maxMoveCount = 0;
    private int _moveCount    = 0;
    private Vector2 _nowPos;
    private Vector2[] _movePos = new Vector2[3];    // 3歩が最大なので3 可変長だとSerializeの関係でエラーになるので
    private TurnPhaseState _turnPhaseState;
    #endregion

    #region Access
    public ActionCheckManager ActionCheckManager{
        get{return _actionCheckManager;}
    }
    public List<List<StagePanel>> StagePanelList{
        get{return _stagePanelList;}
    }
    public TurnPhaseState GetTurnPhaseState{
        get{return _turnPhaseState;}
    }
    #endregion

	// Use this for initialization
	void Start ()
    {
        _turnPhaseState = TurnPhaseState.kCardSelect;
		_punManager.Init(this);
        _actionCheckManager.Init(this);
        _cardManager.Init(this);
        _maxMoveCount = 3;
	}

    #region public function
    public void GameStart()
    {
        var halfSize    = kPanelSize / 2.0f;
        var startLeft   = kPanelSize * kStageSize / -2.0f + halfSize;
        var startBottom = startLeft;
        for(var i = 0; i < kStageSize; ++i){

            _stagePanelList.Add(new List<StagePanel>());
            for(var j = 0; j < kStageSize; ++j){

                var stagePanel = Instantiate(_stagePrefab, new Vector3(kPanelSize * j + startLeft, kPanelSize * i + startBottom, 0.0f) , Quaternion.identity, _gameWorld.transform).GetComponent<StagePanel>();
                stagePanel.Init(this, j, i);
                _stagePanelList[i].Add(stagePanel);
            }
        }

        // 色変更
        //SetNowPos(2, 0);
        _stagePanelList[_nowPosY][_nowPosX].SetState(StagePanel.State.kSelect);
        _nowPos    = new Vector2(_nowPosX, _nowPosY);
        _movePos   = new Vector2[3];
        _moveCount = 0;
        SetEnemyNowPos(_enemyNowPosX, _enemyNowPosY);
    }

    public void SetNowPosMoveStart()
    {
        Debug.Log("SetNowPosMoveStart");
        ChoicePanelSearch(_nowPosX, _nowPosY);
        //SetNowPos(_nowPosX, _nowPosY);
        _nowPos    = new Vector2(_nowPosX, _nowPosY);
        _movePos   = new Vector2[3];
        _moveCount = 0;
    }

    /// <summary>
    /// 現在の位置設定
    /// </summary>
    /// <param name="posX"></param>
    /// <param name="posY"></param>
    public void SetNowPos(int posX, int posY)
    {
        Debug.Log("SetNowPos");
        // 特定の状態ならリターン
        // FIXME: 見た目に変化が無いとエラーっぽいよね
        switch(_turnPhaseState){
        case TurnPhaseState.kCardSelect:
        case TurnPhaseState.kWait:
            return;
        }
        Debug.Log("SetNowPos2");



        _stagePanelList[_nowPosY][_nowPosX].SetState(StagePanel.State.kSelect);
        _nowPosX = posX;
        _nowPosY = posY;
        _movePos[_moveCount] = new Vector2(_nowPosX, _nowPosY);
        ChoicePanelSearch(posX, posY);
        // _stagePanelList[posY][posX].SetState(StagePanel.State.kNowPos);
        // ChoiceReset();
        // if(posX-1 > -1        ) _stagePanelList[posY  ][posX-1].CheckSetState(StagePanel.State.kChoice);
        // if(posY-1 > -1        ) _stagePanelList[posY-1][posX  ].CheckSetState(StagePanel.State.kChoice);
        // if(posX+1 < kStageSize) _stagePanelList[posY  ][posX+1].CheckSetState(StagePanel.State.kChoice);
        // if(posY+1 < kStageSize) _stagePanelList[posY+1][posX  ].CheckSetState(StagePanel.State.kChoice);

        CheckMoveMax();
    }

    /// <summary>
    /// 選択できるパネルをサーチ
    /// </summary>
    /// <param name="posX"></param>
    /// <param name="posY"></param>
    private void ChoicePanelSearch(int posX, int posY)
    {
        _stagePanelList[posY][posX].SetState(StagePanel.State.kNowPos);
        ChoiceReset();
        if(posX-1 > -1        ) _stagePanelList[posY  ][posX-1].CheckSetState(StagePanel.State.kChoice);
        if(posY-1 > -1        ) _stagePanelList[posY-1][posX  ].CheckSetState(StagePanel.State.kChoice);
        if(posX+1 < kStageSize) _stagePanelList[posY  ][posX+1].CheckSetState(StagePanel.State.kChoice);
        if(posY+1 < kStageSize) _stagePanelList[posY+1][posX  ].CheckSetState(StagePanel.State.kChoice);
    }

    /// <summary>
    /// 相手の現在座標を表示
    /// </summary>
    /// <param name="posX"></param>
    /// <param name="posY"></param>
    public void SetEnemyNowPos(int posX, int posY)
    {
        _enemyNowPosX = posX;
        _enemyNowPosY = posY;
        _stagePanelList[_enemyNowPosY][_enemyNowPosX].SetState(StagePanel.State.kEnemyNowPos);
    }

    /// <summary>
    /// マップ更新
    /// </summary>
    /// <param name="hostMoveList"></param>
    /// <param name="guestMoveList"></param>
    /// <param name="hostPanelList"></param>
    /// <param name="guestPanelList"></param>
    public void UpdatePanel(Vector2[] hostMoveList, Vector2[] guestMoveList, int[] hostPanelList, int[] guestPanelList)
    {
        Debug.Log("panelLength:"+hostPanelList.Length);

        // 反転
        Debug.Log("PlayerID:"+_punManager.PlayerID);

        // 遅延処理とかを入れ始めてると後からの方がいいケどとりあえずここで更新
        SetTurnPhaseState(TurnPhaseState.kStageUpdate);

        Debug.Log("nowPos:"+_nowPosX+","+_nowPosY);
        Debug.Log("enemyNowPos:"+_enemyNowPosX+","+_enemyNowPosY);
        GameObject.Find("PlayerIDText").GetComponent<Text>().text = _punManager.PlayerID.ToString();
        if(_punManager.PlayerID == 1){
            


            for(var i = 0; i < guestPanelList.Length; ++i){
                if((StagePanel.State)guestPanelList[i] == StagePanel.State.kPlayer){
                    guestPanelList[i] = (int)StagePanel.State.kEnemy;
                }
            }
        }
        else{
            hostMoveList  = MoveReverse(hostMoveList);
            guestMoveList = MoveReverse(guestMoveList);
            var tmpList    = hostMoveList;
            hostMoveList   = guestMoveList;
            guestMoveList  = tmpList;
            var tmpPanel   = hostPanelList;
            hostPanelList  = guestPanelList;
            guestPanelList = tmpPanel;


            // for(var i = 0; i < hostPanelList.Length; ++i){
            //     if((StagePanel.State)hostPanelList[i] == StagePanel.State.kPlayer){
            //         hostPanelList[i] = (int)StagePanel.State.kEnemy;
            //     }
            // }
            for(var i = 0; i < guestPanelList.Length; ++i){
                if((StagePanel.State)guestPanelList[i] == StagePanel.State.kPlayer){
                    guestPanelList[i] = (int)StagePanel.State.kEnemy;
                }
            }
        }
        // 選択していたパネル初期化
        SelectPanelReset();
        // 反映
        var hostSavePosX = -1;
        var hostSavePosY = -1;
        var posX     = 0;
        var posY     = 0;
        // ホスト
        for(var i = 0; i < hostMoveList.Length; ++i){

            Debug.Log((StagePanel.State)hostPanelList[i]);
            if((StagePanel.State)hostPanelList[i] == StagePanel.State.kNone) break;
            if(hostMoveList[i] == null)break;
            posX = (int)hostMoveList[i].x;
            posY = (int)hostMoveList[i].y;
            // ロックだったら保存しない
            var panelState = (StagePanel.State)hostPanelList[i];
            if(panelState != StagePanel.State.kLock && panelState != StagePanel.State.kEnemy){
                hostSavePosX = posX;
                hostSavePosY = posY;
            }
            Debug.Log("host_pos:"+posX+","+posY);
            _stagePanelList[posY][posX].CheckSetState((StagePanel.State)hostPanelList[i]);
        }
        if(/*_punManager.PlayerID == 1 && */hostSavePosX > -1){
            _nowPosX = hostSavePosX;
            _nowPosY = hostSavePosY;
        }
        // ゲスト
        var guestSavePosX = -1;
        var guestSavePosY = -1;
        for(var i = 0; i < guestMoveList.Length; ++i){

            if((StagePanel.State)guestPanelList[i] == StagePanel.State.kNone) break;
            if(guestMoveList[i] == null)break;
            posX = (int)guestMoveList[i].x;
            posY = (int)guestMoveList[i].y;
            var panelState = (StagePanel.State)guestPanelList[i];
            if(panelState != StagePanel.State.kLock &&  panelState != StagePanel.State.kPlayer){
                guestSavePosX = posX;
                guestSavePosY = posY;
            }
            Debug.Log("guest_pos:"+posX+","+posY);
            _stagePanelList[posY][posX].CheckSetState((StagePanel.State)guestPanelList[i]);            
        }
        if(guestSavePosX > -1){
            SetEnemyNowPos(guestSavePosX, guestSavePosY);
        }

        Debug.Log("nowPos:"+_nowPosX+","+_nowPosY);
        Debug.Log("enemyNowPos:"+_enemyNowPosX+","+_enemyNowPosY);


        GameObject.Find("PlayerPosText").GetComponent<Text>().text = _nowPosX+","+_nowPosY;

        // カード選択フェーズに戻る
        SetTurnPhaseState(TurnPhaseState.kCardSelect);

        PlayerActionReset();
    }

    public void SetTurnPhaseState(TurnPhaseState turnPhaseState)
    {
        _turnPhaseState = turnPhaseState;
        switch(_turnPhaseState){
        case TurnPhaseState.kCardSelect:
            _cardManager.ReStartCardSelectPhase();
            break;
        case TurnPhaseState.kMoveSelect:
            //SetNowPosMoveStart();
            break;
        case TurnPhaseState.kWait:
            break;
        case TurnPhaseState.kStageUpdate:

            break;
        }
    }

    public void SetMaxMoveCount(int setMoveCount)
    {
        _maxMoveCount = setMoveCount;
    }

    /// <summary>
    /// 行動準備状態に戻す
    /// </summary>
    private void PlayerActionReset()
    {
        _moveCount = 0;
        SetNowPos(_nowPosX, _nowPosY);
        _nowPos  = new Vector2(_nowPosX, _nowPosY);
        _movePos = new Vector2[3];
        _moveCount = 0;
    }

    private Vector2[] MoveReverse(Vector2[] moveData)
    {
        for(var i = 0; i < moveData.Length; ++i){

            if(moveData[i] == null)break;
            moveData[i] = new Vector2(kStageSize - 1 - (int)moveData[i].x, kStageSize - 1 - (int)moveData[i].y);
        }
        return moveData;
    }


    /// <summary>
    /// 選択候補リセット
    /// </summary>
    private void ChoiceReset()
    {
        for(var i = 0; i < kStageSize; ++i){
            for(var j = 0; j < kStageSize; ++j){
                _stagePanelList[i][j].CheckChoiceReset();
            }
        }
    }

    /// <summary>
    /// 選択したパネルをリセット
    /// </summary>
    private void SelectPanelReset()
    {
        for(var i = 0; i < kStageSize; ++i){
            for(var j = 0; j < kStageSize; ++j){
                _stagePanelList[i][j].CheckSelectPanelReset();
            }
        }
    }


    /// <summary>
    /// 移動完了チェック
    /// </summary>
    private void CheckMoveMax()
    {
        if(_maxMoveCount == 0) return;
        ++_moveCount;
        Debug.Log(_maxMoveCount);
        Debug.Log(_moveCount);
        if(_moveCount < _maxMoveCount) return;
        //PhotonNetwork.RaiseEvent( (byte)RaiseEventType.kActionChoice, "test", true, RaiseEventOptions.Default );
        byte evCode = 1; // Custom Event 1: Used as "MoveUnitsToTargetPosition" event
        // 送る中身 プレイヤーID、行動、行動前座標、移動配列、最大行動回数
        object[] content = new object[] {_punManager.PlayerID, ActionPattern.kMove3, _nowPos, _movePos, _maxMoveCount}; // Array contains the target position and the IDs of the selected units
        // 対象
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        // ?

        Debug.Log("send");
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
        Debug.Log("send");

        // 選択不可に
        ChoiceReset();
        SetTurnPhaseState(TurnPhaseState.kWait);
    }
    #endregion
}
