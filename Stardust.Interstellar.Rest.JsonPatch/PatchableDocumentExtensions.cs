using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Stardust.Interstellar.Rest.JsonPatch
{
    public static class PatchableDocumentExtensions
    {
        private static IServiceCollection _serviceCollection;
        private static BuilderPair _builders;
        private static ConcurrentDictionary<Type, Type> documentTypes = new ConcurrentDictionary<Type, Type>();
        private static IServiceProvider _provider;

        public static IServiceCollection AddPatchDocument(this IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
            return serviceCollection;
        }

        public static IServiceProvider SetRootProvider(this IServiceProvider provider)
        {
            _provider = provider;
            return provider;
        }

        public static JsonPatchDocument<T> AsJsonPatch<T>(this IPatchableDocument document) where T : class
        {
            var p = document as PatchableDocumentBase<T>;
            return p?.AsJsonPatch();

        }

        public static T ToPatchDocument<T>(this T document) where T : class, IPatchableDocument
        {
            if (!documentTypes.TryGetValue(typeof(T), out var t))
            {
                lock (documentTypes)
                {
                    if (!documentTypes.TryGetValue(typeof(T), out t))
                        t = BuildPatchDocumentType<T>();
                    documentTypes.TryAdd(typeof(T), t);
                }
            }
            return (T)ActivatorUtilities.CreateInstance(_provider, t, document);
        }

        public class BuilderPair
        {
            public AssemblyBuilder AssemblyBuilder { get; set; }

            public ModuleBuilder ModuleBuilder { get; set; }
        }
        private static Type BuildPatchDocumentType<T>() where T : class
        {
            if (_builders == null)
            {
                var myAssemblyName = new AssemblyName();
                myAssemblyName.Name = Guid.NewGuid().ToString().Replace("-", "") + "_PatchDocuments";
                var ab = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);

                var myModuleBuilder = ab.DefineDynamicModule(myAssemblyName.Name);
                _builders = new BuilderPair { AssemblyBuilder = ab, ModuleBuilder = myModuleBuilder };

            }

            var type = _builders.ModuleBuilder.DefineType(
                "Stardust.Interstellar.Rest.JsonPatch.Documents" + typeof(T).Name.Substring(1),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(PatchableDocumentBase<T>),
                new Type[]
                {
                    typeof(T)
                });
            BuildMethod_ctor<T>(type);
            //type.DefineDefaultConstructor()
            foreach (var prop in typeof(T).GetProperties())
            {
                var property = type.DefineProperty(prop.Name, PropertyAttributes.None, CallingConventions.Standard,
                    prop.PropertyType, null);
                var get = BuildMethodget<T>(type, prop);
                var set = BuildMethodset<T>(type, prop);
                property.SetGetMethod(get);
                property.SetSetMethod(set);
            }

            return type.CreateTypeInfo();

        }

        private static MethodBuilder BuildMethodget<T>(TypeBuilder type, PropertyInfo prop) where T : class
        {
            // Declaring method builder
            // Method attributes
            var methodAttributes =
                MethodAttributes.Public
                | MethodAttributes.Virtual
                | MethodAttributes.Final
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot;
            var method = type.DefineMethod("get_" + prop.Name, methodAttributes);
            // Preparing Reflection instances
            FieldInfo field1 = typeof(PatchableDocumentBase<>).MakeGenericType(typeof(T)).GetField("_document", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // Setting return type
            method.SetReturnType(prop.PropertyType);
            // Adding parameters
            var gen = method.GetILGenerator();
            // Writing body
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field1);
            gen.Emit(OpCodes.Callvirt, prop.GetMethod);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;

        }

        private static MethodBuilder BuildMethodset<T>(TypeBuilder type, PropertyInfo prop)
        {
            // Declaring method builder
            // Method attributes
            var methodAttributes =
                  MethodAttributes.Public
                | MethodAttributes.Virtual
                | MethodAttributes.Final
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot;
            var method = type.DefineMethod("set_" + prop.Name, methodAttributes);
            FieldInfo field1 = typeof(PatchableDocumentBase<>).MakeGenericType(typeof(T)).GetField("_document", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // Preparing Reflection instances
            var method1 = typeof(Type).GetMethod(
                "GetTypeFromHandle",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
            typeof(RuntimeTypeHandle)
                    },
                null
                );
            var method2 = typeof(Expression).GetMethod(
                "Parameter",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
            typeof(Type),
            typeof(string)
                    },
                null
                );
            var method3 = typeof(T).GetMethod(
                "get_" + prop.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                    },
                null
                );
            var method4 = typeof(MethodBase).GetMethod(
                "GetMethodFromHandle",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
            typeof(RuntimeMethodHandle)
                    },
                null
                );
            var method5 = typeof(Expression).GetMethod(
                "Property",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
            typeof(Expression),
            typeof(MethodInfo)
                    },
                null
                );
            var lambdas = typeof(Expression).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == "Lambda" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(ParameterExpression[]) &&
                            m.GetParameters()[0].ParameterType == typeof(Expression) && m.ReturnType.IsConstructedGenericType);
            var method6 = lambdas.SingleOrDefault(
                );
            method6 = method6.MakeGenericMethod(typeof(Func<,>).MakeGenericType(typeof(T), prop.PropertyType));
            var method7 = typeof(PatchableDocumentBase<>).MakeGenericType(typeof(T))
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(s => s.Name == "SetReferenceType");
            method7 = method7.MakeGenericMethod(prop.PropertyType);
            var field8 = field1;//typeof(PatchableDocumentBase<>).MakeGenericType(typeof(T)).GetField("_document", BindingFlags.Public | BindingFlags.NonPublic);
            var p = typeof(T).GetProperty(prop.Name);
            var method9 = p.SetMethod;
            // Setting return type
            method.SetReturnType(typeof(void));
            // Adding parameters
            method.SetParameters(
                prop.PropertyType
                );
            // Parameter value
            var value = method.DefineParameter(1, ParameterAttributes.None, "value");
            var gen = method.GetILGenerator();
            // Preparing locals
            var expression = gen.DeclareLocal(typeof(ParameterExpression));
            // Writing body
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldtoken, typeof(T));
            gen.Emit(OpCodes.Call, method1);
            gen.Emit(OpCodes.Ldstr, "d");
            gen.Emit(OpCodes.Call, method2);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldtoken, method3);
            gen.Emit(OpCodes.Call, method4);
            gen.Emit(OpCodes.Castclass, typeof(MethodInfo));
            gen.Emit(OpCodes.Call, method5);
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Newarr, typeof(ParameterExpression));
            gen.Emit(OpCodes.Dup);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Stelem_Ref);
            gen.Emit(OpCodes.Call, method6);
            gen.Emit(OpCodes.Call, method7);
            gen.Emit(OpCodes.Pop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field8);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Callvirt, method9);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;

        }



        private static MethodBuilder BuildMethod_ctor<T>(TypeBuilder type)
        {
            // Declaring method builder
            // Method attributes
            var methodAttributes =
                MethodAttributes.Public
                | MethodAttributes.HideBySig;
            var method = type.DefineMethod(".ctor", methodAttributes);
            // Preparing Reflection instances
            var ctor1 = typeof(PatchableDocumentBase<>).MakeGenericType(typeof(T)).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                    typeof(T)
                },
                null
            );
            // Setting return type
            method.SetReturnType(typeof(void));
            // Adding parameters
            method.SetParameters(
                typeof(T)
            );
            // Parameter profile
            var profile = method.DefineParameter(1, ParameterAttributes.None, "profile");
            var gen = method.GetILGenerator();
            // Writing body
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, ctor1);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;

        }



        public static FieldBuilder BuildField_patchDocument<T>(TypeBuilder type)
        {
            var field = type.DefineField(
                "_patchDocument",
                typeof(JsonPatchDocument<>).MakeGenericType(typeof(T)),
                FieldAttributes.Private
            );
            return field;
        }

        public static FieldBuilder BuildField_document<T>(TypeBuilder type)
        {
            var field = type.DefineField(
                "_document",
                typeof(T),
                FieldAttributes.Private
            );
            return field;
        }




    }
}
