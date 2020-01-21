using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Service
{
    public class DelegateBuilder
    {
        private readonly ModuleBuilder myModuleBuilder;

        public DelegateBuilder(ModuleBuilder myModuleBuilder)
        {
            this.myModuleBuilder = myModuleBuilder;
        }

        private static int typeCounter;

        public Type CreateDelegate(MethodInfo targetMethod, TypeBuilder parent, List<ParameterWrapper> methodParams)
        {
            typeCounter++;
            var typeBuilder = myModuleBuilder.DefineType(string.Format("TempModule.Controllers.{0}{1}{2}Delegate{3}", targetMethod.DeclaringType.Name, targetMethod.Name, targetMethod.GetParameters().Length, typeCounter),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass,
                typeof(object));
            //var typeBuilder = myModuleBuilder.DefineType(string.Format("{0}{1}{2}Delegate", targetMethod.DeclaringType.Name, targetMethod.Name, targetMethod.GetParameters().Length), TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(object));
            var imp = typeBuilder.DefineField("implementation", parent, FieldAttributes.Public);
            var baseController = typeof(ServiceWrapperBase<>).MakeGenericType(targetMethod.DeclaringType).GetField("implementation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var param = typeBuilder.DefineField("parameters", typeof(ParameterWrapper[]), FieldAttributes.Public);
            CreateDelegateCtor(typeBuilder);

            // Declaring method builder
            // Method attributes
            System.Reflection.MethodAttributes methodAttributes =
                System.Reflection.MethodAttributes.Assembly
                | System.Reflection.MethodAttributes.HideBySig;
            MethodBuilder method = typeBuilder.DefineMethod(targetMethod.Name, methodAttributes);
            // Preparing Reflection instances
            //FieldInfo field1 = typeof(<> c__DisplayClass2_0).GetField("<>4__this", BindingFlags.Public | BindingFlags.NonPublic);
            //FieldInfo field2 = typeof(Stardust.Interstellar.Rest.Service.ServiceWrapperBase<>).MakeGenericType(typeof(ITestApi)).GetField("implementation", BindingFlags.Public | BindingFlags.NonPublic);
            //FieldInfo field3 = typeof(<> c__DisplayClass2_0).GetField("serviceParameters", BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo method4 = typeof(ParameterWrapper).GetMethod(
                "get_value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                },
                null
            );
            MethodInfo method5 = targetMethod;
            // Setting return type
            method.SetReturnType(targetMethod.ReturnType);
            // Adding parameters
            ILGenerator gen = method.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, imp);
            gen.Emit(OpCodes.Ldfld, baseController);
            var iii = 0;
            foreach (var item in methodParams)
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, param);
                EmitHelpers.EmitInt32(gen, iii);
                gen.Emit(OpCodes.Ldelem_Ref);
                gen.Emit(OpCodes.Callvirt, method4);
                if (item.Type.IsValueType)
                    gen.Emit(OpCodes.Unbox_Any, item.Type);
                else
                    gen.Emit(OpCodes.Castclass, item.Type);
                iii++;
            }
            gen.Emit(OpCodes.Callvirt, method5);
            gen.Emit(OpCodes.Ret);
            // finished


            var t = typeBuilder.CreateType();
            return t;
        }

        private static void CreateDelegateCtor(TypeBuilder typeBuilder)
        {
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard | CallingConventions.HasThis, new Type[] { });
            var baseCtor = typeof(object).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            var gen = ctor.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, baseCtor);
            gen.Emit(OpCodes.Ret);
        }

    }
}