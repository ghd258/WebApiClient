﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using WebApiClientCore.Serialization;
using WebApiClientCore.Serialization.JsonConverters;

namespace WebApiClientCore
{
    /// <summary>
    /// 表示HttpApi选项
    /// </summary>
    public class HttpApiOptions
    {
        /// <summary>
        /// 获取或设置Http服务完整主机域名
        /// 例如 http://www.abc.com/ 或 http://www.abc.com/path/
        /// 设置了HttpHost值，HttpHostAttribute将失效
        /// </summary>
        public Uri? HttpHost { get; set; }

        /// <summary>
        /// 获取或设置是否使用的日志功能
        /// </summary>
        public bool UseLogging { get; set; } = true;

        /// <summary>
        /// 获取或设置请求头是否包含默认的UserAgent
        /// </summary>
        public bool UseDefaultUserAgent { get; set; } = true;

        /// <summary>
        /// 获取或设置是否对参数的属性值进行输入有效性验证
        /// </summary>
        public bool UseParameterPropertyValidate { get; set; } = true;

        /// <summary>
        /// 获取或设置是否对返回值的属性值进行输入有效性验证
        /// </summary>
        public bool UseReturnValuePropertyValidate { get; set; } = true;



        /// <summary>
        /// 获取 json 序列化选项
        /// </summary>
        public JsonSerializerOptions JsonSerializeOptions { get; } = CreateJsonSerializeOptions();

        /// <summary>
        /// 获取 json 反序列化选项
        /// </summary>
        public JsonSerializerOptions JsonDeserializeOptions { get; } = CreateJsonDeserializeOptions();

        /// <summary>
        /// xml序列化选项
        /// </summary>
        public XmlWriterSettings XmlSerializeOptions { get; } = new XmlWriterSettings();

        /// <summary>
        /// xml反序列化选项
        /// </summary>
        public XmlReaderSettings XmlDeserializeOptions { get; } = new XmlReaderSettings();

        /// <summary>
        /// 获取 keyValue 序列化选项
        /// </summary>
        public KeyValueSerializerOptions KeyValueSerializeOptions { get; } = new KeyValueSerializerOptions();

        /// <summary>
        /// 获取自定义数据存储的字典
        /// </summary>
        public Dictionary<object, object> Properties { get; } = [];

        /// <summary>
        /// 获取接口的全局过滤器集合
        /// </summary>
        public IList<IApiFilter> GlobalFilters { get; } = new List<IApiFilter>();


        /// <summary>
        /// 创建序列化JsonSerializerOptions
        /// </summary> 
        private static JsonSerializerOptions CreateJsonSerializeOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        /// <summary>
        /// 创建反序列化JsonSerializerOptions
        /// </summary>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "JsonCompatibleConverter.EnumReader使用前已经判断RuntimeFeature.IsDynamicCodeSupported")]
        private static JsonSerializerOptions CreateJsonDeserializeOptions()
        {
            var options = CreateJsonSerializeOptions();
            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                options.Converters.Add(JsonCompatibleConverter.EnumReader);
            }
            options.Converters.Add(JsonCompatibleConverter.DateTimeReader);
            return options;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// 插入指定的<see cref="System.Text.Json.Serialization.JsonSerializerContext"/>到所有序列化选项的TypeInfoResolverChain的最前位置
        /// </summary>
        /// <param name="context"></param>
        public void PrependJsonSerializerContext(System.Text.Json.Serialization.JsonSerializerContext context)
        {
            this.JsonSerializeOptions.TypeInfoResolverChain.Insert(0, context);
            this.JsonDeserializeOptions.TypeInfoResolverChain.Insert(0, context);
            this.KeyValueSerializeOptions.GetJsonSerializerOptions().TypeInfoResolverChain.Insert(0, context);
        }
#endif
    }
}