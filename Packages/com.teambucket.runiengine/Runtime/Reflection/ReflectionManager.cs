#nullable enable
using System.Reflection;
using System;
using System.Collections.Generic;

namespace RuniEngine.Reflection
{
    public static class ReflectionManager
    {
        static ReflectionManager()
        {
            assemblys = AppDomain.CurrentDomain.GetAssemblies();

            {
                List<Type> result = new List<Type>();
                for (int assemblysIndex = 0; assemblysIndex < assemblys.Length; assemblysIndex++)
                {
                    Type[] types = assemblys[assemblysIndex].GetTypes();
                    for (int typesIndex = 0; typesIndex < types.Length; typesIndex++)
                    {
                        Type type = types[typesIndex];
                        result.Add(type);
                    }
                }

                types = result.ToArray();
            }
        }

        /// <summary>
        /// All loaded assemblys
        /// </summary>
        public static Assembly[] assemblys { get; }

        /// <summary>
        /// All loaded types
        /// </summary>
        public static Type[] types { get; }
    }
}
