using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GestorProgresoSalas
{
    private static readonly HashSet<string> salasCompletadas = new HashSet<string>();
    private static bool tieneCheckpoint;
    private static string escenaCheckpoint;
    private static Vector3 posicionCheckpoint;

    public static bool EstaSalaCompletada(string idSala)
    {
        return !string.IsNullOrEmpty(idSala) && salasCompletadas.Contains(idSala);
    }

    public static void MarcarSalaCompletada(string idSala)
    {
        if (!string.IsNullOrEmpty(idSala))
        {
            salasCompletadas.Add(idSala);
        }
    }

    public static void GuardarCheckpointEntrada(Vector3 posicionEntrada)
    {
        tieneCheckpoint = true;
        escenaCheckpoint = SceneManager.GetActiveScene().name;
        posicionCheckpoint = posicionEntrada;
    }

    public static bool IntentarObtenerCheckpointEnEscenaActual(out Vector3 posicion)
    {
        if (tieneCheckpoint && escenaCheckpoint == SceneManager.GetActiveScene().name)
        {
            posicion = posicionCheckpoint;
            return true;
        }

        posicion = Vector3.zero;
        return false;
    }

    public static string ObtenerEscenaUltimoCheckpoint()
    {
        return tieneCheckpoint ? escenaCheckpoint : SceneManager.GetActiveScene().name;
    }

    public static void ReiniciarTodoElProgreso()
    {
        salasCompletadas.Clear();
        tieneCheckpoint = false;
        escenaCheckpoint = string.Empty;
    }
}
