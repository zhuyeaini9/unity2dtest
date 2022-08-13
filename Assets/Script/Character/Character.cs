using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
    public Rigidbody2D mRigidbody;
    public LineRenderer mLineRender;
    //----------------------------------------------
    // need save property
    //----------------------------------------------
    public CMachineState mMS = new CMachineState();
    public float mSpeed = 1;
    public DIR mDir = DIR.DOWN;
    public bool mControlRole = false;
    public STATE mInitState = STATE.IDLE_STATE;
    public float mPatrolDis = 5.0f;
    public Vector2 mBornPos;
    public CHATYPE mChaType = CHATYPE.NEUTRALITY;
    void Awake()
    {
    }
    // Start is called before the first frame update
    void Start()
    {
        mRigidbody = GetComponent<Rigidbody2D>();
        mLineRender = GetComponent<LineRenderer>();
        mLineRender.startWidth = 0.01f;
        mLineRender.endWidth = 0.01f;
        mLineRender.loop = true;
        mLineRender.material = new Material(Shader.Find("Sprites/Default"));
        mLineRender.startColor = new Color(255, 0, 0, 255);
        mLineRender.endColor = new Color(255, 0, 0, 255);
        mBornPos = new Vector2(transform.position.x, transform.position.y);
    }

    // Update is called once per frame
    void Update()
    {
        if (mMS.mStates.Count <= 0)
            mMS.gotoState(mInitState, false, this);
        mMS.update(this);
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
            if(mControlRole)
            {
                Camera.main.GetComponent<PixelPerfectCam>().SendMessage("followCha", this);
            }

            mMS.mStates = cd.states;
            foreach (var v in mMS.mStates)
                v.startRoutine(this);

            mInitState = cd.initState;
            mPatrolDis = cd.patrolDis;
            mBornPos = cd.bornPos;
            //------------------------------------------------
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
        chaData.initState = mInitState;
        chaData.patrolDis = mPatrolDis;
        chaData.bornPos = mBornPos;
        //------------------------------------------------
        data.mChaDatas[mGuid] = chaData;
    }

    [CustomEditor(typeof(Character))]
    public class CChaDebugInfoPreview : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var ts = (Character)target;
            if (ts == null)
                return;

            if (ts.mMS == null)
                return;

            // some styling for the header, this is optional
            var bold = new GUIStyle();
            bold.fontStyle = FontStyle.Bold;
            GUILayout.Label("States:", bold);

            foreach (var item in ts.mMS.mStates)
            {
                GUILayout.Label(item.mState.ToString());
            }
        }
    }
}

public class CCharacterData
{
    public Vector3 pos;
    public float speed;
    public DIR dir;
    public bool controlRole;
    public Stack<CState> states;
    public STATE initState;
    public float patrolDis;
    public Vector2 bornPos;
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
    PATROL_STATE,
}
public enum CHATYPE
{
    NPC,
    NEUTRALITY,
    ENEMY,
    FRIEND,
}
public class CState
{
    public CState(STATE s)
    {
        mState = s;
    }
    public STATE mState;
    public float mTimePast = 0f;
    public bool mCancel = false;

    public virtual void init(params object[] ps) { }
    public virtual void unInit(params object[] ps) { mCancel = true; }
    public virtual void update(params object[] ps) { }

    public virtual void startRoutine(Character cha) { }
}
public class CPatrolState : CState
{
    public List<int> mSpriteIndex = new List<int>() { 0, 1, 2, 3 };
    public List<int> mSpriteIdleIndex = new List<int>() { 0, 1 };

    public int mCurSpriteIndex = 0;
    public Queue<CWalkDir> mTarPos = new Queue<CWalkDir>();

    public CPatrolState() : base(STATE.PATROL_STATE) { }
    public override void init(params object[] ps)
    {
        Character cha = ps[0] as Character;
        gotoTarPos(cha.transform.position, randomPos(cha));
        startRoutine(cha);
    }
    public override void startRoutine(Character cha)
    {
        cha.StartCoroutine(updateAni(cha));
        cha.StartCoroutine(updatePatrolPos(cha));
    }
    public override void unInit(params object[] ps)
    {
        base.unInit();
        Character cha = ps[0] as Character;
        cha.mLineRender.positionCount = 0;
    }
    public IEnumerator updatePatrolPos(Character cha)
    {
        while (!mCancel)
        {
            if (mTarPos.Count <= 0)
            {
                if (CHelp.hasPercent(30))
                {
                    gotoTarPos(cha.transform.position, randomPos(cha));
                }
            }
            yield return new WaitForSeconds(2.0f);
        }
    }
    public IEnumerator updateAni(Character cha)
    {
        SpriteRenderer sr = cha.GetComponent<SpriteRenderer>();
        while (!mCancel)
        {
            if (mTarPos.Count <= 0)
            {
                mCurSpriteIndex = (mCurSpriteIndex + 1) % mSpriteIdleIndex.Count;
                sr.sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIdleIndex[mCurSpriteIndex]];

                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                mCurSpriteIndex = (mCurSpriteIndex + 1) % mSpriteIndex.Count;
                sr.sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];

                yield return new WaitForSeconds(0.25f);
            }
        }
    }
    public Vector2 randomPos(Character cha)
    {
        float disx = cha.mBornPos.x + Random.Range(-cha.mPatrolDis, cha.mPatrolDis);
        float disy = cha.mBornPos.y + Random.Range(-cha.mPatrolDis, cha.mPatrolDis);
        return new Vector2(disx, disy);
    }
    void gotoTarPos(Vector3 pos, Vector2 tar)
    {
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

    void drawCircle(Character cha)
    {
        int steps = 40;
        cha.mLineRender.positionCount = steps;
        for (int currentStep = 0; currentStep < steps; currentStep++)
        {
            float circumPro = (float)currentStep / steps;
            float currRadian = circumPro * 2 * Mathf.PI;
            float xScaled = Mathf.Cos(currRadian);
            float yScaled = Mathf.Sin(currRadian);
            float x = xScaled * cha.mPatrolDis + cha.transform.position.x;
            float y = yScaled * cha.mPatrolDis + cha.transform.position.y;

            Vector3 cp = new Vector3(x, y, 0);
            cha.mLineRender.SetPosition(currentStep, cp);
        }
    }

    void checkAttack(Character cha)
    {
        Collider2D[] cs = Physics2D.OverlapCircleAll(CHelp.v3tov2(cha.transform.position), cha.mPatrolDis);
        foreach(var v in cs)
        {
            Character findCha = v.GetComponent<Character>();
            if (!findCha)
                continue;

            if (cha == findCha || cha.mChaType == findCha.mChaType)
                continue;

            if((cha.mChaType == CHATYPE.ENEMY && findCha.mChaType == CHATYPE.FRIEND) ||
                (cha.mChaType == CHATYPE.FRIEND && findCha.mChaType == CHATYPE.ENEMY))
            {

            }
        }
    }
    public override void update(params object[] ps)
    {
        Character cha = ps[0] as Character;

        drawCircle(cha);

        if (cha.mControlRole && CHelp.hasHit())
        {
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            gotoTarPos(cha.transform.position, new Vector2(touchPos.x, touchPos.y));
        }

        if (mTarPos.Count <= 0)
        {
            return;
        }

        CWalkDir wd = mTarPos.Peek();

        if (cha.mDir != wd.dir)
        {
            cha.mDir = wd.dir;
            mCurSpriteIndex = 0;
        }

        Vector2 tarDir = new Vector2(wd.tarPos.x - cha.transform.position.x, wd.tarPos.y - cha.transform.position.y);
        tarDir.Normalize();

        Vector2 dp = tarDir * cha.mSpeed * Time.deltaTime;
        cha.transform.position += new Vector3(dp.x, dp.y, 0);

        if (Mathf.Abs(cha.transform.position.x - wd.tarPos.x) <= 0.01 && Mathf.Abs(cha.transform.position.y - wd.tarPos.y) < 0.01)
        {
            cha.transform.position = new Vector3(wd.tarPos.x, wd.tarPos.y, cha.transform.position.z);
            mTarPos.Dequeue();
        }

    }
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
        startRoutine(cha);
    }
    public override void startRoutine(Character cha)
    {
        cha.StartCoroutine(updateAni(cha));
    }
    public override void unInit(params object[] ps)
    {
        base.unInit();
    }

    public IEnumerator updateAni(Character cha)
    {
        SpriteRenderer sr = cha.GetComponent<SpriteRenderer>();
        while (!mCancel)
        {
            mCurSpriteIndex = (mCurSpriteIndex + 1) % mSpriteIndex.Count;
            sr.sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];

            yield return new WaitForSeconds(0.5f);
        }
    }

    public override void update(params object[] ps)
    {
        Character cha = ps[0] as Character;

        if (cha.mControlRole && CHelp.hasHit())
        {
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            cha.mMS.gotoState(STATE.WALK_STATE, false, new CWalkParam(cha, new Vector2(touchPos.x, touchPos.y)));
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
public class CWalkState : CState
{
    public List<int> mSpriteIndex = new List<int>() { 0, 1, 2, 3 };
    public int mCurSpriteIndex = 0;
    public Queue<Queue<CWalkDir>> mTarPos = new Queue<Queue<CWalkDir>>();
    public CWalkState() : base(STATE.WALK_STATE) { }
    public override void init(params object[] ps)
    {
        CWalkParam wp = ps[0] as CWalkParam;
        gotoTarPos(wp.cha.transform.position, wp.tarPos);
        startRoutine(wp.cha);
    }
    public override void startRoutine(Character cha)
    {
        cha.StartCoroutine(updateAni(cha));
    }
    public override void unInit(params object[] ps)
    {
        base.unInit();
    }

    public IEnumerator updateAni(Character cha)
    {
        SpriteRenderer sr = cha.GetComponent<SpriteRenderer>();
        while (!mCancel)
        {
            mCurSpriteIndex = (mCurSpriteIndex + 1) % mSpriteIndex.Count;
            sr.sprite = cha.mSprites[(int)cha.mDir * 4 + mSpriteIndex[mCurSpriteIndex]];

            yield return new WaitForSeconds(0.25f);
        }
    }

    void gotoTarPos(Vector3 pos, Vector2 tar, bool append = false)
    {
        if (!append)
        {
            if (mTarPos.Count > 0)
                mTarPos.Dequeue();
        }

        float dif_x = Mathf.Abs(pos.x - tar.x);
        float dif_y = Mathf.Abs(pos.y - tar.y);

        Queue<CWalkDir> wd = new Queue<CWalkDir>();
        if (dif_x < dif_y)
        {
            wd.Enqueue(new CWalkDir(new Vector2(tar.x, pos.y), tar.x > pos.x ? DIR.RIGHT : DIR.LEFT));
            wd.Enqueue(new CWalkDir(new Vector2(tar.x, tar.y), tar.y > pos.y ? DIR.TOP : DIR.DOWN));
        }
        else
        {
            wd.Enqueue(new CWalkDir(new Vector2(pos.x, tar.y), tar.y > pos.y ? DIR.TOP : DIR.DOWN));
            wd.Enqueue(new CWalkDir(new Vector2(tar.x, tar.y), tar.x > pos.x ? DIR.RIGHT : DIR.LEFT));
        }
        mTarPos.Enqueue(wd);
    }
    public override void update(params object[] ps)
    {
        Character cha = ps[0] as Character;

        if (cha.mControlRole && CHelp.hasHit())
        {
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            gotoTarPos(cha.transform.position, new Vector2(touchPos.x, touchPos.y));
        }

        if (mTarPos.Count <= 0)
        {
            cha.mMS.gotoState(STATE.IDLE_STATE, false, cha);
            return;
        }

        Queue<CWalkDir> wds = mTarPos.Peek();
        CWalkDir wd = wds.Peek();

        if (cha.mDir != wd.dir)
        {
            cha.mDir = wd.dir;
            mCurSpriteIndex = 0;
        }

        Vector2 tarDir = new Vector2(wd.tarPos.x - cha.transform.position.x, wd.tarPos.y - cha.transform.position.y);
        tarDir.Normalize();

        Vector2 dp = tarDir * cha.mSpeed * Time.deltaTime;
        cha.transform.position += new Vector3(dp.x, dp.y, 0);

        if (Mathf.Abs(cha.transform.position.x - wd.tarPos.x) <= 0.01 && Mathf.Abs(cha.transform.position.y - wd.tarPos.y) < 0.01)
        {
            cha.transform.position = new Vector3(wd.tarPos.x, wd.tarPos.y, cha.transform.position.z);
            wds.Dequeue();
            if (wds.Count <= 0)
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
    public Stack<CState> mStates = new Stack<CState>();

    public void gotoState(STATE newState, bool savePreState = false, params Object[] ps)
    {
        if (mStates.Count > 0)
            mStates.Peek().unInit(ps);

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
        if (s == STATE.PATROL_STATE)
        {
            return new CPatrolState();
        }

        return null;
    }

    public void update(params Object[] ps)
    {
        if (mStates.Count > 0)
            mStates.Peek().update(ps);
    }
}