using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Service
{
    class VoidDelegateBuilder
    {
        private readonly ModuleBuilder myModuleBuilder;

        public VoidDelegateBuilder(ModuleBuilder myModuleBuilder)
        {
            this.myModuleBuilder = myModuleBuilder;
        }

        private  static int typeCounter;

        internal Type CreateVoidDelegate(MethodInfo targetMethod, TypeBuilder parent, List<ParameterWrapper> methodParams)
        {
            typeCounter++;
            var typeBuilder = myModuleBuilder.DefineType(string.Format("TempModule.Controllers.{0}{1}{2}VoidDelegate{3}", targetMethod.DeclaringType.Name, targetMethod.Name, targetMethod.GetParameters().Length, typeCounter),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass,
                typeof(object));
            var imp = typeBuilder.DefineField("implementation", parent, FieldAttributes.Public);
            var baseController = typeof(ServiceWrapperBase<>).MakeGenericType(targetMethod.DeclaringType).GetField("implementation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var param = typeBuilder.DefineField("parameters", typeof(ParameterWrapper[]), FieldAttributes.Public);
            CreateVoidDelegateCtor(typeBuilder);

            // Declaring method builder
            // Method attributes
            var methodAttributes =
                  MethodAttributes.Assembly
                | MethodAttributes.HideBySig;
            MethodBuilder method = typeBuilder.DefineMethod(targetMethod.Name, methodAttributes);
            // Preparing Reflection instances
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
            method.SetReturnType(typeof(Task));
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

        private static void CreateVoidDelegateCtor(TypeBuilder typeBuilder)
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
