using Mono.Debugger.Soft;

namespace MonoTools.Debugger
{
    public class TypeSummary
    {
        private MethodMirror[] _methods;

        public TypeMirror TypeMirror { get; set; }

        public MethodMirror[] Methods
        {
            get
            {
                lock (this)
                {
                    if (_methods == null && TypeMirror != null)
                        _methods = TypeMirror.GetMethods();
                }

                return _methods;
            }
        }
    }
}