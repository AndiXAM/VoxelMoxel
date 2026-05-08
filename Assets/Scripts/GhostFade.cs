using UnityEngine;

public class GhostFade : MonoBehaviour
{
    public float fadeSpeed = 0.5f; // Как быстро исчезает фантом
    private Material mat;

    void Start()
    {
        // Получаем материал фантома
        mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        // Плавно уменьшаем альфа-канал (прозрачность)
        Color color = mat.color;
        color.a -= fadeSpeed * Time.deltaTime;
        mat.color = color;

        // Когда стал полностью невидимым - удаляем объект
        if (color.a <= 0)
        {
            Destroy(gameObject);
        }
    }
}