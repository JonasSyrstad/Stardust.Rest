using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Client
{
    public static class ReflectionEmitHelper
    {

        public static void SetModuleBuilderConstructo(Func<string, BuilderPair> constructoMethod)
        {
            ConstructoMethod = constructoMethod;
        }

        public static Func<string, BuilderPair> ConstructoMethod = s =>
          {
              var myAssemblyName = new AssemblyName();
              myAssemblyName.Name = Guid.NewGuid().ToString().Replace("-", "") + "_RestWrapper";
              //ExtensionsFactory.Logger?.Message("Creating dynamic assembly {0}", myAssemblyName.FullName);
              var ab = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);

              var myModuleBuilder = ab.DefineDynamicModule(myAssemblyName.Name);
              return new BuilderPair { AssemblyBuilder = ab, ModuleBuilder = myModuleBuilder };
          };
    }

    public class BuilderPair
    {
        public AssemblyBuilder AssemblyBuilder { get; set; }

        public ModuleBuilder ModuleBuilder { get; set; }
    }
    internal class ProxyBuilder
    {
        private readonly Type interfaceType;

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public ProxyBuilder(Type interfaceType)
        {
            this.interfaceType = interfaceType;
        }

        private AssemblyBuilder myAssemblyBuilder;


        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public Type Build()

        {
            var ab = AssemblyBuilder(out var myModuleBuilder);
            myAssemblyBuilder = ab;
            var type = ReflectionTypeBuilder(myModuleBuilder, interfaceType.Name + "_dynimp");
            ctor(type);
            foreach (var methodInfo in interfaceType.GetMethods().Length == 0 ? interfaceType.GetInterfaces().First().GetMethods() : interfaceType.GetMethods())
            {
                if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                    BuildMethodAsync(type, methodInfo);
                else if (methodInfo.ReturnType != typeof(void))
                    BuildMethod(type, methodInfo);
                else
                    BuildVoidMethod(type, methodInfo);
            }

            var result = type.CreateTypeInfo();
            //try
            //{
            //    if (ConfigurationManager.AppSettings["stardust.saveGeneratedAssemblies"] == "true")
            //    {
            //        ExtensionsFactory.GetService<ILogger>().Message("Saveing generated assembly");
            //        myAssemblyBuilder.Save("dyn.dll");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    ExtensionsFactory.GetService<ILogger>()?.Error(ex);
            //}
            return result;
        }

        private AssemblyBuilder AssemblyBuilder(out ModuleBuilder myModuleBuilder)
        {
            if (ReflectionEmitHelper.ConstructoMethod != null)
            {
                BuilderPair builders = ReflectionEmitHelper.ConstructoMethod(Guid.NewGuid().ToString().Replace("-", "") + "_RestWrapper");
                myModuleBuilder = builders.ModuleBuilder;
                return builders.AssemblyBuilder;
            }
            throw new InvalidOperationException("");
        }

        private MethodBuilder BuildMethod(TypeBuilder type, MethodInfo serviceAction)
        {
            const MethodAttributes methodAttributes = MethodAttributes.Public
                                                      | MethodAttributes.Virtual
                                                      | MethodAttributes.Final
                                                      | MethodAttributes.HideBySig
                                                      | MethodAttributes.NewSlot;
            var method = type.DefineMethod(serviceAction.Name, methodAttributes);
            // Preparing Reflection instances
            var method1 = typeof(RestWrapper).GetMethod(
                "GetParameters",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[]{
                         typeof(string),
                         typeof(object[])
                     },
                null
                );
            MethodInfo method2 = typeof(RestWrapper).GetMethod(
                "Invoke",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[]{
                         typeof(string),
                         typeof(ParameterWrapper[])
                     },
                null
                ).MakeGenericMethod(serviceAction.ReturnType);


            // Setting return type

            method.SetReturnType(serviceAction.ReturnType);
            // Adding parameters
            method.SetParameters(serviceAction.GetParameters().Select(p => p.ParameterType).ToArray());
            var i = 1;
            foreach (var parameterInfo in serviceAction.GetParameters())
            {
                var param = method.DefineParameter(i, ParameterAttributes.None, parameterInfo.Name);
                i++;
            }
            ILGenerator gen = method.GetILGenerator();
            // Preparing locals
            LocalBuilder par = gen.DeclareLocal(typeof(Object[]));
            LocalBuilder parameters = gen.DeclareLocal(typeof(ParameterWrapper[]));
            LocalBuilder result = gen.DeclareLocal(serviceAction.ReturnType);
            LocalBuilder str = gen.DeclareLocal(serviceAction.ReturnType);
            // Preparing labels
            Label label55 = gen.DefineLabel();
            // Writing body
            var ps = serviceAction.GetParameters();
            EmitHelpers.EmitInt32(gen, ps.Length);
            gen.Emit(OpCodes.Newarr, typeof(object));
            for (int j = 0; j < ps.Length; j++)
            {
                gen.Emit(OpCodes.Dup);
                EmitHelpers.EmitInt32(gen, j);
                EmitHelpers.EmitLdarg(gen, j + 1);

                var paramType = ps[j].ParameterType;
                if (paramType.IsValueType)
                {
                    gen.Emit(OpCodes.Box, paramType);
                }
                gen.Emit(OpCodes.Stelem_Ref);
            }
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, serviceAction.Name);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Call, method1);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, serviceAction.Name);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Call, method2);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldloc_2);
            gen.Emit(OpCodes.Stloc_3);
            gen.Emit(OpCodes.Br_S, label55);
            gen.MarkLabel(label55);
            gen.Emit(OpCodes.Ldloc_3);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;

        }

        private MethodBuilder BuildVoidMethod(TypeBuilder type, MethodInfo serviceAction)
        {
            const MethodAttributes methodAttributes = MethodAttributes.Public
                                                      | MethodAttributes.Virtual
                                                      | MethodAttributes.Final
                                                      | MethodAttributes.HideBySig
                                                      | MethodAttributes.NewSlot;
            var method = type.DefineMethod(serviceAction.Name, methodAttributes);
            // Preparing Reflection instances
            var method1 = typeof(RestWrapper).GetMethod(
                "GetParameters",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[]{
                         typeof(string),
                         typeof(object[])
                     },
                null
                );
            MethodInfo method2 = typeof(RestWrapper).GetMethod(
                "InvokeVoid",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[]{
                         typeof(string),
                         typeof(ParameterWrapper[])
                     },
                null
                );

            // Adding parameters
            method.SetParameters(serviceAction.GetParameters().Select(p => p.ParameterType).ToArray());
            var i = 1;
            foreach (var parameterInfo in serviceAction.GetParameters())
            {
                var param = method.DefineParameter(i, ParameterAttributes.None, parameterInfo.Name);
                i++;
            }
            ILGenerator gen = method.GetILGenerator();
            // Preparing locals
            LocalBuilder par = gen.DeclareLocal(typeof(Object[]));
            LocalBuilder parameters = gen.DeclareLocal(typeof(ParameterWrapper[]));
            // Preparing labels
            Label label55 = gen.DefineLabel();
            // Writing body
            var ps = serviceAction.GetParameters();
            EmitHelpers.EmitInt32(gen, ps.Length);
            gen.Emit(OpCodes.Newarr, typeof(object));
            for (int j = 0; j < ps.Length; j++)
            {
                gen.Emit(OpCodes.Dup);
                EmitHelpers.EmitInt32(gen, j);
                EmitHelpers.EmitLdarg(gen, j + 1);
                var paramType = ps[j].ParameterType;
                if (paramType.IsValueType)
                {
                    gen.Emit(OpCodes.Box, paramType);
                }
                gen.Emit(OpCodes.Stelem_Ref);
            }
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, serviceAction.Name);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Call, method1);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, serviceAction.Name);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Call, method2);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;

        }

        private MethodBuilder BuildMethodAsync(TypeBuilder type, MethodInfo serviceAction)
        {
            const MethodAttributes methodAttributes = MethodAttributes.Public
                                                      | MethodAttributes.Virtual
                                                      | MethodAttributes.Final
                                                      | MethodAttributes.HideBySig
                                                      | MethodAttributes.NewSlot;
            var method = type.DefineMethod(serviceAction.Name, methodAttributes);
            // Preparing Reflection instances
            var method1 = typeof(RestWrapper).GetMethod(
                "GetParameters",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[]{
                         typeof(string),
                         typeof(object[])
                     },
                null
                );

            var method2 = typeof(RestWrapper).GetMethod(serviceAction.ReturnType.GetGenericArguments().Length == 0 ? "InvokeVoidAsync" : "InvokeAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(ParameterWrapper[]) }, null);
            if (serviceAction.ReturnType.GenericTypeArguments.Any())
                method2 = method2.MakeGenericMethod(serviceAction.ReturnType.GenericTypeArguments);


            // Setting return type

            method.SetReturnType(serviceAction.ReturnType);
            // Adding parameters
            method.SetParameters(serviceAction.GetParameters().Select(p => p.ParameterType).ToArray());
            var i = 1;
            foreach (var parameterInfo in serviceAction.GetParameters())
            {
                var param = method.DefineParameter(i, ParameterAttributes.None, parameterInfo.Name);
                i++;
            }
            ILGenerator gen = method.GetILGenerator();
            // Preparing locals
            LocalBuilder par = gen.DeclareLocal(typeof(Object[]));
            LocalBuilder parameters = gen.DeclareLocal(typeof(ParameterWrapper[]));
            LocalBuilder result = gen.DeclareLocal(serviceAction.ReturnType);
            LocalBuilder str = gen.DeclareLocal(serviceAction.ReturnType);
            // Preparing labels
            Label label55 = gen.DefineLabel();
            // Writing body
            var ps = serviceAction.GetParameters();
            EmitHelpers.EmitInt32(gen, ps.Length);
            gen.Emit(OpCodes.Newarr, typeof(object));
            for (int j = 0; j < ps.Length; j++)
            {
                gen.Emit(OpCodes.Dup);
                EmitHelpers.EmitInt32(gen, j);
                EmitHelpers.EmitLdarg(gen, j + 1);
                var paramType = ps[j].ParameterType;
                if (paramType.IsValueType)
                {
                    gen.Emit(OpCodes.Box, paramType);
                }
                gen.Emit(OpCodes.Stelem_Ref);

            }
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, serviceAction.Name);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Call, method1);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, serviceAction.Name);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Call, method2);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldloc_2);
            gen.Emit(OpCodes.Stloc_3);
            gen.Emit(OpCodes.Br_S, label55);
            gen.MarkLabel(label55);
            gen.Emit(OpCodes.Ldloc_3);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;
        }



        public ConstructorBuilder ctor(TypeBuilder type)
        {
            const MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig;

            var method = type.DefineConstructor(methodAttributes, CallingConventions.Standard | CallingConventions.HasThis, new[] { typeof(IAuthenticationHandler), typeof(IHeaderHandlerFactory), typeof(TypeWrapper), typeof(IServiceProvider) });
            var authenticationHandler = method.DefineParameter(1, ParameterAttributes.None, "authenticationHandler");
            var headerHandlers = method.DefineParameter(2, ParameterAttributes.None, "headerHandlers");
            var interfaceType = method.DefineParameter(3, ParameterAttributes.None, "interfaceType");
            var serviceLocator = method.DefineParameter(4, ParameterAttributes.None, "serviceLocator");
            var ctor1 = typeof(RestWrapper).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IAuthenticationHandler), typeof(IHeaderHandlerFactory), typeof(TypeWrapper), typeof(IServiceProvider) }, null);

            var gen = method.GetILGenerator();
            // Writing body
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Ldarg, (short)4);
            gen.Emit(OpCodes.Call, ctor1);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ret);

            // finished
            return method;
        }



        private TypeBuilder ReflectionTypeBuilder(ModuleBuilder module, string typeName)
        {
            var type = module.DefineType("TempModule." + Guid.NewGuid().ToString().Replace("-", "") + "." + typeName,
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(RestWrapper),
                new[] { interfaceType }
                );
            return type;
        }
    }
}