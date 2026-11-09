using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class EventoCadenas : MonoBehaviour
{
    [Header("Configuración del Tutorial (9 Cadenas)")]
    [SerializeField, Min(1)] private int pulsosPorDireccion = 2;
    [SerializeField] private bool centrarJugadorEnTrigger = false;
    [SerializeField] private bool impulsarConDashAlLiberarse = true;

    [Header("Visuales y Tirón Direccional")]
    [SerializeField] private Sprite spriteJugadorEncadenado;
    [SerializeField] private AnimationClip animJugadorEncadenado;
    [SerializeField] private GameObject[] objetosVisualesCadenas;
    [SerializeField, Min(0f)] private float distanciaTiron = 0.22f;
    [SerializeField, Min(0.05f)] private float duracionTiron = 0.16f;
    [SerializeField, Min(0f)] private float intensidadVibracion = 0.04f;

    [Header("Partículas al Romper Cadena")]
    [SerializeField] private bool emitirParticulasAlRomper = true;
    [SerializeField] private Color colorParticulasCadena = new Color(0.75f, 0.75f, 0.8f, 1f);

    [Header("Eventos (Opcional)")]
    public UnityEvent AlIniciarEncadenamiento;
    public UnityEvent AlRomperUnaCadena;
    public UnityEvent AlLiberarsePorCompleto;

    private bool yaSeActivo;
    private bool eventoEnCurso;

    private readonly int[] progresoMov = new int[4];
    private readonly int[] progresoAtk = new int[4];
    private int cadenasDireccionalesRotas;
    private int indiceVisualCadena;

    private Jugador jugador;
    private AtaqueJugador ataqueJugador;
    private Rigidbody2D rbJugador;
    private SpriteRenderer srJugador;
    private PlayerInput playerInput;
    private InputAction accionMover;
    private InputAction accionAtacar;
    private InputAction accionDash;

    private Vector3 posicionAnclada;
    private Vector2Int ultimaDirMovLeida;
    private Vector2Int ultimaDirAtkLeida;
    private bool esperandoSoltarDash;

    private Coroutine rutinaForcejeo;
    private static Material materialParticulas;

    void Awake()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].isTrigger = true;
        }

        if (objetosVisualesCadenas != null)
        {
            for (int i = 0; i < objetosVisualesCadenas.Length; i++)
            {
                if (objetosVisualesCadenas[i] != null)
                {
                    objetosVisualesCadenas[i].SetActive(false);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (yaSeActivo || !other.CompareTag("Player")) return;

        if (!other.TryGetComponent(out jugador))
        {
            jugador = Jugador.Instancia;
        }

        if (jugador == null) return;

        yaSeActivo = true;
        IniciarEvento();
    }

    private void IniciarEvento()
    {
        eventoEnCurso = true;
        cadenasDireccionalesRotas = 0;
        indiceVisualCadena = 0;

        for (int i = 0; i < 4; i++)
        {
            progresoMov[i] = 0;
            progresoAtk[i] = 0;
        }

        jugador.TryGetComponent(out rbJugador);
        srJugador = jugador.GetComponentInChildren<SpriteRenderer>();

        if (centrarJugadorEnTrigger)
        {
            posicionAnclada = transform.position;
            jugador.transform.position = posicionAnclada;
            if (rbJugador != null) rbJugador.position = posicionAnclada;
        }
        else
        {
            posicionAnclada = jugador.transform.position;
        }

        jugador.EstablecerEstadoEncadenado(true, spriteJugadorEncadenado, animJugadorEncadenado);

        if (jugador.TryGetComponent(out ataqueJugador))
        {
            ataqueJugador.EstablecerBloqueoExterno(true);
        }

        if (jugador.TryGetComponent(out playerInput) && playerInput.actions != null)
        {
            accionMover = playerInput.actions.FindAction("Move");
            accionAtacar = playerInput.actions.FindAction("Look");
            accionDash = playerInput.actions.FindAction("Dash");
        }

        ultimaDirMovLeida = LeerDireccionMover();
        ultimaDirAtkLeida = LeerDireccionAtacar();
        esperandoSoltarDash = LeerInputDash();

        if (objetosVisualesCadenas != null)
        {
            for (int i = 0; i < objetosVisualesCadenas.Length; i++)
            {
                if (objetosVisualesCadenas[i] != null)
                {
                    objetosVisualesCadenas[i].SetActive(true);
                }
            }
        }

        AlIniciarEncadenamiento?.Invoke();
    }

    void Update()
    {
        if (!eventoEnCurso || jugador == null) return;

        Vector2Int dirMovActual = LeerDireccionMover();
        Vector2Int dirAtkActual = LeerDireccionAtacar();
        bool presionandoDash = LeerInputDash();

        bool nuevoPulsoMov = dirMovActual != Vector2Int.zero && dirMovActual != ultimaDirMovLeida;
        bool nuevoPulsoAtk = dirAtkActual != Vector2Int.zero && dirAtkActual != ultimaDirAtkLeida;

        ultimaDirMovLeida = dirMovActual;
        ultimaDirAtkLeida = dirAtkActual;
        if (!presionandoDash) esperandoSoltarDash = false;

        if (cadenasDireccionalesRotas < 8)
        {
            if (nuevoPulsoMov)
            {
                ProcesarIntentoDireccional(progresoMov, dirMovActual);
            }

            if (nuevoPulsoAtk)
            {
                ProcesarIntentoDireccional(progresoAtk, dirAtkActual);
            }
        }
        else
        {
            if (presionandoDash && !esperandoSoltarDash)
            {
                esperandoSoltarDash = true;
                Vector2 dirFinal = dirMovActual != Vector2Int.zero ? (Vector2)dirMovActual : jugador.UltimaDireccion;
                RomperSiguienteCadena(dirFinal);
                LiberarJugador(dirFinal);
            }
        }
    }

    private void ProcesarIntentoDireccional(int[] arregloProgreso, Vector2Int dir)
    {
        int idx = ObtenerIndiceCardinal(dir);
        if (idx < 0) return;

        if (arregloProgreso[idx] >= pulsosPorDireccion)
        {
            IniciarAnimacionTiron((Vector2)dir * 0.4f);
            return;
        }

        arregloProgreso[idx]++;

        if (arregloProgreso[idx] >= pulsosPorDireccion)
        {
            cadenasDireccionalesRotas++;
            RomperSiguienteCadena(dir);
        }
        else
        {
            IniciarAnimacionTiron((Vector2)dir * 0.65f);
        }
    }

    private int ObtenerIndiceCardinal(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return 0;
        if (dir == Vector2Int.down) return 1;
        if (dir == Vector2Int.left) return 2;
        if (dir == Vector2Int.right) return 3;
        return -1;
    }

    private Vector2Int LeerDireccionMover()
    {
        Vector2 v = Vector2.zero;

        if (accionMover != null)
        {
            v = accionMover.ReadValue<Vector2>();
        }

        if (v.sqrMagnitude < 0.25f)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed) v.y -= 1f;
                if (kb.dKey.isPressed) v.x += 1f;
                if (kb.aKey.isPressed) v.x -= 1f;
            }

            var gp = Gamepad.current;
            if (v.sqrMagnitude < 0.25f && gp != null)
            {
                v = gp.leftStick.ReadValue();
            }
        }

        return ConvertirACardinal(v);
    }

    private Vector2Int LeerDireccionAtacar()
    {
        Vector2 v = Vector2.zero;

        if (accionAtacar != null)
        {
            v = accionAtacar.ReadValue<Vector2>();
        }

        if (v.sqrMagnitude < 0.25f)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.isPressed) v.y += 1f;
                if (kb.downArrowKey.isPressed) v.y -= 1f;
                if (kb.rightArrowKey.isPressed) v.x += 1f;
                if (kb.leftArrowKey.isPressed) v.x -= 1f;
            }

            var gp = Gamepad.current;
            if (v.sqrMagnitude < 0.25f && gp != null)
            {
                v = gp.rightStick.ReadValue();
            }
        }

        return ConvertirACardinal(v);
    }

    private Vector2Int ConvertirACardinal(Vector2 v)
    {
        if (v.sqrMagnitude < 0.25f) return Vector2Int.zero;

        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
        {
            return v.x > 0f ? Vector2Int.right : Vector2Int.left;
        }
        return v.y > 0f ? Vector2Int.up : Vector2Int.down;
    }

    private bool LeerInputDash()
    {
        if (accionDash != null && accionDash.WasPressedThisFrame()) return true;

        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame))
        {
            return true;
        }

        var gp = Gamepad.current;
        if (gp != null && (gp.rightShoulder.wasPressedThisFrame || gp.leftShoulder.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame))
        {
            return true;
        }

        return false;
    }

    private void RomperSiguienteCadena(Vector2 direccionIntento)
    {
        Vector3 posEfecto = posicionAnclada;

        if (objetosVisualesCadenas != null && indiceVisualCadena < objetosVisualesCadenas.Length)
        {
            GameObject objCadena = objetosVisualesCadenas[indiceVisualCadena];
            if (objCadena != null)
            {
                posEfecto = objCadena.transform.position;
                objCadena.SetActive(false);
            }
        }

        indiceVisualCadena++;

        if (emitirParticulasAlRomper)
        {
            GenerarParticulasRotura(posEfecto);
        }

        IniciarAnimacionTiron(direccionIntento);
        AlRomperUnaCadena?.Invoke();
    }

    private void IniciarAnimacionTiron(Vector2 direccion)
    {
        if (rutinaForcejeo != null) StopCoroutine(rutinaForcejeo);
        rutinaForcejeo = StartCoroutine(RutinaTironDireccional(direccion));
    }

    private IEnumerator RutinaTironDireccional(Vector2 direccion)
    {
        if (jugador == null) yield break;

        if (srJugador != null && Mathf.Abs(direccion.x) > 0.1f)
        {
            srJugador.flipX = direccion.x < 0f;
        }

        Vector3 dir3D = new Vector3(direccion.x, direccion.y, 0f);
        float t = 0f;
        float d = Mathf.Max(0.04f, duracionTiron);

        while (t < d && eventoEnCurso && jugador != null)
        {
            t += Time.deltaTime;
            float progreso = t / d;
            float curvaTiron = Mathf.Sin(progreso * Mathf.PI);

            Vector3 desplazamiento = dir3D * (distanciaTiron * curvaTiron);
            Vector3 vibracion = (Vector3)(Random.insideUnitCircle * (intensidadVibracion * (1f - progreso)));
            Vector3 posFinal = posicionAnclada + desplazamiento + vibracion;

            jugador.transform.position = posFinal;
            if (rbJugador != null) rbJugador.position = posFinal;

            yield return null;
        }

        if (eventoEnCurso && jugador != null)
        {
            jugador.transform.position = posicionAnclada;
            if (rbJugador != null) rbJugador.position = posicionAnclada;
        }

        rutinaForcejeo = null;
    }

    private void LiberarJugador(Vector2 dirSalida)
    {
        eventoEnCurso = false;

        if (rutinaForcejeo != null)
        {
            StopCoroutine(rutinaForcejeo);
            rutinaForcejeo = null;
        }

        jugador.transform.position = posicionAnclada;
        if (rbJugador != null) rbJugador.position = posicionAnclada;

        if (ataqueJugador != null)
        {
            ataqueJugador.EstablecerBloqueoExterno(false);
        }

        jugador.EstablecerEstadoEncadenado(false, null, null);

        if (dirSalida.sqrMagnitude > 0.01f)
        {
            jugador.EstablecerDireccion(dirSalida);
        }

        if (impulsarConDashAlLiberarse)
        {
            jugador.ForzarDashInmediato();
        }

        AlLiberarsePorCompleto?.Invoke();
    }

    private void GenerarParticulasRotura(Vector3 posicion)
    {
        if (materialParticulas == null)
        {
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s != null) materialParticulas = new Material(s);
        }

        GameObject obj = new GameObject("ParticulasCadenaRota");
        obj.transform.position = posicion;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = obj.GetComponent<ParticleSystemRenderer>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (materialParticulas != null) psr.sharedMaterial = materialParticulas;
        psr.sortingOrder = 20;

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = colorParticulasCadena;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        ps.Play();
        Destroy(obj, 0.5f);
    }
}
