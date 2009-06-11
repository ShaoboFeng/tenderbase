namespace TenderBaseImpl
{
    using System;
    using System.Reflection;
    
    public class StandardReflectionProvider : ReflectionProvider
    {
        internal static readonly Type[] defaultConstructorProfile = new Type[0];

        public virtual ConstructorInfo GetDefaultConstructor(Type type)
        {
            //BindingFlags flags = BindingFlags.DeclaredOnly;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return type.GetConstructor(flags, null, defaultConstructorProfile, null);
            //return type.GetConstructor(defaultConstructorProfile);
        }

        public virtual void SetInt(FieldInfo field, object o, int val)
        {
            field.SetValue(o, val);
        }

        public virtual void SetLong(FieldInfo field, object o, long val)
        {
            field.SetValue(o, val);
        }

        public virtual void SetShort(FieldInfo field, object o, short val)
        {
            field.SetValue(o, (short) val);
        }

        public virtual void SetChar(FieldInfo field, object o, char val)
        {
            field.SetValue(o, (char) val);
        }

        public virtual void SetByte(FieldInfo field, object o, byte val)
        {
            field.SetValue(o, val);
        }

        public virtual void SetFloat(FieldInfo field, object o, float val)
        {
            field.SetValue(o, val);
        }

        public virtual void SetDouble(FieldInfo field, object o, double val)
        {
            field.SetValue(o, val);
        }

        public virtual void SetBoolean(FieldInfo field, object o, bool val)
        {
            field.SetValue(o, val);
        }

        public virtual void Set(FieldInfo field, object o, object val)
        {
            field.SetValue(o, val);
        }
    }
}

