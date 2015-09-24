﻿// -----------------------------------------------------------------------
//  <copyright file="WebApiFunctionHandler.cs" company="OSharp开源团队">
//      Copyright (c) 2014-2015 OSharp. All rights reserved.
//  </copyright>
//  <site>http://www.osharp.org</site>
//  <last-editor>郭明锋</last-editor>
//  <last-date>2015-09-21 20:52</last-date>
// -----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;

using OSharp.Core.Reflection;
using OSharp.Core.Security;
using OSharp.SiteBase.Properties;
using OSharp.SiteBase.Security;
using OSharp.Utility;
using OSharp.Utility.Extensions;
using OSharp.Web.Mvc.Security;


namespace OSharp.SiteBase.Http
{
    /// <summary>
    /// WebApi功能信息处理器
    /// </summary>
    public class WebApiFunctionHandler : FunctionHandlerBase<Function, Guid>
    {
        /// <summary>
        /// 获取或设置 控制器类型查找器
        /// </summary>
        protected override ITypeFinder TypeFinder
        {
            get { return new WebApiControllerTypeFinder(); }
        }

        /// <summary>
        /// 获取或设置 功能查找器
        /// </summary>
        protected override IMethodInfoFinder MethodInfoFinder
        {
            get { return new WebApiActionMethodInfoFinder(); }
        }

        /// <summary>
        /// 重写以实现从类型信息创建功能信息
        /// </summary>
        /// <param name="type">类型信息</param>
        /// <returns></returns>
        protected override Function GetFunction(Type type)
        {
            if (!typeof(ApiController).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(Resources.WebApiActionMethodInfoFinder_TypeNotApiControllerType.FormatWith(type.FullName));
            }
            Function function = new Function()
            {
                Name = type.ToDescription(),
                Area = GetArea(type),
                Controller = type.Name.Replace("ApiController", string.Empty),
                IsController = true,
                FunctionType = FunctionType.Anonymouse
            };
            return function;
        }

        /// <summary>
        /// 重写以实现从方法信息创建功能信息
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        protected override Function GetFunction(MethodInfo method)
        {
            if (method.ReturnType != typeof(IHttpActionResult) && method.ReturnType != typeof(Task<IHttpActionResult>))
            {
                throw new InvalidOperationException(Resources.MvcFunctionHandler_MethodNotMvcAction.FormatWith(method.Name));
            }

            FunctionType functionType = FunctionType.Anonymouse;
            if (method.HasAttribute<AllowAnonymousAttribute>(true))
            {
                functionType = FunctionType.Anonymouse;
            }
            else if (method.HasAttribute<LoginedAttribute>(true))
            {
                functionType = FunctionType.Logined;
            }
            else if (method.HasAttribute<RoleLimitAttribute>(true))
            {
                functionType = FunctionType.RoleLimit;
            }
            Type type = method.DeclaringType;
            if (type == null)
            {
                throw new InvalidOperationException("声明功能“{0}”的类型为空".FormatWith(method.Name));
            }
            Function function = new Function()
            {
                Name = method.ToDescription(),
                Area = GetArea(type),
                Controller = type.Name.Replace("Controller", string.Empty),
                Action = method.Name,
                FunctionType = functionType,
                IsController = false,
                IsAjax = method.HasAttribute<AjaxOnlyAttribute>(),
                IsChild = false
            };
            return function;
        }

        /// <summary>
        /// 重写以实现从类型中获取功能的区域信息
        /// </summary>
        protected override string GetArea(Type type)
        {
            type.Required<Type, InvalidOperationException>(m => typeof(ApiController).IsAssignableFrom(m) && !m.IsAbstract,
                Resources.WebApiActionMethodInfoFinder_TypeNotApiControllerType.FormatWith(type.FullName));
            string @namespace = type.Namespace;
            if (@namespace == null)
            {
                return null;
            }
            int index = @namespace.IndexOf("Areas", StringComparison.Ordinal) + 6;
            string area = index > 6 ? @namespace.Substring(index, @namespace.IndexOf(".Controllers", StringComparison.Ordinal) - index) : null;
            return area;
        }

        /// <summary>
        /// 重写以实现是否忽略指定方法的功能信息
        /// </summary>
        /// <param name="method">方法信息</param>
        /// <returns></returns>
        protected override bool IsIgnoreMethod(MethodInfo method)
        {
            return method.HasAttribute<HttpPostAttribute>();
        }
    }
}