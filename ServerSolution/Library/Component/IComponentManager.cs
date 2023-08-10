using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Component
{
    public interface IComponentManager
    {
        void AddComponent<T>(T component) where T : class;
        T GetComponent<T>() where T : class;
        void RemoveComponent<T>() where T : class;
    }
}
