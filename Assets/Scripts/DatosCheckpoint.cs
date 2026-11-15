using UnityEngine;

public static class DatosCheckpoint
{
    public static string escenaActual;
    public static Vector3 posicion;
    public static bool hayCheckpoint;

    public static void Guardar(string escena, Vector3 pos)
    {
        escenaActual = escena;
        posicion = pos;
        hayCheckpoint = true;
    }

    public static void Limpiar()
    {
        hayCheckpoint = false;
    }
}