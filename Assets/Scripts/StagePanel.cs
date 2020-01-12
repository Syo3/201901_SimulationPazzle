using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StagePanel : MonoBehaviour, IPointerClickHandler {

    public enum State{
        kNone,
        kPlayer,
        kEnemy,
        kNowPos,
        kEnemyNowPos,
        kSelect,
        kChoice,
        kLock,
    }

    #region SerializeField
    [SerializeField, Tooltip("")]
    private SpriteRenderer _sprite;
    [SerializeField, Tooltip("")]
    private BoxCollider2D _collider;
    #endregion


    #region private field
    private State _state;
    private MainSceneManager _sceneManager;
    private int _posX;
    private int _posY;
    #endregion

    #region
    public State PanelState{
        get{return _state;}
    }
    #endregion


    /// <summary>
    /// 初期化
    /// </summary>
    public void Init(MainSceneManager sceneManager, int posX, int posY)
    {
        _sceneManager = sceneManager;
        _state = State.kNone;
        _posX  = posX;
        _posY  = posY;
    }

    /// <summary>
    /// 状態設定
    /// </summary>
    /// <param name="state"></param>
    public void SetState(State state)
    {
        _state = state;
        SetColor();
    }

    /// <summary>
    /// 変更チェック　特定の状態の場合は変更しない
    /// </summary>
    /// <param name="state"></param>
    public void CheckSetState(State state)
    {
        // 洗濯中は選択候補にしない
        switch(_state){
        case State.kSelect:
        case State.kLock:
        case State.kPlayer:
        case State.kEnemy:
        case State.kEnemyNowPos:
            if(state == State.kChoice){
                return;
            }
            break;
        }
        SetState(state);
    }

    /// <summary>
    /// クリック時
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick ( PointerEventData eventData )
    {
        if(_state != State.kChoice) return;
        Debug.Log(_posX+","+_posY);
        _sceneManager.SetNowPos(_posX, _posY);
    }

    /// <summary>
    /// 選択初期化
    /// </summary>
    public void CheckChoiceReset()
    {
        SetState(_state == State.kChoice ? State.kNone : _state);
    }

    /// <summary>
    /// 選択したパネル初期化
    /// </summary>
    public void CheckSelectPanelReset()
    {
        SetState(_state == State.kSelect || _state == State.kNowPos ? State.kNone : _state);
    }

    /// <summary>
    /// 色反映
    /// </summary>
    private void SetColor()
    {
        switch(_state){
        case State.kNone:
            _sprite.color = Color.white;
            break;
        case State.kNowPos:
            _sprite.color = Color.cyan;
            break;
        case State.kEnemyNowPos:
            _sprite.color = Color.magenta;
            break;
        case State.kPlayer:
            _sprite.color = Color.blue;
            break;
        case State.kEnemy:
            _sprite.color = Color.red;
            break;
        case State.kChoice:
            _sprite.color = Color.green;
            break;
        case State.kSelect:
            _sprite.color = new Color(0.25f, 0.25f, 1.0f);
            break;
        case State.kLock:
            _sprite.color = Color.gray;
            break;
        }

    }
}
