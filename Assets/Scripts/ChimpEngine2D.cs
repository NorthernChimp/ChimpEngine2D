using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChimpEngine2D : MonoBehaviour
{
    List<BoxCollider2D> wallsInContact;
    List<ChimpEngine2D> enginesInContact;
    List<MovementAffector> affectors;
    List<Vector3> normalsThisFrame;
    List<EngineMod> mods;
    Vector4 normals;// a vector of floats for each normal x and y are right and up facing normals, z and w are left and down respectively
    public CircleCollider2D coll;
    public EngineSettings defaultSettings;
    public EngineSettings currentSettings;
    bool grounded = false;
    public Vector3 movementVelocity = Vector3.zero;

    public Vector3 velocity = Vector3.zero;

    float jumpCounterTime = 0f;//time into the jump. -3 is about to jump 0 is peak, 3 is falling fast
    float jumpSpeed = 6f;//the speed at which jump time passes. lower is floaty and slow, higher is faster. this is how fast the jump happens, not how far you jump
    float jumpHeight = 1f; //the modifier that the normalized jump value is multiplied by. it is the pinnacle height of the jump from where you jumped (with no other factors affecting)
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void SetupEngine()
    {
        normalsThisFrame = new List<Vector3>();
        coll = GetComponent<CircleCollider2D>();
        affectors = new List<MovementAffector>() { };
        mods = new List<EngineMod>();
        wallsInContact = new List<BoxCollider2D>();
        enginesInContact = new List<ChimpEngine2D>();
        defaultSettings = new EngineSettings();
        currentSettings = EngineSettings.CopySettings(defaultSettings);
        MainScript.engines.Add(this);
    }
    void UpdateCurrentSettings(float timePassed)
    {
        currentSettings = EngineSettings.CopySettings(defaultSettings);
        for(int i = 0; i < mods.Count; i++)
        {
            EngineMod m = mods[i];
        }
    }
    public void AddMovementAffector(MovementAffector af)
    {
        affectors.Add(af);
    }
    Vector2 AddUpMovementAffectors(float timePassed)
    {
        Vector2 total = Vector2.zero;
        for(int i = 0; i < affectors.Count;i++)
        {
            MovementAffector a = affectors[i];
            float currentSpeed = a.baseSpeed;
            if (!a.endCounter.hasfinished)
            {
                switch (a.type)
                {
                    case MovementAffectorType.arbitrary:
                        
                        if (a.accelerates) { currentSpeed += (a.endCounter.currentTime * a.accelRate); }
                        total += a.directToMove * timePassed * currentSpeed;
                        break;
                    case MovementAffectorType.momentumBased:
                        if (a.accelerates) { currentSpeed += (a.endCounter.currentTime * a.accelRate); }
                        total += a.directToMove * timePassed * currentSpeed;
                        break;
                }
            }
            else
            {
                affectors.RemoveAt(i);
                i--;
            }
            
        }
        return total + (Vector2.down * timePassed * MainScript.brickHeight * 4.20f); ;  
    }
    float MarioFunction(float timeSinceJump)
    {
        return -(timeSinceJump * timeSinceJump) + 9f;
    }
    Vector2 GetLocalControls()
    {
        Vector2 moveDir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        return moveDir;
    }
    public void SetJumpCounter(float newJumpCounter) { jumpCounterTime = newJumpCounter; }
    public void UpdateEngine(float timePassed)
    {
        Vector2 movementFinal = Vector2.zero; //the movement the engine calculates
        Vector2 addedMovement = Vector2.zero;//movement that is added on top of that in the end
        UpdateCurrentSettings(timePassed);
        movementFinal += AddUpMovementAffectors(timePassed);
        if (currentSettings.isAffectedByGravity )
        {
            if(velocity.y <= 0.01f)
            {
                float minDist = GetMinDistFromCenter() * 1.15f;
                RaycastHit2D h = Physics2D.Raycast(transform.position, Vector2.down, minDist, LayerMask.GetMask("Wall"));
                Debug.DrawRay(transform.position, Vector3.down * minDist, Color.green);
                if (h.collider != null)
                {
                    SetJumpCounter(0f);
                    //transform.position = h.point + (Vector2.up * minDist * 1.05f);
                    grounded = true;
                }
                else
                {
                    if (Mathf.Sign(transform.position.y) == -1f)//if we are on the bottom half of the screens
                    {
                        //Debug.Log(Mathf.Abs(transform.position.y) + " is y pos " + GetHighestYValue() + " is the highesty value");
                        float diff = Mathf.Abs(transform.position.y) - GetHighestYValue();//how far are we from teh bottom most area possible
                        
                        diff = Mathf.Abs(diff);                                                            //Debug.Log(diff + " and pos y " + transform.position.y);
                        if (velocity.y <= 0.01f && Mathf.Abs(diff) < 0.001f ) { grounded = true; SetJumpCounter(0f); } else { grounded = false; }

                    }
                    else { grounded = false; }

                }
            }
            else { grounded = false; }
            //Debug.Log(grounded);
            if (!grounded)
            {
                float prevMario = MarioFunction(jumpCounterTime);
                jumpCounterTime += timePassed * jumpSpeed;
                float currentMario = MarioFunction(jumpCounterTime);
                float diff = currentMario - prevMario;
                jumpCounterTime = Mathf.Clamp(jumpCounterTime, -3f, 3f);
                movementFinal += Vector2.up * diff * jumpHeight * MainScript.brickHeight;
            }
            
            //Debug.Log(jumpCounterTime + " is the jump counter time and " + movementFinal + " is move final");
        }

        if (currentSettings.isControlled)
        {
            Vector2 moveDir = GetLocalControls();
            float accelRate = 120.5f;
            float decelRate = 120.5f;
            if (moveDir != Vector2.zero)
            {
                movementVelocity += (Vector3)moveDir.normalized * timePassed * accelRate;
                float maxSpeed = MainScript.brickHeight * 0.15f;
                float dot = Vector2.Dot(movementVelocity, moveDir);
                if(dot < 0f)
                {
                    DecelerateMovementVelocity(decelRate,timePassed);
                }
                if (movementVelocity.magnitude > maxSpeed) { movementVelocity = movementVelocity.normalized * maxSpeed; }
            }
            else { DecelerateMovementVelocity(decelRate, timePassed); }
            //Debug.Log("movement velocity is " + movementVelocity + " and movementfinal is " + movementFinal);
        }
        //Debug.Log("movement final is " + movementFinal + " before checkmovementagainst normals");
        foreach(ChimpEngine2D e in enginesInContact){Vector3 directFromEngine = (e.transform.position - transform.position).normalized;movementFinal += (Vector2)directFromEngine * -0.05f ;}
        movementFinal += (Vector2)movementVelocity;
        movementFinal = CheckMovementAgainstNormals(movementFinal);
        //Debug.Log("movement final is " + movementFinal + " after checkmovementagainst normals");
        if (movementFinal != Vector2.zero) { velocity += (Vector3)movementFinal * timePassed; }
        Vector3 movementVelocityFinal = movementVelocity ;
        if (velocity != Vector3.zero) { transform.Translate(velocity + movementVelocityFinal, Space.World); }//if (velocity.y > 0.001f && grounded) { grounded = false; } }
        CheckForCollisions(timePassed);
        velocity *= 0.985f;
    }
    void DecelerateMovementVelocity(float decelRate,float timePassed)
    {
        float amtToDecrease = timePassed * decelRate; if (movementVelocity.magnitude <= amtToDecrease) { movementVelocity = Vector3.zero; } else { movementVelocity -= movementVelocity.normalized * amtToDecrease; }
    }
    Vector3 CheckMovementAgainstNormals(Vector3 movementFin)
    {
        Vector3 movementFinal = movementFin;
        foreach (Vector3 wallNormal in normalsThisFrame)
        {
            float dot = Vector3.Dot(wallNormal, movementFinal);
            if(dot < 0f) 
            {
                movementFinal = Vector3.Reflect(movementFinal, wallNormal);
            }
        }
        return movementFinal;
    }
    float GetMinDistFromCenterScreen() { return MainScript.brickHeight * currentSettings.size * 1f; }
    float GetMinDistFromTop() { return GetMinDistFromCenterScreen() + MainScript.brickHeight; }
    float GetHighestYValue() { return (Screen.height * 0.005f) - GetMinDistFromTop(); }
    void CheckWeAreWithinTheLevelBounds()
    {
        float minDistFromCenter = GetMinDistFromCenterScreen();//engines like the one testing for collisions here have a size variable and their local scale is based on that
        float highestYValue = GetHighestYValue();
        //highestYValue *= 0.99f;
        float clampedY = Mathf.Clamp(transform.position.y, -highestYValue, highestYValue);
        if(clampedY != transform.position.y)
        {
            Vector3 normal = new Vector3(0f, Mathf.Sign(transform.position.y) * -1f, 0f);
            normalsThisFrame.Add(normal);
            BounceOff(normal);
            //if (!grounded) {  }
            transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
        }
        float minDistFromCenterHorizontal = MainScript.brickHeight * 24f;
        float highestXValue = minDistFromCenterHorizontal - minDistFromCenter;
        //highestXValue *= 0.99f;
        float clampedX = Mathf.Clamp(transform.position.x, -highestXValue, highestXValue);
        if(clampedX != transform.position.x)
        {
            Vector3 normal = new Vector3(Mathf.Sign(transform.position.x) * -1f, 0f, 0f);
            normalsThisFrame.Add(normal);
            BounceOff(normal);
            transform.position = new Vector3(Mathf.Sign(transform.position.x) * (highestXValue), transform.position.y, transform.position.z);
        }
    }
    public float GetMinDistFromCenter() { return MainScript.brickHeight * currentSettings.size * 1f; }
    void CheckForCollisions(float timePassed)
    {
        Vector3 pushOut = Vector3.zero;
        normals = Vector4.zero;
        normalsThisFrame = new List<Vector3>();
        float minDistanceRecentlyImpactedRatio = 1.05f;//the ratio by which the min distance can be multiplied if they were in contact previously
        foreach(BoxCollider2D b in MainScript.walls)
        {
            Vector3 directFromWall = RemoveZ(transform.position - b.transform.position);
            Quaternion rotateToWallLocal = Quaternion.Euler(0f, 0f, b.transform.eulerAngles.z * -1);
            Vector3 directFromWallRotated = rotateToWallLocal * directFromWall;
            float minDistWidth = b.transform.localScale.x * 0.5f * b.size.x;
            float minDistHeight = b.transform.localScale.y * 0.5f * b.size.y;
            float clampedX = Mathf.Clamp(directFromWallRotated.x, -minDistWidth, minDistWidth);
            float clampedY = Mathf.Clamp(directFromWallRotated.y, -minDistHeight, minDistHeight);
            float minDistFromCenter = GetMinDistFromCenter();//engines like the one testing for collisions here have a size variable and their local scale is based on that
            Vector3 clampedPosNew = b.transform.position + (b.transform.right * clampedX ) + (b.transform.up * clampedY );
            Vector3 directFromClampedPos = RemoveZ(transform.position - clampedPosNew);
            float distanceToClamped = directFromClampedPos.magnitude;
            if(distanceToClamped < minDistFromCenter)//we impact
            {
                transform.position = clampedPosNew + (directFromClampedPos.normalized * minDistFromCenter);
                normalsThisFrame.Add(directFromClampedPos.normalized);
                BounceOff(directFromClampedPos.normalized * 1f); 
                if (Mathf.Sign(directFromClampedPos.x) == 1f) { }
                if (!wallsInContact.Contains(b)) { wallsInContact.Add(b); }
            }else if (wallsInContact.Contains(b))
            {
                if (distanceToClamped < minDistFromCenter * minDistanceRecentlyImpactedRatio)
                {
                    normalsThisFrame.Add(directFromClampedPos.normalized);
                }
                else { wallsInContact.Remove(b); }
            }
        }
        foreach(ChimpEngine2D e in MainScript.engines)
        {
            if(e != this)
            {
                Vector3 directToEngine = RemoveZ(e.transform.position - transform.position);
                float distance = directToEngine.magnitude;
                float minDistance = (e.currentSettings.size + currentSettings.size) * MainScript.brickHeight;
                if (distance < minDistance)
                {
                    float remainingDistance = minDistance - distance;
                    remainingDistance *= 0.5f;
                    float timeTotal = timePassed + (remainingDistance / 0.420f);
                    Vector3 moveOutTotal = directToEngine.normalized * remainingDistance * timeTotal * -1f;
                    if (!this.enginesInContact.Contains(e)) { enginesInContact.Add(e); BounceOff(directToEngine.normalized * -1f); }
                    if (!e.enginesInContact.Contains(this)) { enginesInContact.Add(this); e.BounceOff(directToEngine.normalized); }
                    Vector3 normal = directToEngine.normalized * -1f;
                    pushOut += normal;
                    Vector3 moveOut = directToEngine.normalized * remainingDistance * -1f;
                    normalsThisFrame.Add(directToEngine.normalized * -1f);
                    transform.Translate(moveOut * 1f, Space.World);
                    float absorbsAmt = 0.2f;
                    e.velocity += velocity * (Time.fixedDeltaTime/1f) * absorbsAmt;
                    velocity += e.velocity * (Time.fixedDeltaTime/1f) * absorbsAmt;
                }
                else 
                { 
                    if (enginesInContact.Contains(e)) 
                    { 
                        enginesInContact.Remove(e); 
                    } 
                    if (e.enginesInContact.Contains(this)) 
                    { 
                        e.enginesInContact.Remove(this); 
                    }
                    
                }
            }
        }
        CheckWeAreWithinTheLevelBounds();
    }
    public void BounceOff(Vector3 normal)
    {
        float bounceAmt = currentSettings.bounceAmt;
        if (currentSettings.isAffectedByGravity)
        {
            if(Mathf.Sign(velocity.y) < 0f && normal.y > 0.5f && Mathf.Abs(velocity.y) < MainScript.brickHeight * 0.01f) { SetJumpCounter(0f); }
            if(Mathf.Sign(velocity.y) > 0f && normal.y < -0.5f && Mathf.Abs(velocity.y) < MainScript.brickHeight * 0.01f) { SetJumpCounter(0f); }
            //if(normal.y > 0.5f && jumpCounterTime >= 0f ) { SetJumpCounter(-2f * normal.y * bounceAmt); }
        }
        //float bounceAmt = 0f;
        
        if(normal.y > 0f)
        {
            //if (grounded) { normal = new Vector3(normal.x, 0f, 0f).normalized; }
        }
        //Debug.Log("bouncing off " + normal);
        if (Vector3.Dot(velocity,normal) < -0.1f) 
        { 
            velocity = Vector3.Reflect(velocity, normal) * bounceAmt; 
            if(normal.y > 0.5f)
            {
                if(velocity.y < -0.01f) { grounded = true; }
                //if(velocity.magnitude <= MainScript.brickHeight/Time.fixedDeltaTime) { grounded = true; velocity = new Vector3(velocity.x, 0f); }
            }
        }
        foreach(MovementAffector a in affectors)
        {
            if(a.type == MovementAffectorType.momentumBased)
            {
                float dotProd = Vector2.Dot(a.directToMove, (Vector2)normal);
                if(dotProd < 0f)
                {
                    //Debug.Log("changing direct to move previous: " + a.directToMove + " the normal was " + normal);
                    a.directToMove = (Vector2)Vector2.Reflect(a.directToMove, (Vector2)normal);
                    //Debug.Log("changing direct to move afterward: " + a.directToMove);
                }
                
            }
        }
    }
    public static Vector3 RemoveZ(Vector3 v) { return new Vector3(v.x, v.y, 0f); }
    // Update is called once per frame
    void Update()
    {
        
    }
}
public class EngineSettings
{
    public bool isAffectedByGravity = false;
    public bool isControlled = true;
    public bool bounces = false;
    public float bounceAmt = 0.7f;//how much velocity is maintained when bouncing. zero means none 1 means all, more than 1 scales 2 is double what you impacted with.
    public float size = 1f;
    public float mass = 1f;
    public static EngineSettings CopySettings(EngineSettings set)
    {
        EngineSettings temp = new EngineSettings();
        temp.isAffectedByGravity = set.isAffectedByGravity;
        temp.isControlled = set.isControlled;
        temp.bounces = set.bounces;
        temp.size = set.size;
        temp.mass = set.mass;
        return temp;
    }

}
public class EngineMod
{

}
public class MovementAffector
{
    public float baseSpeed = 0f;
    public float accelRate = 0f;
    public bool accelerates = false;
    public Counter endCounter;
    public Vector2 directToMove = Vector2.zero;
    public MovementAffectorType type = MovementAffectorType.none;
    public static MovementAffector GetBasicAffector(float speed,Vector2 dir,float endTime)
    {
        MovementAffector a = new MovementAffector();
        a.baseSpeed = speed;
        a.directToMove = dir;
        a.endCounter = new Counter(endTime);
        MainScript.counters.Add(a.endCounter);
        return a;
    }
}
public enum MovementAffectorType { momentumBased, arbitrary,none}