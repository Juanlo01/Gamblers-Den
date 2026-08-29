using UnityEngine;
using System;
using System.Collections.Generic;

public class ThreadManager : MonoBehaviour
{
    private static ThreadManager _instance;
    private readonly Queue<Action> _queue = new Queue<Action>();
    private readonly object _lock = new object();

    void Awake()
    {
        // singleton pattern :o
        if (_instance != null) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this) { _instance = null; }
    }

    public static void Enqueue(Action action)
    {
        lock (_instance._lock)
        {
            _instance._queue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_lock)
        {
            while (_queue.Count > 0) { _queue.Dequeue().Invoke(); }
        }
    }
}
