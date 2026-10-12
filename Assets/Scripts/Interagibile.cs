using UnityEngine;

public class Interagibile : MonoBehaviour
{
    public string nome = "Oggetto";

    public void Interagisci()
    {
        Debug.Log("Hai toccato: " + nome);
    }
}