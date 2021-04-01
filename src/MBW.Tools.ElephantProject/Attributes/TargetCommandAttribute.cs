using System;

namespace MBW.Tools.ElephantProject.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class TargetCommandAttribute : Attribute
    {
        public Type CommandType { get; }

        public TargetCommandAttribute(Type commandType)
        {
            CommandType = commandType;
        }
    }
}