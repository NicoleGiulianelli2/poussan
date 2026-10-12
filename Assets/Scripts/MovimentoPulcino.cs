using UnityEngine;
using UnityEngine.InputSystem;
 
// Va sul Pulcino. WASD lo muove RISPETTO ALLA TELECAMERA:
// W = "in avanti" per come stai guardando, anche se hai girato la telecamera con Q/E.
// Tenendo premuto Shift corre.
[RequireComponent(typeof(CharacterController))]
public class MovimentoPulcino : MonoBehaviour
{
    public float velocita = 3f;       // metri al secondo
    public float corsa = 2f;          // con Shift va 2 volte più veloce
    public float girata = 720f;       // quanto velocemente si gira (gradi al secondo)
 
    CharacterController controller;
 
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }
 
    void Update()
    {
        var tastiera = Keyboard.current;
        if (tastiera == null) return;
 
        // 1. Leggo i tasti
        Vector3 direzione = Vector3.zero;
        if (tastiera.wKey.isPressed) direzione.z += 1;
        if (tastiera.sKey.isPressed) direzione.z -= 1;
        if (tastiera.dKey.isPressed) direzione.x += 1;
        if (tastiera.aKey.isPressed) direzione.x -= 1;
        if (direzione.sqrMagnitude > 1) direzione.Normalize();
 
        // 2. Giro la direzione come è girata la telecamera (solo in orizzontale)
        var cam = Camera.main;
        if (cam != null)
            direzione = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f) * direzione;
 
        // 3. Mi muovo (SimpleMove gestisce da solo gravità e muri)
        float v = velocita * (tastiera.leftShiftKey.isPressed ? corsa : 1f);
        controller.SimpleMove(direzione * v);
 
        // 4. Mi giro piano piano verso dove cammino
        if (direzione != Vector3.zero)
        {
            Quaternion voluta = Quaternion.LookRotation(direzione);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, voluta, girata * Time.deltaTime);
        }
    }
}
 