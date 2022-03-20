﻿using System;
using System.Collections;

namespace RRQMCore.Dependency
{
    /// <summary>
    /// 注入容器接口
    /// </summary>
    public interface IContainer
    {
        /// <summary>
        /// 注册临时映射
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface;

        /// <summary>
        /// 注册单例
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="singleton"></param>
        public void RegisterSingleton<TInterface, TImplementation>(TImplementation singleton) where TImplementation : TInterface;

        /// <summary>
        /// 注册单例
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface;


        /// <summary>
        /// 创建类型对应的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>();

        /// <summary>
        /// 创建类型对应的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Resolve(Type type);
    }
    /// <summary>
    /// IOC容器
    /// </summary>
    public class Container : IContainer
    {
        private readonly Hashtable registrations;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Container()
        {
            this.registrations = new Hashtable();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
        {
            if (this.registrations.ContainsKey(typeof(TInterface)))
            {
                this.registrations[typeof(TInterface)] = typeof(TImplementation);
            }
            else
            {
                this.registrations.Add(typeof(TInterface), typeof(TImplementation));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="singleton"></param>
        public void RegisterSingleton<TInterface, TImplementation>(TImplementation singleton) where TImplementation : TInterface
        {
            if (this.registrations.ContainsKey(typeof(TInterface)))
            {
                this.registrations[typeof(TInterface)] = singleton;
            }
            else
            {
                this.registrations.Add(typeof(TInterface), singleton);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
        {
            if (this.registrations.ContainsKey(typeof(TInterface)))
            {
                this.registrations[typeof(TInterface)] = this.Resolve<TImplementation>();
            }
            else
            {
                this.registrations.Add(typeof(TInterface), this.Resolve<TImplementation>());
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private object Create(Type interfaceType)
        {
            object value = this.registrations[interfaceType];

            if (value == null)
            {
                if (interfaceType.IsPrimitive || interfaceType == typeof(string))
                {
                    return default;
                }
                else
                {
                    var constructors = interfaceType.GetConstructors();
                    if (constructors.Length > 0)
                    {
                        var parameters = constructors[0].GetParameters();
                        object[] ps = new object[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].ParameterType.IsPrimitive || parameters[i].ParameterType == typeof(string))
                            {
                                if (parameters[i].HasDefaultValue)
                                {
                                    ps[i] = parameters[i].DefaultValue;
                                }
                                else
                                {
                                    ps[i] = default;
                                }
                            }
                            else
                            {
                                ps[i] = this.Create(parameters[i].ParameterType);
                            }
                        }

                        return Activator.CreateInstance(interfaceType, ps);
                    }
                    else
                    {
                        throw new RRQMException($"没有找到类型{interfaceType.Name}的公共构造函数。");
                    }
                }
            }
            else if (value is Type type)
            {
                var constructors = type.GetConstructors();
                if (constructors.Length > 0)
                {
                    var parameters = constructors[0].GetParameters();
                    object[] ps = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType.IsPrimitive || parameters[i].ParameterType == typeof(string))
                        {
                            if (parameters[i].HasDefaultValue)
                            {
                                ps[i] = parameters[i].DefaultValue;
                            }
                            else
                            {
                                ps[i] = default;
                            }
                        }
                        else
                        {
                            ps[i] = this.Create(parameters[i].ParameterType);
                        }
                    }

                    return Activator.CreateInstance(type, ps);
                }
                else
                {
                    throw new RRQMException($"没有找到类型{interfaceType.Name}的公共构造函数。");
                }
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            return (T)this.Create(typeof(T));
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Resolve(Type type)
        {
            return this.Create(type);
        }
    }
}
