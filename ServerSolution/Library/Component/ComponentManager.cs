using System;
using System.Collections.Generic;

namespace Library.Component
{
    public class ComponentManager : IComponentManager, IDisposable
    {
        // Here is where we can store components
        private Dictionary<Type, object> _components = new Dictionary<Type, object>();

        public void AddComponent<T>(T component) where T : class
        {
            _components[typeof(T)] = component;
        }
        

        public T GetComponent<T>() where T : class
        {
            _components.TryGetValue(typeof(T), out object component);
            return component as T;
        }

        public void RemoveComponent<T>() where T : class
        {
            _components.Remove(typeof(T));
        }
        public void Dispose()
        {
            _components.Clear();
        }
    }

}
