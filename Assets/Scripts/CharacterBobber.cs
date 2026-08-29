using UnityEngine;

public class CharacterBobber : MonoBehaviour
{
    private float xWidth = 0.025f;
    private float yWidth = 0.05f;
    private float speed = 0.5f;

    private Transform _target;
    private Vector3 _origin;
    private float _angle;
    private float _speed;

    void Awake()
    {
        _target = transform;
        _origin = _target.localPosition;
        _speed = speed + Random.Range(-0.1f, 0.1f);
        _angle = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        _angle += _speed * Time.deltaTime;

        _target.localPosition = _origin + new Vector3(
            Mathf.Cos(_angle) * xWidth,
            Mathf.Sin(_angle) * yWidth,
            0f);
    }
}
