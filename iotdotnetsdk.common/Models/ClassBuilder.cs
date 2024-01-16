using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace iotdotnetsdk.common.Models
{
    internal class ValueAttribute : Attribute
    {
        private string guid;
        public string Guid
        {
            get
            {
                return guid;
            }
        }

        private string tag;
        public string Tag
        {
            get { return tag; }
            set { tag = value; }
        }


        public ValueAttribute(string guid, string tag)
        {
            this.guid = guid;
            this.tag = tag;
        }

    }

    internal static class ClassBuilder
    {
        public static object CreateObject(string ClassName, List<NameValueType> properties, bool verify = false)
        {
            AssemblyName asemblyName = new AssemblyName(ClassName);
            TypeBuilder DynamicClass = CreateClass(asemblyName);
            DynamicClass.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var p in properties)
            {
                CreateProperty(DynamicClass, p.Name, Type.GetType(p.Type), p.Guid, p.Tag, verify);
            }

            Type type = DynamicClass.CreateType();
            var obj = Activator.CreateInstance(type);

            return obj;
        }

        private static TypeBuilder CreateClass(AssemblyName asemblyName)
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("RuleModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(asemblyName.FullName
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , null);
            return typeBuilder;
        }

        private static void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType, string guid, string tag, bool verify)
        {
            if ((!verify) && propertyType.Name.Equals("Double"))
            {
                propertyType = typeof(Nullable<>).MakeGenericType(typeof(double));
            }
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            Type[] ctorParams = new Type[] { typeof(string), typeof(string) };
            ConstructorInfo classCtorInfo = typeof(ValueAttribute).GetConstructor(ctorParams);

            CustomAttributeBuilder myCABuilder = new CustomAttributeBuilder(
                        classCtorInfo,
                        new object[] { guid, tag });
            propertyBuilder.SetCustomAttribute(myCABuilder);

            MethodBuilder setPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
