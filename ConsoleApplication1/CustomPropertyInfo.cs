using System;
using System.Globalization;
using System.Reflection;

namespace ConsoleApplication1
{
    public class CustomPropertyInfo : PropertyInfo
    {
        public CustomPropertyInfo(string name, Type propertyType)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name");
            }

            Name = name;
            PropertyType = propertyType;
        }

        public override string Name { get; }

        public override Type DeclaringType { get; }

        public override Type ReflectedType { get; }

        public override Type PropertyType { get; }

        public override PropertyAttributes Attributes { get; }

        public override bool CanRead { get; }

        public override bool CanWrite { get; }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}