public class Master_Miscellaneous : MonoBehaviour
{

    [SerializeField]
    Material mat_voidBlockObject_bottom;

    public static readonly int baseMapID = Shader.PropertyToID("_BaseMap");

    public static Vector2 textureOffset_voidBlockObject_bottom = new Vector2(0f, 0f);
    Vector2 driftSpeed_voidBlockObject_bottom = new Vector2(0f, 0f);
    Vector2 driftForce_voidBlockObject_bottom = new Vector2(0f, 0f);
    Vector2 driftNoise_voidBlockObject_bottom = new Vector2(0f, 0f);
    float driftForceHeroFactor_voidBlockObject_bottom = 0.1f;
    float driftForceNoiseFactor_voidBlockObject_bottom = 0.05f;

    void Start()
    {
        InvokeRepeating(nameof(GenerateDriftNoiseForceBottom), 0f, 4f);
    }

    void Update()
    {
        ConfigureVoidBlockObjectTextures();
    }

    void ConfigureVoidBlockObjectTextures()
    {
        driftForce_voidBlockObject_bottom = driftForceHeroFactor_voidBlockObject_bottom * Master_Core.heroMoveSpeed * new Vector2(Master_Core.heroMoveDirection.x, Master_Core.heroMoveDirection.z)
            + driftForceNoiseFactor_voidBlockObject_bottom * driftNoise_voidBlockObject_bottom;

        driftSpeed_voidBlockObject_bottom += driftForce_voidBlockObject_bottom * Time.deltaTime;

        if (driftSpeed_voidBlockObject_bottom.sqrMagnitude > 0.09f)//限速每秒0.3个循环
            driftSpeed_voidBlockObject_bottom = driftSpeed_voidBlockObject_bottom.normalized * 0.3f;

        textureOffset_voidBlockObject_bottom += driftSpeed_voidBlockObject_bottom * Time.deltaTime;
        
        mat_voidBlockObject_bottom.SetTextureOffset(baseMapID, textureOffset_voidBlockObject_bottom);
    }
    void GenerateDriftNoiseForceBottom()
    {
        driftNoise_voidBlockObject_bottom = new Vector2(Random.Range(-9, 10), Random.Range(-9, 10)).normalized;
        Invoke(nameof(CancelDriftNoiseForceBottom), 0.2f);
    }
    void CancelDriftNoiseForceBottom()
    {
        driftNoise_voidBlockObject_bottom = Vector2.zero;
    }
}
