using Microsoft.Web.Http;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Stardust.Interstellar.Rest.Annotations.Service;
using Stardust.Particles;

namespace Stardust.Interstellar.Rest.Service
{
    internal class ServiceBuilder
    {
        private AssemblyBuilder myAssemblyBuilder;

        private ModuleBuilder myModuleBuilder;

        static ServiceBuilder()
        {
            ReflectionEmitHelper.SetModuleBuilderConstructo(builder);
        }

        private static BuilderPair builder(string arg)
        {
            var myCurrentDomain = AppDomain.CurrentDomain;
            var myAssemblyName = new AssemblyName();
            myAssemblyName.Name = Guid.NewGuid().ToString().Replace("-", "") + "_ServiceWrapper";
            var myAssemblyBuilder = myCurrentDomain.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.RunAndSave);
            var myModuleBuilder = myAssemblyBuilder.DefineDynamicModule("TempModule", "service.dll");

            return new BuilderPair { ModuleBuilder = myModuleBuilder, AssemblyBuilder = myAssemblyBuilder };
        }

        public ServiceBuilder()
        {
            //var myCurrentDomain = AppDomain.CurrentDomain;
            //var myAssemblyName = new AssemblyName();
            //myAssemblyName.Name = Guid.NewGuid().ToString().Replace("-", "") + "_ServiceWrapper";
            //myAssemblyBuilder = myCurrentDomain.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.RunAndSave);
            //myModuleBuilder = myAssemblyBuilder.DefineDynamicModule("TempModule", "service.dll");
            //var myAssemblyName = new AssemblyName();
            //myAssemblyName.Name = Guid.NewGuid().ToString().Replace("-", "") + "_ServiceWrapper";
            //ExtensionsFactory.GetService<ILogger>()?.Message("Creating dynamic assembly {0}", myAssemblyName.FullName);
            //myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.RunAndCollect);
            //myModuleBuilder = myAssemblyBuilder.DefineDynamicModule(myAssemblyName.Name);
            var builders = builder(Guid.NewGuid().ToString().Replace("-", "") + "_ServiceWrapper");
            myModuleBuilder = builders.ModuleBuilder;
            myAssemblyBuilder = builders.AssemblyBuilder;
        }

        public Type CreateServiceImplementation<T>(IServiceLocator serviceLocator)
        {
            return CreateServiceImplementation(typeof(T), serviceLocator);
        }

        public Type CreateServiceImplementation(Type interfaceType, IServiceLocator serviceLocator)
        {
            try
            {
                serviceLocator?.GetService<ILogger>()?.Message("Generating webapi controller for {0}", interfaceType.FullName);
                var type = CreateServiceType(interfaceType);
                ctor(type, interfaceType);
                foreach (var methodInfo in interfaceType.GetMethods().Length == 0 ? interfaceType.GetInterfaces().First().GetMethods() : interfaceType.GetMethods())
                {
                    if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                    {
                        if (methodInfo.ReturnType.GetGenericArguments().Length == 0)
                        {
                            BuildAsyncVoidMethod(type, methodInfo, serviceLocator);
                        }
                        else BuildAsyncMethod(type, methodInfo, serviceLocator);
                    }
                    else
                    {
                        if (methodInfo.ReturnType == typeof(void)) BuildVoidMethod(type, methodInfo, serviceLocator);
                        else BuildMethod(type, methodInfo, serviceLocator);
                    }
                }
                return type.CreateType();
            }
            catch (Exception ex)
            {
                serviceLocator.GetService<ILogger>()?.Error(ex);
                serviceLocator.GetService<ILogger>()?.Message("Skipping type: {0}", interfaceType.FullName);
                if (ServiceFactory.ThrowOnException) throw;
                return null;
            }
        }

        public MethodBuilder InternalMethodBuilder(TypeBuilder type, MethodInfo implementationMethod, Func<MethodInfo, MethodBuilder, Type[], List<ParameterWrapper>, MethodBuilder> bodyBuilder, IServiceLocator serviceLocator)
        {
            var method = DefineMethod(type, implementationMethod, out List<ParameterWrapper> methodParams, out Type[] pTypes, serviceLocator);
            return bodyBuilder(implementationMethod, method, pTypes, methodParams);
        }

        public MethodBuilder BuildMethod(TypeBuilder type, MethodInfo implementationMethod, IServiceLocator serviceLocator)
        {
            return InternalMethodBuilder(type, implementationMethod, BodyImplementer, serviceLocator);
        }

        private static MethodBuilder BodyImplementer(MethodInfo implementationMethod, MethodBuilder method, Type[] pTypes, List<ParameterWrapper> methodParams)
        {
            MethodInfo gatherParams = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType)
                .GetMethod("GatherParameters", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(String), typeof(Object[]) }, null);
            var baseType = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType);
            var implementation = baseType.GetRuntimeFields().Single(f => f.Name == "implementation");
            MethodInfo getValue = typeof(ParameterWrapper).GetMethod("get_value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            MethodInfo createResponse = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod("CreateResponse", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (implementationMethod.ReturnType == typeof(void)) createResponse = createResponse.MakeGenericMethod(typeof(object));
            else createResponse = createResponse.MakeGenericMethod(implementationMethod.ReturnType);
            MethodInfo method9 = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType)
                .GetMethod("CreateErrorResponse", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Exception) }, null);



            // Setting return type

            ILGenerator gen = method.GetILGenerator();
            // Preparing locals
            LocalBuilder parameters = gen.DeclareLocal(typeof(Object[]));
            LocalBuilder serviceParameters = gen.DeclareLocal(typeof(ParameterWrapper[]));
            LocalBuilder result = gen.DeclareLocal(typeof(String));
            LocalBuilder message = gen.DeclareLocal(typeof(HttpResponseMessage));
            LocalBuilder ex = gen.DeclareLocal(typeof(Exception));
            // Preparing labels
            Label label97 = gen.DefineLabel();
            // Writing body
            gen.Emit(OpCodes.Nop);
            gen.BeginExceptionBlock();
            gen.Emit(OpCodes.Nop);
            var ps = pTypes;
            EmitHelpers.EmitInt32(gen, ps.Length);
            gen.Emit(OpCodes.Newarr, typeof(object));
            for (int j = 0; j < ps.Length; j++)
            {
                gen.Emit(OpCodes.Dup);
                EmitHelpers.EmitInt32(gen, j);
                EmitHelpers.EmitLdarg(gen, j + 1);
                var paramType = ps[j];
                if (paramType.IsValueType)
                {
                    gen.Emit(OpCodes.Box, paramType);
                }
                gen.Emit(OpCodes.Stelem_Ref);
            }
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, implementationMethod.Name);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Call, gatherParams);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, implementation);
            int iii = 0;
            foreach (var parameterWrapper in methodParams)
            {
                gen.Emit(OpCodes.Ldloc_1);
                EmitHelpers.EmitInt32(gen, iii);
                gen.Emit(OpCodes.Ldelem_Ref);
                gen.Emit(OpCodes.Callvirt, getValue);
                if (parameterWrapper.Type.IsValueType) gen.Emit(OpCodes.Unbox_Any, parameterWrapper.Type);
                else gen.Emit(OpCodes.Castclass, parameterWrapper.Type);
                iii++;
            }
            gen.Emit(OpCodes.Callvirt, implementationMethod);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldc_I4, 200);
            if (implementationMethod.ReturnType != typeof(void)) gen.Emit(OpCodes.Ldloc_2);
            gen.Emit(OpCodes.Call, createResponse);
            gen.Emit(OpCodes.Stloc_3);
            gen.Emit(OpCodes.Leave_S, label97);
            gen.BeginCatchBlock(typeof(Exception));
            gen.Emit(OpCodes.Stloc_S, 4);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldloc_S, 4);
            gen.Emit(OpCodes.Call, method9);
            gen.Emit(OpCodes.Stloc_3);
            gen.Emit(OpCodes.Leave_S, label97);
            gen.EndExceptionBlock();
            gen.MarkLabel(label97);
            gen.Emit(OpCodes.Ldloc_3);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;
        }

        private static MethodBuilder DefineMethod(TypeBuilder type, MethodInfo implementationMethod, out List<ParameterWrapper> methodParams, out Type[] pTypes, IServiceLocator serviceLocator)
        {

            // Declaring method builder
            // Method attributes
            const MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
            var method = type.DefineMethod(implementationMethod.Name, methodAttributes);
            // Preparing Reflection instances
            

            var route = typeof(RouteAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(String) }, null);
            var httpGet = httpMethodAttribute(implementationMethod, serviceLocator);
            var uriAttrib = typeof(FromUriAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            var bodyAttrib = typeof(FromBodyAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);

            method.SetReturnType(typeof(Task).IsAssignableFrom(implementationMethod.ReturnType) ? typeof(Task<HttpResponseMessage>) : typeof(HttpResponseMessage));
            // Adding parameters
            methodParams = GetMethodParams(implementationMethod, serviceLocator);
            pTypes = methodParams.Where(p => p.In != InclutionTypes.Header).Select(p => p.Type).ToArray();
            method.SetParameters(pTypes.ToArray());
            // Parameter id
            int pid = 1;
            foreach (var parameterWrapper in methodParams.Where(p => p.In != InclutionTypes.Header))
            {
                try
                {
                    var p = method.DefineParameter(pid, ParameterAttributes.None, parameterWrapper.Name);
                    if (parameterWrapper.In == InclutionTypes.Path) p.SetCustomAttribute(new CustomAttributeBuilder(uriAttrib, new Type[] { }));
                    else if (parameterWrapper.In == InclutionTypes.Body) p.SetCustomAttribute(new CustomAttributeBuilder(bodyAttrib, new Type[] { }));
                    pid++;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            DefineAuthorizeAttributes(implementationMethod, method);
            // Adding custom attributes to method
            // [RouteAttribute]
            var template = implementationMethod.GetCustomAttribute<RouteAttribute>()?.Template ?? implementationMethod.GetCustomAttribute<VerbAttribute>()?.Route;
            if (template == null) template = ExtensionsFactory.GetServiceTemplate(implementationMethod, serviceLocator);
            if (template == null) template = "";
            method.SetCustomAttribute(new CustomAttributeBuilder(route, new[] { template }));
            BuildServiceDescriptionAttribute(implementationMethod, method);
            // [HttpGetAttribute]
            method.SetCustomAttribute(new CustomAttributeBuilder(httpGet, new Type[] { }));
            ConstructorInfo ctor5 = typeof(ResponseTypeAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Type) }, null);
            method.SetCustomAttribute(new CustomAttributeBuilder(ctor5, new object[] { GetReturnType(implementationMethod) }));
            try
            {
                var attributeCreators = implementationMethod.GetCustomAttributes().OfType<ICreateImplementationAttribute>();
                foreach (var createImplementationAttribute in attributeCreators)
                {
                    method.SetCustomAttribute(createImplementationAttribute.CreateAttribute());
                }
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }
            return method;
        }

        private static void BuildServiceDescriptionAttribute(MethodInfo implementationMethod, MethodBuilder method)
        {
            var descriptionAttribute = implementationMethod.GetCustomAttribute<ServiceDescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                var ctor = typeof(ServiceDescriptionAttribute).GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { typeof(string) }, null);
                var props = new[]
                {
                    typeof(ServiceDescriptionAttribute).GetProperty("Tags"),
                    typeof(ServiceDescriptionAttribute).GetProperty("Summary"),
                    typeof(ServiceDescriptionAttribute).GetProperty("IsDeprecated"),
                    typeof(ServiceDescriptionAttribute).GetProperty("Responses")
                };
                var values = new object[]
                {
                    descriptionAttribute.Tags,
                    descriptionAttribute.Summary,
                    descriptionAttribute.IsDeprecated,
                    descriptionAttribute.Responses
                };
                method.SetCustomAttribute(new CustomAttributeBuilder(ctor, new[] { descriptionAttribute.Description }, props,
                    values));
            }
        }

        private static void DefineAuthorizeAttributes(MethodInfo implementationMethod, MethodBuilder method)
        {
            var authorizeAttribs = implementationMethod.GetCustomAttributes<AuthorizeWrapperAttribute>().ToList();
            var classAuthorizeAttribs = implementationMethod.DeclaringType.GetCustomAttributes<AuthorizeWrapperAttribute>();
            var authorizeWrapperAttributes = classAuthorizeAttribs as AuthorizeWrapperAttribute[] ?? classAuthorizeAttribs.ToArray();
            if (classAuthorizeAttribs != null && authorizeWrapperAttributes.Any()) authorizeAttribs.AddRange(authorizeWrapperAttributes);
            if (authorizeAttribs.Any())
            {
                foreach (var authorizeWrapperAttribute in authorizeAttribs)
                {
                    var authAtrib = authorizeWrapperAttribute.GetAutorizationAttribute();
                    var ctorParams = authorizeWrapperAttribute.GetConstructorParameters();
                    var ctor = authAtrib.GetType().GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, (from p in ctorParams select p.GetType()).ToArray(), null);
                    var props = authAtrib.GetType().GetProperties().Where(p => p.SetMethod != null);
                    var propVals = props.Select(p => p.GetMethod.Invoke(authAtrib, null));
                    method.SetCustomAttribute(new CustomAttributeBuilder(ctor, ctorParams, props.ToArray(), propVals.ToArray()));

                }
            }
        }

        private static Type GetReturnType(MethodInfo implementationMethod)
        {
            if (typeof(Task).IsAssignableFrom(implementationMethod.ReturnType))
            {
                if (implementationMethod.ReturnType.IsGenericType) return implementationMethod.ReturnType.GenericTypeArguments.FirstOrDefault();
                else return typeof(void);
            }

            return implementationMethod.ReturnType;
        }

        private static List<ParameterWrapper> GetMethodParams(MethodInfo implementationMethod, IServiceLocator serviceLocator)
        {
            var resolver = serviceLocator.GetService<IServiceParameterResolver>();
            var resolvedParams = resolver?.ResolveParameters(implementationMethod);
            if (resolvedParams != null && resolvedParams.Count() == implementationMethod.GetParameters().Length) return resolvedParams.ToList();
            var methodParams = new List<ParameterWrapper>();
            foreach (var parameterInfo in implementationMethod.GetParameters())
            {
                var @in = parameterInfo.GetCustomAttribute<InAttribute>(true);
                if (@in == null)
                {
                    var fromBody = parameterInfo.GetCustomAttribute<FromBodyAttribute>(true);
                    if (fromBody != null) @in = new InAttribute(InclutionTypes.Body);
                    if (@in == null)
                    {
                        var fromUri = parameterInfo.GetCustomAttribute<FromUriAttribute>(true);
                        if (fromUri != null) @in = new InAttribute(InclutionTypes.Path);
                    }
                }
                methodParams.Add(new ParameterWrapper { Name = parameterInfo.Name, Type = parameterInfo.ParameterType, In = @in?.InclutionType ?? InclutionTypes.Body });
            }
            return methodParams;
        }

        public MethodBuilder BuildVoidMethod(TypeBuilder type, MethodInfo implementationMethod, IServiceLocator serviceLocator)
        {

            return InternalMethodBuilder(type, implementationMethod, VoidMethodBuilder, serviceLocator);
        }

        private MethodBuilder VoidMethodBuilder(MethodInfo implementationMethod, MethodBuilder method, Type[] pTypes, List<ParameterWrapper> methodParams)
        {
            MethodInfo gatherParams = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod(
                "GatherParameters",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                              typeof(String),
                              typeof(Object[])
                          },
                null
                );
            var baseType = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType);
            var implementation = baseType.GetRuntimeFields().Single(f => f.Name == "implementation");
            MethodInfo getValue = typeof(ParameterWrapper).GetMethod(
                "get_value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                          },
                null
                );
            MethodInfo createResponse = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod("CreateResponse", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (implementationMethod.ReturnType == typeof(void))
                createResponse = createResponse.MakeGenericMethod(typeof(object));
            else
                createResponse = createResponse.MakeGenericMethod(implementationMethod.ReturnType);
            MethodInfo method9 = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod(
                "CreateErrorResponse",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                              typeof(Exception)
                          },
                null
                );
            ILGenerator gen = method.GetILGenerator();
            // Preparing locals
            LocalBuilder parameters = gen.DeclareLocal(typeof(Object[]));
            LocalBuilder serviceParameters = gen.DeclareLocal(typeof(ParameterWrapper[]));
            LocalBuilder message = gen.DeclareLocal(typeof(HttpResponseMessage));
            LocalBuilder ex = gen.DeclareLocal(typeof(Exception));
            // Writing body
            gen.Emit(OpCodes.Nop);
            gen.BeginExceptionBlock();
            gen.Emit(OpCodes.Nop);
            var ps = pTypes;
            EmitHelpers.EmitInt32(gen, ps.Length);
            gen.Emit(OpCodes.Newarr, typeof(object));
            for (int j = 0; j < ps.Length; j++)
            {
                gen.Emit(OpCodes.Dup);
                EmitHelpers.EmitInt32(gen, j);
                EmitHelpers.EmitLdarg(gen, j + 1);
                var paramType = ps[j];
                if (paramType.IsValueType)
                {
                    gen.Emit(OpCodes.Box, paramType);
                }
                gen.Emit(OpCodes.Stelem_Ref);

            }
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, implementationMethod.Name);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Call, gatherParams);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, implementation);
            int iii = 0;
            foreach (var parameterWrapper in methodParams)
            {
                gen.Emit(OpCodes.Ldloc_1);
                EmitHelpers.EmitInt32(gen, iii);//gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ldelem_Ref);
                gen.Emit(OpCodes.Callvirt, getValue);
                if (parameterWrapper.Type.IsValueType)
                {
                    gen.Emit(OpCodes.Unbox_Any, parameterWrapper.Type);

                }
                else
                    gen.Emit(OpCodes.Castclass, parameterWrapper.Type);
                iii++;
            }
            gen.Emit(OpCodes.Callvirt, implementationMethod);
            if (implementationMethod.ReturnType != typeof(void))
                gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldc_I4, 200);
            if (implementationMethod.ReturnType != typeof(void))
                gen.Emit(OpCodes.Ldloc_2);
            else { gen.Emit(OpCodes.Ldnull); }

            gen.Emit(OpCodes.Call, createResponse);
            gen.Emit(OpCodes.Stloc_2);
            gen.BeginCatchBlock(typeof(Exception));
            gen.Emit(OpCodes.Stloc_3);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldloc_3);
            gen.Emit(OpCodes.Call, method9);
            gen.Emit(OpCodes.Stloc_2);
            gen.EndExceptionBlock();
            gen.Emit(OpCodes.Ldloc_2);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;
        }

        private static ConstructorInfo httpMethodAttribute(MethodInfo implementationMethod, IServiceLocator serviceLocator)
        {
            var httpMethod = serviceLocator.GetService<IWebMethodConverter>()?.GetHttpMethods(implementationMethod);
            if (httpMethod != null && httpMethod.Any())
            {
                switch (httpMethod.First().Method.ToUpper())
                {
                    case "GET":
                        return typeof(HttpGetAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
                    case "POST":
                        return typeof(HttpPostAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
                    case "PUT":
                        return typeof(HttpPutAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
                    case "DELETE":
                        return typeof(HttpDeleteAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
                    case "OPTIONS":
                        return typeof(HttpOptionsAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
                    case "HEAD":
                        return typeof(HttpHeadAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
                    case "PATCH":
                        return typeof(HttpPatchAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
                }
            }
            var attribs = implementationMethod.GetCustomAttributes().ToList();


            if (attribs.Any(p => p is HttpGetAttribute))
                return typeof(HttpGetAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is HttpPostAttribute))
                return typeof(HttpPostAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is HttpPutAttribute))
                return typeof(HttpPutAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is HttpDeleteAttribute))
                return typeof(HttpDeleteAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is HttpOptionsAttribute))
                return typeof(HttpOptionsAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is HttpHeadAttribute))
                return typeof(HttpHeadAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is HttpPatchAttribute))
                return typeof(HttpPatchAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);

            if (attribs.Any(p => p is GetAttribute))
                return typeof(HttpGetAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is PostAttribute))
                return typeof(HttpPostAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is PutAttribute))
                return typeof(HttpPutAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is DeleteAttribute))
                return typeof(HttpDeleteAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is OptionsAttribute))
                return typeof(HttpOptionsAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is HeadAttribute))
                return typeof(HttpHeadAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            if (attribs.Any(p => p is PatchAttribute))
                return typeof(HttpPatchAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            return typeof(HttpGetAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);



        }

        public MethodBuilder BuildAsyncMethod(TypeBuilder type, MethodInfo implementationMethod, IServiceLocator serviceLocator)
        {
            return InternalMethodBuilder(type, implementationMethod, (a, b, c, d) => BuildAsyncMethodBody(a, b, c, d, type), serviceLocator);
        }
        private static readonly int typeCounter = 0;



        private MethodBuilder BuildAsyncMethodBody(MethodInfo implementationMethod, MethodBuilder method, Type[] pTypes, List<ParameterWrapper> methodParams, TypeBuilder parent)
        {
            MethodInfo gatherParams = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod(
                    "GatherParameters",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[]{
                                  typeof(string),
                                  typeof(object[])
                              },
                    null
                    );
            var delegateType = new DelegateBuilder(myModuleBuilder).CreateDelegate(implementationMethod, parent, methodParams);
            var delegateCtor = delegateType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            var baseType = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType);
            var implementation = baseType.GetRuntimeFields().Single(f => f.Name == "implementation");
            MethodInfo getValue = typeof(ParameterWrapper).GetMethod(
                "get_value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                          },
                null
                );
            ConstructorInfo ctor9 = typeof(System.Func<>).MakeGenericType(typeof(Task<>).MakeGenericType(implementationMethod.ReturnType.GenericTypeArguments)).GetConstructor(
       BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
       null,
       new Type[]{
            typeof(Object),
            typeof(IntPtr)
           },
       null
       );

            //  MethodInfo createResponse = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod("CreateResponseAsync", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            //createResponse = createResponse.MakeGenericMethod(implementationMethod.ReturnType.GetGenericArguments());
            MethodInfo method9 = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod(
                "CreateErrorResponse",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                                  typeof(Exception)
                          },
                null
                );

            var delegateMethod = delegateType.GetMethod(implementationMethod.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);//.MakeGenericMethod(implementationMethod.ReturnType.GenericTypeArguments);
            var method10 = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType)
                .GetMethod("ExecuteMethodAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            method10 = method10.MakeGenericMethod(implementationMethod.ReturnType.GenericTypeArguments);
            FieldInfo field7 = delegateType.GetField("parameters", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field5 = delegateType.GetField("implementation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo fromResult = typeof(Task).GetMethod("FromResult").MakeGenericMethod(typeof(HttpResponseMessage));
            ILGenerator gen = method.GetILGenerator();
            // Preparing locals
            LocalBuilder del = gen.DeclareLocal(delegateType);
            LocalBuilder serviceParameters = gen.DeclareLocal(typeof(object[]));
            LocalBuilder result = gen.DeclareLocal(typeof(Task<>).MakeGenericType(typeof(HttpResponseMessage)));
            LocalBuilder ex = gen.DeclareLocal(typeof(Exception));
            // Preparing labels
            Label label97 = gen.DefineLabel();
            // Writing body
            gen.Emit(OpCodes.Nop);
            gen.BeginExceptionBlock();
            gen.Emit(OpCodes.Newobj, delegateCtor);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Stfld, field5);
            var ps = pTypes;
            EmitHelpers.EmitInt32(gen, ps.Length);
            gen.Emit(OpCodes.Newarr, typeof(object));
            for (int j = 0; j < ps.Length; j++)
            {
                gen.Emit(OpCodes.Dup);
                EmitHelpers.EmitInt32(gen, j);
                EmitHelpers.EmitLdarg(gen, j + 1);
                var paramType = ps[j];
                if (paramType.IsValueType)
                {
                    gen.Emit(OpCodes.Box, paramType);
                }
                gen.Emit(OpCodes.Stelem_Ref);

            }
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, implementationMethod.Name);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Call, gatherParams);

            gen.Emit(OpCodes.Stfld, field7);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldftn, delegateMethod);
            gen.Emit(OpCodes.Newobj, ctor9);
            gen.Emit(OpCodes.Call, method10);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Leave_S, label97);
            gen.Emit(OpCodes.Leave_S, label97);
            gen.BeginCatchBlock(typeof(Exception));
            gen.Emit(OpCodes.Stloc_S, 3);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldloc_S, 3);
            gen.Emit(OpCodes.Call, method9);
            gen.Emit(OpCodes.Call, fromResult);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Leave_S, label97);
            gen.EndExceptionBlock();
            gen.MarkLabel(label97);
            gen.Emit(OpCodes.Ldloc_2);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;
        }

        public MethodBuilder BuildAsyncVoidMethod(TypeBuilder type, MethodInfo implementationMethod, IServiceLocator serviceLocator)
        {
            return InternalMethodBuilder(type, implementationMethod, (a, b, c, d) => BuildAsyncVoidMethodBody(a, b, c, d, type), serviceLocator);
        }

        private MethodBuilder BuildAsyncVoidMethodBody(MethodInfo implementationMethod, MethodBuilder method, Type[] pTypes, List<ParameterWrapper> methodParams, TypeBuilder parent)
        {
            MethodInfo gatherParams = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod(
                     "GatherParameters",
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                     null,
                     new Type[]{
                                  typeof(string),
                                  typeof(object[])
                               },
                     null
                     );
            var delegateType = new VoidDelegateBuilder(myModuleBuilder).CreateVoidDelegate(implementationMethod, parent, methodParams);
            var delegateCtor = delegateType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
            var baseType = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType);
            var implementation = baseType.GetRuntimeFields().Single(f => f.Name == "implementation");
            MethodInfo getValue = typeof(ParameterWrapper).GetMethod(
                "get_value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                          },
                null
                );
            ConstructorInfo ctor9 = typeof(System.Func<>).MakeGenericType(typeof(Task)).GetConstructor(
       BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
       null,
       new Type[]{
            typeof(Object),
            typeof(IntPtr)
           },
       null
       );

            MethodInfo createResponse = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod("CreateResponseAsync", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            //createResponse = createResponse.MakeGenericMethod(implementationMethod.ReturnType.GetGenericArguments());
            MethodInfo method9 = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType).GetMethod(
                "CreateErrorResponse",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[]{
                                  typeof(Exception)
                          },
                null
                );

            var delegateMethod = delegateType.GetMethod(implementationMethod.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);//.MakeGenericMethod(implementationMethod.ReturnType.GenericTypeArguments);
            var method10 = typeof(ServiceWrapperBase<>).MakeGenericType(implementationMethod.DeclaringType)
                .GetMethod("ExecuteMethodVoidAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo field7 = delegateType.GetField("parameters", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field5 = delegateType.GetField("implementation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo fromResult = typeof(Task).GetMethod("FromResult").MakeGenericMethod(typeof(HttpResponseMessage));
            ILGenerator gen = method.GetILGenerator();
            // Preparing locals
            LocalBuilder del = gen.DeclareLocal(delegateType);
            LocalBuilder serviceParameters = gen.DeclareLocal(typeof(object[]));
            LocalBuilder result = gen.DeclareLocal(typeof(Task<>).MakeGenericType(typeof(HttpResponseMessage)));
            LocalBuilder ex = gen.DeclareLocal(typeof(Exception));
            // Preparing labels
            Label label97 = gen.DefineLabel();
            // Writing body
            gen.Emit(OpCodes.Nop);
            gen.BeginExceptionBlock();
            gen.Emit(OpCodes.Newobj, delegateCtor);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Stfld, field5);
            var ps = pTypes;
            EmitHelpers.EmitInt32(gen, ps.Length);
            gen.Emit(OpCodes.Newarr, typeof(object));
            for (int j = 0; j < ps.Length; j++)
            {
                gen.Emit(OpCodes.Dup);
                EmitHelpers.EmitInt32(gen, j);
                EmitHelpers.EmitLdarg(gen, j + 1);
                var paramType = ps[j];
                if (paramType.IsValueType)
                {
                    gen.Emit(OpCodes.Box, paramType);
                }
                gen.Emit(OpCodes.Stelem_Ref);

            }
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, implementationMethod.Name);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Call, gatherParams);

            gen.Emit(OpCodes.Stfld, field7);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldftn, delegateMethod);
            gen.Emit(OpCodes.Newobj, ctor9);
            gen.Emit(OpCodes.Call, method10);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Leave_S, label97);
            gen.Emit(OpCodes.Leave_S, label97);
            gen.BeginCatchBlock(typeof(Exception));
            gen.Emit(OpCodes.Stloc_S, 3);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldloc_S, 3);
            gen.Emit(OpCodes.Call, method9);
            gen.Emit(OpCodes.Call, fromResult);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Leave_S, label97);
            gen.EndExceptionBlock();
            gen.MarkLabel(label97);
            gen.Emit(OpCodes.Ldloc_2);
            gen.Emit(OpCodes.Ret);
            // finished
            return method;
        }

        public ConstructorBuilder ctor(TypeBuilder type, Type interfaceType)
        {
            const MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig;

            var method = type.DefineConstructor(methodAttributes, CallingConventions.Standard | CallingConventions.HasThis, new[] { interfaceType, typeof(IServiceLocator) });
            var implementation = method.DefineParameter(1, ParameterAttributes.None, "implementation");
            var serviceLocator = method.DefineParameter(2, ParameterAttributes.None, "serviceLocator");
            var ctor1 = typeof(ServiceWrapperBase<>).MakeGenericType(interfaceType).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { interfaceType, typeof(IServiceLocator) }, null);

            var gen = method.GetILGenerator();
            // Writing body
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Call, ctor1);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ret);

            // finished
            return method;
        }

        private TypeBuilder CreateServiceType(Type interfaceType)
        {
            var routePrefix = GetRoutePrefix(interfaceType);
            var type = myModuleBuilder.DefineType("TempModule.Controllers." + interfaceType.Name.Remove(0, 1) + "Controller", TypeAttributes.Public | TypeAttributes.Class, typeof(ServiceWrapperBase<>).MakeGenericType(interfaceType));
            var attributeCreators = interfaceType.GetCustomAttributes().OfType<ICreateImplementationAttribute>();
            foreach (var createImplementationAttribute in attributeCreators)
            {
                type.SetCustomAttribute(createImplementationAttribute.CreateAttribute());
            }
            var version = interfaceType.GetCustomAttribute<VersionAttribute>();
            if (version != null)
            {
                var versionCtor = typeof(ApiVersionAttribute).GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[]{
                        typeof(string)
                    },
                    null
                );
                type.SetCustomAttribute(new CustomAttributeBuilder(versionCtor, new object[] { version.Version }, new[] { typeof(ApiVersionAttribute).GetProperty("Deprecated") }, new object[] { version.Deprecated }));
            }
            var obsolete = interfaceType.GetCustomAttribute<ObsoleteAttribute>();
            if (obsolete != null)
            {
                var obsoleteCtor = typeof(ObsoleteAttribute).GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[]{
                        typeof(string),
                        typeof(bool)
                    },
                    null
                );
                type.SetCustomAttribute(new CustomAttributeBuilder(obsoleteCtor, new object[] { obsolete.Message, obsolete.IsError }));
            }

            if (routePrefix != null)
            {
                var prefix = routePrefix.Prefix;
                if (routePrefix.IncludeTypeName) prefix = prefix + "/" + (interfaceType.GetGenericArguments().Any() ? interfaceType.GetGenericArguments().FirstOrDefault()?.Name.ToLower() : interfaceType.GetInterfaces().FirstOrDefault()?.GetGenericArguments().First().Name.ToLower());
                var routePrefixCtor = typeof(RoutePrefixAttribute).GetConstructor(
                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                           null,
                           new Type[]{
                        typeof(string)
                               },
                           null
                           );

                type.SetCustomAttribute(new CustomAttributeBuilder(routePrefixCtor, new object[] { prefix }));
            }

            AddDescriptionAttribute(interfaceType, type);

            return type;
        }

        private static void AddDescriptionAttribute(Type interfaceType, TypeBuilder type)
        {
            var descriptionAttribute = interfaceType.GetCustomAttribute<ServiceDescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                var ctor = typeof(ServiceDescriptionAttribute).GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { typeof(string) }, null);
                var props = new[]
                {
                    typeof(ServiceDescriptionAttribute).GetProperty("Tags"),
                    typeof(ServiceDescriptionAttribute).GetProperty("Summary"),
                    typeof(ServiceDescriptionAttribute).GetProperty("IsDeprecated"),
                    typeof(ServiceDescriptionAttribute).GetProperty("Responses")
                };
                var values = new object[]
                {
                    descriptionAttribute.Tags,
                    descriptionAttribute.Summary,
                    descriptionAttribute.IsDeprecated,
                    descriptionAttribute.Responses
                };
                type.SetCustomAttribute(new CustomAttributeBuilder(ctor, new[] { descriptionAttribute.Description }, props,
                    values));
            }
        }

        private static IRoutePrefix GetRoutePrefix(Type interfaceType)
        {
            IRoutePrefix routePrefix = interfaceType.GetCustomAttribute<ApiAttribute>()
                                       ?? interfaceType.GetInterfaces().FirstOrDefault()?.GetCustomAttribute<ApiAttribute>();
            if (routePrefix == null)
                routePrefix = interfaceType.GetCustomAttribute<IRoutePrefixAttribute>()
                              ?? interfaceType.GetInterfaces().FirstOrDefault()?.GetCustomAttribute<IRoutePrefixAttribute>();
            return routePrefix;
        }

        public void Save()
        {
            if (ConfigurationManager.AppSettings["stardust.saveGeneratedAssemblies"] == "true")
                myAssemblyBuilder.Save("service.dll");
        }

        public Assembly GetCustomAssembly()
        {
            return myModuleBuilder.Assembly;
        }
    }
}