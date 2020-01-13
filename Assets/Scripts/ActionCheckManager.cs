using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ActionCheckManager : MonoBehaviour {

    #region const
    public enum WinState{
        kNone,
        kHostWin,
        kGuestWin,
        kDraw
    }
    #endregion

    #region
    private MainSceneManager _sceneManager;
    private object[] _hostObject;
    private object[] _guestObject;
    #endregion

    public void Init(MainSceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }

    /// <summary>
    /// 行動オブジェクトを保存
    /// </summary>
    /// <param name="actionObject"></param>
    public void SetActionObject(object[] actionObject)
    {
        // ホスト
        if((int)actionObject[0] == 1){
            _hostObject = actionObject;
        }
        // ゲスト
        else{
            _guestObject = actionObject;
        }
        // 判定
        if(_hostObject != null && _guestObject != null){
            CheckAction();
        }
    }

    /// <summary>
    /// 行動を判定
    /// </summary>
    private void CheckAction()
    {
        // 最大行動回数取得
        var hostMoveMax  = (int)_hostObject[4];
        var guestMoveMax = (int)_guestObject[4];
        // FIXME: int変換時に桁落ちしそう
        // ゲストデータの座標反転 判定用に反転の必要がある
        var guestPos     = (Vector2)_guestObject[2];
        var guestMovePos = (Vector2[])_guestObject[3];
        var stageSize    = MainSceneManager.kStageSize - 1;
        var guestPosX    = stageSize - (int)guestPos.x;
        var guestPosY    = stageSize - (int)guestPos.y;
        guestPos         = new Vector2(guestPosX, guestPosY);
        for(var i = 0; i < guestMovePos.Length; ++i){
            
            if(guestMovePos[i] == null) break;
            guestMovePos[i] = new Vector2(stageSize - (int)guestMovePos[i].x, stageSize - (int)guestMovePos[i].y);
        }
        var hostPos     = (Vector2)_hostObject[2];
        var hostMovePos = (Vector2[])_hostObject[3];
        var hostPosX    = (int)hostPos.x;
        var hostPosY    = (int)hostPos.y;
        var stagePanelList = _sceneManager.StagePanelList;

        // 判定開始
        var hostMoveList = new List<Vector2>();
        var guestMoveList = new List<Vector2>();
        var hostPanelList = new List<int>();
        var guestPanelList = new List<int>();
        hostMoveList.Add(hostPos);
        guestMoveList.Add(guestPos);
        hostPanelList.Add((int)StagePanel.State.kPlayer);
        guestPanelList.Add((int)StagePanel.State.kPlayer);
        var hostMoveFlg  = true;
        var guestMoveFlg = true;
        for(var i = 0; i < hostMovePos.Length; ++i){

            // ホスト
            if(hostMovePos[i] != null && i < hostMoveMax){
                hostPosX  = (int)hostMovePos[i].x;
                hostPosY  = (int)hostMovePos[i].y;
            }
            else{
                hostMoveFlg = false;
            }
            // ゲスト
            if(guestMovePos[i] != null && i < guestMoveMax){
                guestPosX = (int)guestMovePos[i].x;
                guestPosY = (int)guestMovePos[i].y;
            }
            else{
                guestMoveFlg = false;
            }
            // 衝突判定
            if(hostMoveFlg && guestMoveFlg && hostPosX == guestPosX && hostPosY == guestPosY){
                hostMoveList.Add(hostMovePos[i]);
                hostPanelList.Add((int)StagePanel.State.kLock);
                guestMoveList.Add(guestMovePos[i]);
                guestPanelList.Add((int)StagePanel.State.kLock);
                break;
            }

            // ホスト移動判定
            if(hostMoveFlg){
                if(CheckMovePanel(hostPosX, hostPosY)){
                    stagePanelList[hostPosY][hostPosX].SetState(StagePanel.State.kPlayer);
                    hostMoveList.Add(hostMovePos[i]);
                    hostPanelList.Add((int)StagePanel.State.kPlayer);
                }
                else{
                    hostMoveFlg = false;
                }
            }
            // ゲスト移動判定
            if(guestMoveFlg){
                if(CheckMovePanel(guestPosX, guestPosY)){
                    // stagePanelList[guestPosY][guestPosX].SetState(StagePanel.State.kPlayer);
                    stagePanelList[guestPosY][guestPosX].SetState(StagePanel.State.kEnemy);
                    guestMoveList.Add(guestMovePos[i]);
                    guestPanelList.Add((int)StagePanel.State.kPlayer);
                }
                else{
                    guestMoveFlg = false;
                }
            }
        }

        
        var winnerFlg = CheckWinner();
        var winState  = WinState.kNone;
        // 勝敗計算
        if(winnerFlg){
            winState = CheckPanelCount(stagePanelList);
        }
        // 送信
        byte evCode = 2; // Custom Event 1: Used as "MoveUnitsToTargetPosition" event
        // 送る中身 プレイヤーID、行動、行動前座標、移動配列、etc
        object[] content = new object[] {hostMoveList.ToArray(), guestMoveList.ToArray(), hostPanelList.ToArray(), guestPanelList.ToArray(), (int)winState}; // Array contains the target position and the IDs of the selected units
        // 対象
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        // ?

        Debug.Log("send");
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
        Debug.Log("send");


        // オブジェクト初期化
        _guestObject = null;
        _hostObject  = null;
    }

    /// <summary>
    /// 移動可能判定
    /// </summary>
    /// <param name="posX"></param>
    /// <param name="posY"></param>
    /// <returns></returns>
    private bool CheckMovePanel(int posX, int posY)
    {
        var stagePanelList = _sceneManager.StagePanelList;
        switch(stagePanelList[posY][posX].PanelState){
        case StagePanel.State.kPlayer:
        case StagePanel.State.kEnemy:
        case StagePanel.State.kLock:
            return false;
        }
        return true;
    }

    /// <summary>
    /// 勝敗判定
    /// </summary>
    /// <returns></returns>
    private bool CheckWinner()
    {
        return _hostObject.Length > 5 && (bool)_hostObject[5] && _guestObject.Length > 5 && (bool)_guestObject[5];
    }

    /// <summary>
    /// 勝敗判定
    /// </summary>
    /// <param name="stagePanelList"></param>
    /// <returns></returns>
    private WinState CheckPanelCount(List<List<StagePanel>> stagePanelList)
    {
        // パネル枚数を計算
        var hostPanelCounter  = 0;
        var guestPanelCounter = 0;
        for(var i = 0; i < stagePanelList.Count; ++i){
            var tmpText = "";
            for(var j = 0; j < stagePanelList[i].Count; ++j){

                switch(stagePanelList[i][j].PanelState){
                case StagePanel.State.kPlayer:
                case StagePanel.State.kNowPos:
                case StagePanel.State.kSelect:
                    ++hostPanelCounter;
                    break;
                case StagePanel.State.kEnemy:
                case StagePanel.State.kEnemyNowPos:
                    ++guestPanelCounter;
                    break;
                }
                tmpText += stagePanelList[i][j].PanelState+",";
            }
            Debug.Log(tmpText);
        }
        Debug.Log("win check:"+hostPanelCounter+","+guestPanelCounter);
        if(hostPanelCounter != guestPanelCounter){
            return hostPanelCounter > guestPanelCounter ? WinState.kHostWin : WinState.kGuestWin;
        }
        return WinState.kDraw;
    }
}
