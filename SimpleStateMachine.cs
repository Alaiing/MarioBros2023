using System;
using System.Collections.Generic;

namespace Oudidon
{
    public class SimpleStateMachine
    {
        public class State
        {
            public string name;
            public Action OnEnter;
            public Action<float, float> OnUpdate;
            public Action OnExit;
            public float timer;
        }

        private readonly Dictionary<string, State> _states = new Dictionary<string, State>();

        private State _currentState;
        public string CurrentState => _currentState?.name;
        public float CurrentStateTimer => _currentState.timer;

        public void AddState(string name, Action OnEnter, Action OnExit, Action<float, float> OnUpdate)
        {
            if (!_states.ContainsKey(name))
            {
                _states.Add(name, new State { name = name, OnEnter = OnEnter, OnExit = OnExit, OnUpdate = OnUpdate });
            }
        }

        public void SetState(string name)
        {
            _currentState?.OnExit?.Invoke();
            if (_states.TryGetValue(name, out _currentState))
            {
                _currentState.timer = 0;
                _currentState.OnEnter?.Invoke();
            }
        }

        public void Update(float deltaTime)
        {
            _currentState.timer += deltaTime;
            _currentState?.OnUpdate?.Invoke(deltaTime, _currentState.timer);
        }
    }
}
