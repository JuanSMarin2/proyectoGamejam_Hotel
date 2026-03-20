using UnityEngine;

public class HeartBeat : MonoBehaviour
{
    [Header("Configuración del Pulso")]
    [SerializeField] private float _amplitude = 0.1f; 
    [SerializeField] private float _frequency = 2f; 

    private Vector3 _initialScale;

    void Start()
    {
    
        _initialScale = transform.localScale;
    }

    void Update()
    {
  
        float pulse = Mathf.Sin(Time.time * _frequency) * _amplitude;


        transform.localScale = _initialScale + new Vector3(pulse, pulse, 0);
    }
}