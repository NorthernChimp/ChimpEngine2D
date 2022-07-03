using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScript : MonoBehaviour
{

    public GameObject brickPrefab;
    public GameObject wallPrefab;
    public GameObject particlePrefab;
    Vector3 normalizedScale = new Vector3(1f, 1f, 1f);
    public static float brickHeight = 0f;
    public static List<ChimpEngine2D> engines;
    public static List<BoxCollider2D> walls;
    public static List<Counter> counters;

    public static float minDistFromCenterHorizontal;
    public static float minDistFromCenterVertical;

    public List<Color> particleColors;

    // Start is called before the first frame update
    void Start()
    {
        SetupGame();
        Vector3 origin = Vector3.zero + (Vector3.up * Screen.height * 0.005f) + (Vector3.left * 24.5f * brickHeight);
        Camera.main.orthographicSize = Screen.height * 0.005f;
        for (int i = 0; i < 30; i++)
        {
            for(int x = 0; x < 50; x++)
            {
                bool isEdge = (x == 0 || x == 49 || i == 0 || i == 29);
                if (isEdge)
                {
                    Vector3 currentPos = origin + (Vector3.down * i * brickHeight) + (Vector3.right * x * brickHeight) + (Vector3.down * 0.5f * brickHeight);
                    CreateBrick(currentPos);
                }
            }
        }
        for(int i = 0; i < 1; i++)
        {
            Vector3 randomPoint = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));
            CreateParticle(randomPoint);
        }
        CreateWall(Vector3.right * 3.5f, 5f, 1.5f);
        CreateWall(Vector3.down * 2.5f, 5f, 1f);
        CreateWall(Vector3.up * 2.5f, 5f, 1f);
        CreateWall(Vector3.left * 3.5f, 5f, 1.5f);
    }
    void SetupGame()
    {
        counters = new List<Counter>();
        walls = new List<BoxCollider2D>();
        engines = new List<ChimpEngine2D>();
        float height = Screen.height / 30f;
        height *= 0.01f;
        brickHeight = height;
        float scale = height / 0.32f;
        normalizedScale = new Vector3(scale, scale, scale);
    }
    void CreateParticle(Vector3 pos)
    {
        int randomInt = (int)Random.Range(0f, particleColors.Count);
        Color c = particleColors[randomInt];
        pos += Vector3.right * Random.value * 0.01f;//this is just so they dont spawn exactly on top of each other which will stack them weirdly in a way that would almost never happen without mathematcial precision
        Transform t = Instantiate(particlePrefab, pos, Quaternion.identity).transform;
        t.GetComponent<SpriteRenderer>().material.color = new Color(c.r, c.g, c.b); ;
        t.localScale = normalizedScale;
        ChimpEngine2D e = t.GetComponent<ChimpEngine2D>();
        e.SetupEngine();
        e.defaultSettings.isAffectedByGravity = true;
    }
    void CreateWall(Vector3 pos, float brckWidth, float brckHeight)
    {
        Transform t = Instantiate(wallPrefab, pos, Quaternion.identity).transform;
        t.localScale = new Vector3(brckWidth * normalizedScale.x,brckHeight * normalizedScale.y,normalizedScale.z);
        walls.Add(t.GetComponent<BoxCollider2D>());
    }
    void CreateBrick(Vector3 pos)
    {
        Transform t = Instantiate(brickPrefab, pos, Quaternion.identity).transform;
        t.localScale = normalizedScale;
    }
    void CreateExplosion(Vector3 pos)
    {
        Vector3 explosionPoint = ChimpEngine2D.RemoveZ(pos);
        float minDistance = brickHeight * 4.20f;
        foreach (ChimpEngine2D e in engines)
        {
            Vector3 enginePoint = ChimpEngine2D.RemoveZ(e.transform.position);
            Vector3 directToEngine = enginePoint - explosionPoint;
            if(directToEngine.magnitude <= minDistance)
            {
                MovementAffector a = new MovementAffector();
                a.type = MovementAffectorType.momentumBased;
                a.directToMove = new Vector2(directToEngine.x, directToEngine.y).normalized;
                a.baseSpeed = 3f;
                a.endCounter = new Counter(10f);
                counters.Add(a.endCounter);
                //e.AddMovementAffector(a);
                e.velocity = Vector3.Reflect(e.velocity, Vector3.up);
                e.velocity += Vector3.up * 0.420f * brickHeight ;
                e.velocity += directToEngine.normalized * 0.420f * brickHeight ;
                //e.velocity += directToEngine.normalized * 24.20f * brickHeight * (Time.fixedDeltaTime/ 1f);
                e.SetJumpCounter(-3f);
            }
        }
    }
    void UpdateGame(float timePassed)
    {
        foreach(Counter c in counters) { if (!c.hasfinished) { c.UpdateCounter(timePassed); } }
        foreach(ChimpEngine2D e in engines) { e.UpdateEngine(timePassed); }
        foreach(BoxCollider2D b in walls) { b.transform.Rotate(new Vector3(0f, 0f, Time.deltaTime * 90f)); }
    }
    private void FixedUpdate()
    {
        UpdateGame(Time.fixedDeltaTime);
    }
    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 p = Input.mousePosition;
            p.z = 20;
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            //testGameObject.transform.position = pos;
            Vector3 v = Camera.main.ViewportToWorldPoint(Input.mousePosition);
            CreateParticle(pos);
            //CreateParticle(new Vector3(v.x,v.y,0f));
        }
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 p = Input.mousePosition;
            p.z = 20;
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            //testGameObject.transform.position = pos;
            Vector3 v = Camera.main.ViewportToWorldPoint(Input.mousePosition);
            CreateExplosion(pos);
        }
    }
}
