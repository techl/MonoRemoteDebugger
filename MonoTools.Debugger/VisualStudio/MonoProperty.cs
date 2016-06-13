using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Mono.Debugger.Soft;
using Microsoft.MIDebugEngine;

namespace MonoTools.Debugger.Debugger.VisualStudio
{
    internal class ExpandedProperty
    {
        private readonly StackFrame frame;
        private readonly LocalVariable localVariable;
        private readonly List<Mirror> allProperties;

        public ExpandedProperty(TypeMirror typeMirror, StackFrame frame, LocalVariable localVariable)
        {
            this.frame = frame;
            this.localVariable = localVariable;
            var properties = typeMirror.GetProperties().Cast<Mirror>();
            var methods = typeMirror.GetMethods().Cast<Mirror>();
            var fields = typeMirror.GetFields().Cast<Mirror>();
            var children = properties.Concat(methods).Concat(fields);
            allProperties = children.ToList();
        }
    }

    internal class MonoProperty : IDebugProperty2
    {
        private readonly StackFrame frame;
        private readonly LocalVariable variable;
        private readonly TypeMirror mirror;
        private readonly Mirror childMirror;

        public MonoProperty(StackFrame frame, LocalVariable variable)
            : this(frame, variable, null, null)
        {
        }

        public MonoProperty(StackFrame frame, LocalVariable localVariable, TypeMirror typeMirror, Mirror childMirror)
        {
            this.frame = frame;
            this.variable = localVariable;
            this.mirror = typeMirror;
            this.childMirror = childMirror;
        }

        public int EnumChildren(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, ref Guid guidFilter,
            enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout,
            out IEnumDebugPropertyInfo2 ppEnum)
        {
            var typeMirror = variable.Type;
            var properties = typeMirror.GetProperties().Cast<Mirror>();
            var methods = typeMirror.GetMethods();
            var fields = typeMirror.GetFields();
            var children = properties.Concat(methods).Concat(fields).ToList();

            ppEnum = new AD7PropertyEnum(children.Select(x => new MonoProperty(frame, variable, typeMirror, x).GetDebugPropertyInfo(dwFields)).ToArray());
            return VSConstants.S_OK;
        }

        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            throw new NotImplementedException();
        }

        public int GetExtendedInfo(ref Guid guidExtendedInfo, out object pExtendedInfo)
        {
            throw new NotImplementedException();
        }

        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new NotImplementedException();
        }

        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            throw new NotImplementedException();
        }

        public int GetParent(out IDebugProperty2 ppParent)
        {
            throw new NotImplementedException();
        }

        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, uint dwTimeout,
            IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            rgpArgs = null;
            pPropertyInfo[0] = GetDebugPropertyInfo(dwFields);
            return VSConstants.S_OK;
        }

        public int GetReference(out IDebugReference2 ppReference)
        {
            throw new NotImplementedException();
        }

        public int GetSize(out uint pdwSize)
        {
            throw new NotImplementedException();
        }

        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue,
            uint dwTimeout)
        {
            throw new NotImplementedException();
        }

        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            throw new NotImplementedException();
        }

        internal DEBUG_PROPERTY_INFO GetDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields)
        {
            var propertyInfo = new DEBUG_PROPERTY_INFO();
            var info = GetMirrorInfo();
            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME) != 0)
            {
                propertyInfo.bstrFullName = info != null ? info.Name : variable.Name;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
            {
                propertyInfo.bstrName = info != null ? info.Name : variable.Name;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE) != 0)
            {
                if (info != null)
                {
                    if (info.PropertyType != null)
                        propertyInfo.bstrType = info.PropertyType.FullName;
                }
                else
                    propertyInfo.bstrType = variable.Type.Namespace + "." + variable.Type.Name;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0 && info == null)
            {
                Value value = frame.GetValue(variable);
                if (value is ObjectMirror)
                {
                    var obj = ((ObjectMirror)value);
                    MethodMirror toStringMethod = obj.Type.GetMethod("ToString");
                    value = obj.InvokeMethod(frame.Thread, toStringMethod, Enumerable.Empty<Value>().ToList(),
                        InvokeOptions.DisableBreakpoints);
                    propertyInfo.bstrValue = ((StringMirror)value).Value;
                }
                else if (value is PrimitiveValue)
                {
                    var obj = ((PrimitiveValue)value);
                    if (obj.Value != null)
                        propertyInfo.bstrValue = obj.Value.ToString();
                }

                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0)
            {
                propertyInfo.dwAttrib = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY;

                if (IsExpandable())
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
                }
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB;
            }

            if (((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP) != 0) || IsExpandable())
            {
                propertyInfo.pProperty = this;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP;
            }

            return propertyInfo;
        }

        private PropertyChildInfo GetMirrorInfo()
        {
            PropertyChildInfo childInfo = null;
            var methodMirror = childMirror as MethodMirror;
            var propertyMirror = childMirror as PropertyInfoMirror;
            var fieldMirror = childMirror as FieldInfoMirror;
            //if (methodMirror != null)
            //{
            //    childInfo = new PropertyChildInfo();
            //    childInfo.Name = methodMirror.Name;
            //    childInfo.FullName = methodMirror.FullName;
            //}
            //else

            if (propertyMirror != null)
            {
                childInfo = new PropertyChildInfo();
                childInfo.Name = propertyMirror.Name;
                childInfo.FullName = propertyMirror.PropertyType.FullName;
                childInfo.PropertyType = propertyMirror.PropertyType;
            }
            else if (fieldMirror != null)
            {
                childInfo = new PropertyChildInfo();
                childInfo.Name = fieldMirror.Name;
                childInfo.FullName = fieldMirror.FieldType.FullName;
                childInfo.PropertyType = fieldMirror.FieldType;
            }

            return childInfo;
        }

        private bool IsExpandable()
        {
            return true;
        }
    }

    internal class PropertyChildInfo
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public TypeMirror PropertyType { get; set; }
    }
}