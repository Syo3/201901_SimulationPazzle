using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionCard : MonoBehaviour {

    public enum ActionPattern{
        kNone,
        kMove1,
        kMove2,
        kMove3
    }

    #region SerializeField
    [SerializeField, Tooltip("")]
    private ActionPattern _actionPattern;
    [SerializeField, Tooltip("Image")]
    private Image _image;
    #endregion

    #region 
    private CardManager _cardManager;
    private bool _activeFlg;
    #endregion

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="cardManager"></param>
    public void Init(CardManager cardManager)
    {
        _cardManager = cardManager;
        SetTapActive(true);
    }

    /// <summary>
    /// タップされた場合
    /// </summary>
    public void OnClick()
    {
        _cardManager.SetSelectCard(_actionPattern);
        Debug.Log(_actionPattern);
    }

    /// <summary>
    /// タップ可能か設定
    /// </summary>
    /// <param name="flg"></param>
    public void SetTapActive(bool flg)
    {
        // FIXME: 後から変更する 色はそのままだけどあれなやつが欲しい

        _activeFlg           = flg;
        _image.color         = _activeFlg ? Color.white : Color.gray;
        _image.raycastTarget = _activeFlg;
    }
}
