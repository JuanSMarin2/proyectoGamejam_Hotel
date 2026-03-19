using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScrollingBackgorund : MonoBehaviour
{
    public float speed;

    [SerializeField] private Renderer backgroundRenderer;
    void Update()
    {
     backgroundRenderer.material.mainTextureOffset += new Vector2(0, speed * Time.deltaTime);

    }
}
