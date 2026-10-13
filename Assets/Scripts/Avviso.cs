using UnityEngine;
 
// Scritte in basso sullo schermo ("Premi B per...", "La lavatrice finirà in...").
// Se più oggetti scrivono nello stesso momento, le scritte si impilano invece di sovrapporsi.
// Uso, dentro OnGUI():   Avviso.Disegna("testo");
public static class Avviso
{
    static int ultimoFrame = -1;
    static int indice;
 
    public static void Disegna(string messaggio)
    {
        if (string.IsNullOrEmpty(messaggio) || Event.current.type != EventType.Repaint) return;
        if (Time.frameCount != ultimoFrame)
        {
            ultimoFrame = Time.frameCount;
            indice = 0;
        }
        const float w = 380f, h = 40f, spazio = 6f;
        float y = Screen.height - 50f - h - indice * (h + spazio);
        indice++;
        GUI.Box(new Rect((Screen.width - w) / 2f, y, w, h), messaggio);
    }
}