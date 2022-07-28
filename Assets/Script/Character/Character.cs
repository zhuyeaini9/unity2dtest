using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour, ISaveLoad
{
    public Sprite[] mSprites;
    public string mSpriteSheetName = "";
    public string mGuid = "";
    [ContextMenu("gen guid")]
    void genGuid()
    {
        mGuid = System.Guid.NewGuid().ToString();
    }
    [ContextMenu("gen sprites")]
    void genSprites()
    {
        string spriteSheetName = "";
        if (mSpriteSheetName == "")
        {
            spriteSheetName = name;
        }
        mSprites = Resources.LoadAll<Sprite>(string.Format("Characters/{0}/{0}", name, name));
    }
    Rigidbody2D mRigidbody;
    //----------------------------------------------
    // need save property
    //----------------------------------------------
    CMachineState mMS;
    public float mSpeed = 1;
    public DIR mDir = DIR.DOWN;
    public bool mControlRole = false;
    void Awake()
    {
        mMS = CMachineState.getMS();
    }
    // Start is called before the first frame update
    void Start()
    {
        mRigidbody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mMS.mStates.Count <= 0)
            mMS.gotoState(STATE.IDLE_STATE, false, this);
        mMS.update(this);
    }

    public void updateMove(Vector2 mov)
    {
        toOneDir(ref mov);
        //下面的方式会引起碰撞后无限移动
        //mRigidbody.velocity = mov * mSpeed;

        setDir(mov);

        Vector2 dp = mov * mSpeed * Time.fixedDeltaTime;
        transform.position += new Vector3(dp.x, dp.y, 0);
    }

    void toOneDir(ref Vector2 mov)
    {
        if (mov.x > 0 || mov.x < 0)
        {
            mov.y = 0;
            return;
        }
        if (mov.y > 0 || mov.y < 0)
        {
            mov.x = 0;
            return;
        }
    }

    void setDir(Vector2 mov)
    {
        if (mov.x != 0 || mov.y != 0)
        {
            if (mov.y > 0)
                mDir = DIR.TOP;
            if (mov.y < 0)
                mDir = DIR.DOWN;

            if (mov.x > 0)
                mDir = DIR.RIGHT;
            if (mov.x < 0)
                mDir = DIR.LEFT;
        }
    }

    public void loadData(GameData data)
    {
        if (data.mChaDatas.ContainsKey(mGuid))
        {
            mSpeed = data.mChaDatas[mGuid].speed;
        }
    }

    public void saveData(GameData data)
    {
        CCharacterData chaData = new CCharacterData();
        chaData.speed = mSpeed;
        data.mChaDatas[mGuid] = chaData;
    }
}


[System.Serializable]
public class CCharacterData
{
    public float speed;
}

public enum DIR
{
    DOWN,
    LEFT,
    RIGHT,
    TOP,
}
public enum STATE
{
    IDLE_STATE,
    WALK_STATE,
    ATTACK_STATE,
    DEAD_STATE,
}
public class CState
{
    public CState(STATE s)
    {
        mState = s;
    }
    public STATE mState;
    public float mTimePast = 0f;

    public virtual void init(params Object[] ps) { }
    public virtual void unInit() { }
    public virtual void update(params Object[] ps) { }
}
public class CIdleState : CState
{
    List<int> mSpriteIndex = new List<int>() { 0, 1 };
    int mCurSpriteIndex = 0;
    DIR mLastDir;
    public CIdleState() : base(STATE.IDLE_STATE)
    { }
    public override void init(params Object[] ps)
    {
        Character cha = ps[0] as Character;
        mLastDir = cha.mDir;
        mCurSpriteIndex = 0;
        cha.GetComponent<SpriteRenderer>().sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];
    }

    public override void update(params Object[] ps)
    {
        mTimePast += Time.deltaTime;

        Character cha = ps[0] as Character;
        SpriteRenderer sr = cha.GetComponent<SpriteRenderer>();

        if (Input.GetMouseButtonUp(0))
        {
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CMachineState.getMS().gotoState(STATE.WALK_STATE, false, new CWalkParam(cha, new Vector2(touchPos.x, touchPos.y)));
        }

        if (cha.mDir != mLastDir)
        {
            mLastDir = cha.mDir;
            mCurSpriteIndex = 0;
            sr.sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];

        }
        if (mTimePast > 0.5f)
        {
            mTimePast = 0f;
            mCurSpriteIndex = (mCurSpriteIndex + 1) % mSpriteIndex.Count;
            sr.sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];
        }
    }

}

public class CWalkParam : Object
{
    public Character cha;
    public Vector2 tarPos;
    public CWalkParam(Character c, Vector2 tp)
    {
        cha = c;
        tarPos = tp;
    }
}
public class CWalkState : CState
{
    List<int> mSpriteIndex = new List<int>() { 0, 1 };
    int mCurSpriteIndex = 0;
    DIR mLastDir;
    Queue<Vector2> mTarPos = new Queue<Vector2>();
    public CWalkState() : base(STATE.WALK_STATE) { }
    public override void init(params Object[] ps)
    {
        CWalkParam wp = ps[0] as CWalkParam;
        gotoTarPos(wp.cha.transform.position, wp.tarPos);
    }
    void gotoTarPos(Vector3 pos, Vector2 tar, bool append = false)
    {
        if (!append)
        {
            for (int i = 0; i < 2; i++)
            {
                if (mTarPos.Count > 0)
                    mTarPos.Dequeue();
            }
        }

        float dif_x = Mathf.Abs(pos.x - tar.x);
        float dif_y = Mathf.Abs(pos.y - tar.y);

        if (dif_x < dif_y)
        {
            mTarPos.Enqueue(new Vector2(tar.x, pos.y));
            mTarPos.Enqueue(new Vector2(tar.x, tar.y));
        }
        else
        {
            mTarPos.Enqueue(new Vector2(pos.x, tar.y));
            mTarPos.Enqueue(new Vector2(tar.x, tar.y));
        }

    }
    public override void update(params Object[] ps)
    {
        Character cha = ps[0] as Character;

        if (Input.GetMouseButtonUp(0))
        {
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            gotoTarPos(cha.transform.position, new Vector2(touchPos.x, touchPos.y));
        }

        if (mTarPos.Count <= 0)
        {
            CMachineState.getMS().gotoState(STATE.IDLE_STATE, false, cha);
            return;
        }

        Vector2 tarPos = mTarPos.Peek();
        Vector2 tarDir = new Vector2(tarPos.x - cha.transform.position.x, tarPos.y - cha.transform.position.y);
        tarDir.Normalize();

        Vector2 dp = tarDir * cha.mSpeed * Time.deltaTime;
        cha.transform.position += new Vector3(dp.x, dp.y, 0);

        if (Mathf.Abs(cha.transform.position.x - tarPos.x) <= 0.01 && Mathf.Abs(cha.transform.position.y - tarPos.y) < 0.01)
        {
            cha.transform.position = new Vector3(tarPos.x, tarPos.y, cha.transform.position.z);
            mTarPos.Dequeue();
        }
    }
}

public class CAttackState : CState
{
    public CAttackState() : base(STATE.ATTACK_STATE) { }
}
public class CMachineState
{
    public static CMachineState mSharedInstance = null;
    public Stack<CState> mStates = new Stack<CState>();

    public static CMachineState getMS()
    {
        if (mSharedInstance == null)
            mSharedInstance = new CMachineState();

        return mSharedInstance;
    }
    public void gotoState(STATE newState, bool savePreState = false, params Object[] ps)
    {
        if (mStates.Count > 0)
            mStates.Peek().unInit();

        CState ns = geneState(newState);
        ns.init(ps);

        if (savePreState)
        {
            mStates.Push(ns);
        }
        else
        {
            if (mStates.Count > 0)
                mStates.Pop();
            mStates.Push(ns);
        }
    }
    CState geneState(STATE s)
    {
        if (s == STATE.IDLE_STATE)
        {
            return new CIdleState();
        }
        if (s == STATE.WALK_STATE)
        {
            return new CWalkState();
        }
        if (s == STATE.ATTACK_STATE)
        {
            return new CAttackState();
        }

        return null;
    }

    public void update(params Object[] ps)
    {
        if (mStates.Count > 0)
            mStates.Peek().update(ps);
    }
}