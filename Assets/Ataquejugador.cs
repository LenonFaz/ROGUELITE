using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class AtaqueJugador : MonoBehaviour
{
    [Header("Ataque")]
    public float rango = 1.2f;
    public Vector2 tamHitbox = new Vector2(1.2f, 1f);
    public float cooldown = 0.3f;
    public LayerMask capaEnemigos;
    public int danio = 1;

    [Header("Empuje")]
    [Range(0f, 30f)] public float fuerzaEmpuje = 8f;

    [Header("Debug - Scene view")]
    public bool mostrarGizmo = true;

    [Header("Debug - Game view")]
    public bool mostrarHitbox = true;
    public float tiempoVisible = 0.15f;
    public Color colorHitbox = Color.red;
    public float grosorLinea = 0.05f;

    private float timerCooldown = 0f;
    private Vector2 ultimaDireccion = Vector2.down;
    private Vector2 ultimoCentro;

    private LineRenderer lr;
    private Coroutine rutinaOcultar;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        ConfigurarLinea();
    }

    void ConfigurarLinea()
    {
        lr.positionCount = 5;
        lr.loop = false;
        lr.useWorldSpace = true;
        lr.widthMultiplier = grosorLinea;
        lr.startColor = colorHitbox;
        lr.endColor = colorHitbox;
        if (lr.material == null)
            lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.enabled = false;
    }

    void Update()
    {
        timerCooldown -= Time.deltaTime;

        Vector2 dir = LeerDireccion();
        if (dir.sqrMagnitude > 0.01f && timerCooldown <= 0f)
        {
            Atacar(dir.normalized);
            timerCooldown = cooldown;
        }
    }

    Vector2 LeerDireccion()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        if (kb.upArrowKey.wasPressedThisFrame) return Vector2.up;
        if (kb.downArrowKey.wasPressedThisFrame) return Vector2.down;
        if (kb.leftArrowKey.wasPressedThisFrame) return Vector2.left;
        if (kb.rightArrowKey.wasPressedThisFrame) return Vector2.right;
        return Vector2.zero;
    }

    void Atacar(Vector2 dir)
    {
        ultimaDireccion = dir;
        Vector2 centro = (Vector2)transform.position + dir * rango;
        ultimoCentro = centro;

        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centro, tamHitbox, angulo, capaEnemigos);

        foreach (var c in golpeados)
        {
            var vida = c.GetComponent<Vida>();
            if (vida != null) vida.RecibirDanio(danio);

            var empuje = c.GetComponent<Empuje>();
            if (empuje != null) empuje.AplicarEmpuje(dir, fuerzaEmpuje);

            var aturdido = c.GetComponent<Aturdido>();
            if (aturdido != null) aturdido.Aturdir();
        }

        if (mostrarHitbox) MostrarHitbox(centro, angulo);
    }

    void MostrarHitbox(Vector2 centro, float angulo)
    {
        Vector3[] esquinas = ObtenerEsquinas(centro, tamHitbox, angulo);
        lr.SetPosition(0, esquinas[0]);
        lr.SetPosition(1, esquinas[1]);
        lr.SetPosition(2, esquinas[2]);
        lr.SetPosition(3, esquinas[3]);
        lr.SetPosition(4, esquinas[0]);
        lr.startColor = colorHitbox;
        lr.endColor = colorHitbox;
        lr.widthMultiplier = grosorLinea;
        lr.enabled = true;

        if (rutinaOcultar != null) StopCoroutine(rutinaOcultar);
        rutinaOcultar = StartCoroutine(OcultarLuego(tiempoVisible));
    }

    IEnumerator OcultarLuego(float t)
    {
        yield return new WaitForSeconds(t);
        lr.enabled = false;
    }

    Vector3[] ObtenerEsquinas(Vector2 centro, Vector2 tam, float angulo)
    {
        Vector2 mitad = tam * 0.5f;
        Vector2[] local = {
            new Vector2(-mitad.x, -mitad.y),
            new Vector2(mitad.x, -mitad.y),
            new Vector2(mitad.x, mitad.y),
            new Vector2(-mitad.x, mitad.y),
        };

        Quaternion rot = Quaternion.Euler(0f, 0f, angulo);
        Vector3[] mundo = new Vector3[4];
        for (int i = 0; i < 4; i++)
            mundo[i] = new Vector3(centro.x, centro.y, 0f) + rot * new Vector3(local[i].x, local[i].y, 0f);
        return mundo;
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmo) return;
        Gizmos.color = colorHitbox;
        Vector2 centro = Application.isPlaying ? ultimoCentro : (Vector2)transform.position + ultimaDireccion * rango;
        Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamHitbox);
    }
}