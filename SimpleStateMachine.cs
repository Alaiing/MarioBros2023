﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oudidon
{
    public class SimpleStateMachine
    {
        public class State
        {
            public string name;
            public Action OnEnter;
            public Action<float> OnUpdate;
            public Action OnExit;
        }

        private readonly Dictionary<string, State> _states = new Dictionary<string, State>();

        private State _currentState;
        public string CurrentState => _currentState?.name;

        public void AddState(string name, Action OnEnter, Action OnExit, Action<float> OnUpdate)
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
                _currentState.OnEnter?.Invoke();
            }
        }

        public void Update(float deltaTime)
        {
            _currentState?.OnUpdate(deltaTime);
        }
    }
}