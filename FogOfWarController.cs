using UnityEngine;

public class FogOfWarController : MonoBehaviour
{
    public Material fogMaterial; // assign your FogOfWarRaymarch material here

    public Transform player;     // player transform (world position used)
    public float visionRadius = 5f;

    [Header("Occlusion texture world bounds")]
    public Vector2 worldMin = new Vector2(0, 0);
    public Vector2 worldMax = new Vector2(10, 10);

    void Update()
    {
        // FragmentShader();
        ComputeShader();
    }

    private void FragmentShader()
    {
        if (player is null || fogMaterial is null) return;

        Vector3 pos = player.position;

        // Pass player pos.x, pos.z + radius to shader (_PlayerPosRadius)
        fogMaterial.SetVector("_PlayerPosRadius", new Vector4(pos.x, pos.z, visionRadius, 0));

        // Pass world bounds used to map world space to UV coords in occlusion texture
        fogMaterial.SetVector("_WorldMin", new Vector4(worldMin.x, worldMin.y, 0, 0));
        fogMaterial.SetVector("_WorldMax", new Vector4(worldMax.x, worldMax.y, 0, 0));
    }
    
    [Header("Shader & Texture Settings")]
    public ComputeShader fogComputeShader;
    public Texture2D occlusionTexture;
    public int textureResolution = 256;

    private RenderTexture fogTexture;
    private ComputeBuffer observerBuffer;
    private int kernelID;

    private FogObserverData[] observers;

    public RenderTexture FogTexture => fogTexture;
    
    void Start()
    {
        // Setup output texture
        fogTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RFloat);
        fogTexture.enableRandomWrite = true;
        fogTexture.filterMode = FilterMode.Point;
        fogTexture.Create();

        kernelID = fogComputeShader.FindKernel("CSMain");
    }
    
    public struct FogObserverData
    {
        public Vector2 position;
        public float radius;

        public FogObserverData(Vector2 pos, float r)
        {
            position = pos;
            radius = r;
        }

        public Vector4 ToVector4() => new Vector4(position.x, position.y, radius, 0f);
    }
    
    public void UpdateObservers(FogObserverData[] newObservers)
    {
        observers = newObservers;
        if (observerBuffer != null)
            observerBuffer.Release();

        observerBuffer = new ComputeBuffer(observers.Length, sizeof(float) * 4);
        Vector4[] data = new Vector4[observers.Length];
        for (int i = 0; i < observers.Length; i++)
            data[i] = observers[i].ToVector4();

        observerBuffer.SetData(data);
    }

    void ComputeShader()
    {
        if (fogComputeShader == null || observers == null || observers.Length == 0)
            return;

        // Set compute shader parameters
        fogComputeShader.SetTexture(kernelID, "Result", fogTexture);
        fogComputeShader.SetTexture(kernelID, "OcclusionTex", occlusionTexture);
        fogComputeShader.SetBuffer(kernelID, "Observers", observerBuffer);
        fogComputeShader.SetInt("ObserverCount", observers.Length);
        fogComputeShader.SetVector("WorldMin", worldMin);
        fogComputeShader.SetVector("WorldMax", worldMax);
        fogComputeShader.SetInts("TextureSize", textureResolution, textureResolution);

        int threadGroupsX = Mathf.CeilToInt(textureResolution / 8f);
        int threadGroupsY = Mathf.CeilToInt(textureResolution / 8f);

        fogComputeShader.Dispatch(kernelID, threadGroupsX, threadGroupsY, 1);
    }
    
    void OnDestroy()
    {
        if (observerBuffer != null)
            observerBuffer.Release();
        if (fogTexture != null)
            fogTexture.Release();
    }
    
    void LateUpdate()
    {
        var players = FindObjectsOfType<Unit>(); // or tag/unit list
        var obs = new FogObserverData[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            Vector3 pos = players[i].transform.position;
            float radius = players[i].viewRange;
            obs[i] = new FogObserverData(new Vector2(pos.x, pos.z), radius); // assuming XZ
        }

        UpdateObservers(obs);
    }

}