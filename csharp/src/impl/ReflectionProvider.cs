namespace TenderBaseImpl
{
    using System;
    using System.Reflection;
    
    public interface ReflectionProvider
    {
        ConstructorInfo GetDefaultConstructor(Type cls);

        void SetInt(FieldInfo field, Object o, int val);

        void SetLong(FieldInfo field, Object o, long val);

        void SetShort(FieldInfo field, Object o, short val);

        void SetChar(FieldInfo field, Object o, char val);

        void SetByte(FieldInfo field, Object o, byte val);

        void SetFloat(FieldInfo field, Object o, float val);

        void SetDouble(FieldInfo field, Object o, double val);

        void SetBoolean(FieldInfo field, Object o, bool val);

        void Set(FieldInfo field, Object o, Object val);
    }
}

