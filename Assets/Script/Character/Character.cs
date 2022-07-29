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
            CCharacterData cd = data.mChaDatas[mGuid];
            //------------------------------------------------
            transform.position = cd.pos;
            mSpeed = cd.speed;
            mDir = cd.dir;
            mControlRole = cd.controlRole;
            mMS.mStates = cd.states;
            //------------------------------------------------
            mSpeed = cd.speed;
        }
    }

    public void saveData(GameData data)
    {
        CCharacterData chaData = new CCharacterData();
        //------------------------------------------------
        chaData.pos = transform.position;
        chaData.speed = mSpeed;
        chaData.dir = mDir;
        chaData.controlRole = mControlRole;

        chaData.states = mMS.mStates;
        //------------------------------------------------
        data.mChaDatas[mGuid] = chaData;
    }
}

public class CCharacterData
{
    public Vector3 pos;
    public float speed;
    public DIR dir;
    public bool controlRole;
    public Stack<CState> states;
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

    public virtual void init(params object[] ps) { }
    public virtual void unInit() { }
    public virtual void update(params object[] ps) { }
}
public class CIdleState : CState
{
    public List<int> mSpriteIndex = new List<int>() { 0, 1 };
    public int mCurSpriteIndex = 0;
    public CIdleState() : base(STATE.IDLE_STATE)
    { }
    public override void init(params object[] ps)
    {
        Character cha = ps[0] as Character;
        mCurSpriteIndex = 0;
        cha.GetComponent<SpriteRenderer>().sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];
    }

    public override void update(params object[] ps)
    {
        mTimePast += Time.deltaTime;

        Character cha = ps[0] as Character;
        SpriteRenderer sr = cha.GetComponent<SpriteRenderer>();

        if (cha.mControlRole && CHelp.hasHit())
        {
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CMachineState.getMS().gotoState(STATE.WALK_STATE, false, new CWalkParam(cha, new Vector2(touchPos.x, touchPos.y)));
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

public class CWalkDir
{
    public Vector2 tarPos;
    public DIR dir;
    public CWalkDir(Vector2 v, DIR d)
    {
        tarPos = v;
        dir = d;
    }
}
public class  CWalkState : CState
{
    public List<int> mSpriteIndex = new List<int>() { 0, 1, 2, 3 };
    public int mCurSpriteIndex = 0;
    public Queue<CWalkDir> mTarPos = new Queue<CWalkDir>();
    public CWalkState() : base(STATE.WALK_STATE) { }
    public override void init(params object[] ps)
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
            mTarPos.Enqueue(new CWalkDir(new Vector2(tar.x, pos.y), tar.x > pos.x ? DIR.RIGHT : DIR.LEFT));
            mTarPos.Enqueue(new CWalkDir(new Vector2(tar.x, tar.y), tar.y > pos.y ? DIR.TOP : DIR.DOWN));
        }
        else
        {
            mTarPos.Enqueue(new CWalkDir(new Vector2(pos.x, tar.y), tar.y > pos.y ? DIR.TOP : DIR.DOWN));
            mTarPos.Enqueue(new CWalkDir(new Vector2(tar.x, tar.y), tar.x > pos.x ? DIR.RIGHT : DIR.LEFT));
        }

    }
    public override void update(params object[] ps)
    {
        mTimePast += Time.deltaTime;

        Character cha = ps[0] as Character;

        if (cha.mControlRole && CHelp.hasHit())
        {
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            gotoTarPos(cha.transform.position, new Vector2(touchPos.x, touchPos.y));
        }

        if (mTarPos.Count <= 0)
        {
            CMachineState.getMS().gotoState(STATE.IDLE_STATE, false, cha);
            return;
        }

        CWalkDir wd = mTarPos.Peek();
        Vector2 tarDir = new Vector2(wd.tarPos.x - cha.transform.position.x, wd.tarPos.y - cha.transform.position.y);
        tarDir.Normalize();

        Vector2 dp = tarDir * cha.mSpeed * Time.deltaTime;
        cha.transform.position += new Vector3(dp.x, dp.y, 0);

        if (Mathf.Abs(cha.transform.position.x - wd.tarPos.x) <= 0.01 && Mathf.Abs(cha.transform.position.y - wd.tarPos.y) < 0.01)
        {
            cha.transform.position = new Vector3(wd.tarPos.x, wd.tarPos.y, cha.transform.position.z);
            mTarPos.Dequeue();
        }

        SpriteRenderer sr = cha.GetComponent<SpriteRenderer>();
        if (cha.mDir != wd.dir)
        {
            cha.mDir = wd.dir;
            mCurSpriteIndex = 0;
            mTimePast = 0f;
            sr.sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];
        }
        if (mTimePast > 0.25f)
        {
            mTimePast = 0f;
            mCurSpriteIndex = (mCurSpriteIndex + 1) % mSpriteIndex.Count;
            sr.sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];
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