using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour {

    #region SerializeField
    [SerializeField, Tooltip("カード一覧")]
    private List<ActionCard> _actionCardList;
    #endregion

    #region 
    private MainSceneManager _sceneManager;
    private ActionCard.ActionPattern _actionPattern;
    #endregion

    #region access
    public MainSceneManager SceneManager{
        get{return _sceneManager;}
    }
    public ActionCard.ActionPattern ActionPattern{
        get{return _actionPattern;}
    }
    #endregion

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="sceneManager"></param>
    public void Init(MainSceneManager sceneManager)
    {
        _sceneManager = sceneManager;
        for(var i = 0; i < _actionCardList.Count; ++i){
            _actionCardList[i].Init(this);
        }
    }

    /// <summary>
    /// カード選択フェーズ開始
    /// </summary>
    public void ReStartCardSelectPhase()
    {
        SetAllCardActive(true);
    }

    /// <summary>
    /// 選択したカードによって行うアクションを決める
    /// </summary>
    /// <param name="pattern"></param>
    public void SetSelectCard(ActionCard.ActionPattern pattern)
    {
        _actionPattern = pattern;
        switch(pattern){
        case ActionCard.ActionPattern.kMove1:
            _sceneManager.SetMaxMoveCount(1);
            break;
        case ActionCard.ActionPattern.kMove2:
            _sceneManager.SetMaxMoveCount(2);
            break;
        case ActionCard.ActionPattern.kMove3:
            _sceneManager.SetMaxMoveCount(3);
            break;        
        }
        _sceneManager.SetTurnPhaseState(MainSceneManager.TurnPhaseState.kMoveSelect);

        switch(pattern){
        case ActionCard.ActionPattern.kMove1:
        case ActionCard.ActionPattern.kMove2:
        case ActionCard.ActionPattern.kMove3:
            _sceneManager.SetNowPosMoveStart();
            break;        
        }

        SetAllCardActive(false);
    }

    /// <summary>
    /// 全カードにアクティブ設定
    /// </summary>
    /// <param name="setFlg"></param>
    private void SetAllCardActive(bool setFlg)
    {
        for(var i = 0; i < _actionCardList.Count; ++i){
            _actionCardList[i].SetTapActive(setFlg);
        }
    }    
}
