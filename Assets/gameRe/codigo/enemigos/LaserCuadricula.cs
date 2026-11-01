using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserCuadricula : MonoBehaviour
{
    private LineRenderer lr;
    private Vector2 puntoA;
    private Vector2 puntoB;
    private float tAviso;
    private float tActivo;
    private float grosorAviso;
    private float grosorLaser;
    private Color colorAviso;
    private Color colorLaser;
    private int danio;
    private float empuje;
    private LayerMask capaJugador;
    private float timerDanio;
    private bool disparando;

    private static Material matCompartido;

    public void Inicializar(Vector2 a, Vector2 b, float aviso, float duracion, float gAviso, float gLaser, Color cAviso, Color cLaser, int dmg, float fEmpuje, LayerMask capa)
    {
        TryGetComponent(out lr);
        puntoA = a;
        puntoB = b;
        tAviso = aviso;
        tActivo = duracion;
        grosorAviso = gAviso;
        grosorLaser = gLaser;
        colorAviso = cAviso;
        colorLaser = cLaser;
        danio = dmg;
        empuje = fEmpuje;
        capaJugador = capa;

        if (matCompartido == null)
        {
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s != null) matCompartido = new Material(s);
        }

        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.sharedMaterial = matCompartido;
        lr.sortingOrder = 10;

        ActualizarVisual(colorAviso, grosorAviso);
    }

    void Update()
    {
        if (!disparando)
        {
            tAviso -= Time.deltaTime;
            if (tAviso <= 0f)
            {
                disparando = true;
                ActualizarVisual(colorLaser, grosorLaser);
                ChequearImpacto();
            }
            return;
        }

        tActivo -= Time.deltaTime;
        timerDanio -= Time.deltaTime;

        if (timerDanio <= 0f)
        {
            ChequearImpacto();
            timerDanio = 0.15f;
        }

        if (tActivo <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void ActualizarVisual(Color c, float grosor)
    {
        lr.startColor = c;
        lr.endColor = c;
        lr.widthMultiplier = grosor;
        lr.SetPosition(0, puntoA);
        lr.SetPosition(1, puntoB);
    }

    private void ChequearImpacto()
    {
        Vector2 dir = puntoB - puntoA;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return;

        Vector2 dirNorm = dir / dist;
        Vector2 centro = (puntoA + puntoB) * 0.5f;
        float angulo = Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg;

        Collider2D[] hits = Physics2D.OverlapBoxAll(centro, new Vector2(dist, grosorLaser), angulo, capaJugador);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i];
            if (c.TryGetComponent(out Jugador jug) && jug.EsInvulnerablePorDash) continue;

            if (c.TryGetComponent(out Vida v))
            {
                if (v.EsInvulnerable) continue;
                v.RecibirDanio(danio);
            }
            if (c.TryGetComponent(out Empuje emp))
            {
                Vector2 dirEmp = ((Vector2)c.transform.position - centro).normalized;
                if (dirEmp == Vector2.zero) dirEmp = new Vector2(-dirNorm.y, dirNorm.x);
                emp.AplicarEmpuje(dirEmp, empuje);
            }
        }
    }
}
