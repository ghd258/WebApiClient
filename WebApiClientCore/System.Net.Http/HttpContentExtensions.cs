﻿using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebApiClientCore.Exceptions;
using WebApiClientCore.Internals;

namespace System.Net.Http
{
    /// <summary>
    /// HttpContent扩展
    /// </summary>
    public static class HttpContentExtensions
    {
        private const string IsBufferedPropertyName = "IsBuffered";
        private const string IsBufferedGetMethodName = "get_IsBuffered";

        /// <summary>
        /// IsBuffered字段
        /// </summary>
        private static readonly Func<HttpContent, bool>? isBufferedFunc;

        /// <summary>
        /// 静态构造器
        /// </summary>
        static HttpContentExtensions()
        {
            var property = typeof(HttpContent).GetProperty(IsBufferedPropertyName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (property != null)
            {
#if NET8_0_OR_GREATER
                if (property.GetGetMethod(nonPublic: true)?.Name == IsBufferedGetMethodName)
                {
                    isBufferedFunc = GetIsBuffered;
                }
#endif
                if (isBufferedFunc == null)
                {
                    isBufferedFunc = LambdaUtil.CreateGetFunc<HttpContent, bool>(property);
                }
            }
        }

#if NET8_0_OR_GREATER
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = IsBufferedGetMethodName)]
        private static extern bool GetIsBuffered(HttpContent httpContent);
#endif

        /// <summary>
        /// 获取是否已缓存数据 
        /// </summary>
        /// <param name="httpContent"></param>
        /// <returns></returns>
        public static bool? IsBuffered(this HttpContent httpContent)
        {
            return isBufferedFunc == null ? null : isBufferedFunc(httpContent);
        }

        /// <summary>
        /// 确保HttpContent的内容未被缓存
        /// 已被缓存则抛出HttpContentBufferedException
        /// </summary>
        /// <param name="httpContent"></param>
        /// <exception cref="HttpContentBufferedException"></exception>
        public static void EnsureNotBuffered(this HttpContent httpContent)
        {
            if (httpContent.IsBuffered() == true)
            {
                throw new HttpContentBufferedException();
            }
        }


        /// <summary>
        /// 读取 json 内容为指定的类型
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="content">http内容</param>
        /// <param name="options">json反序列化选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        public static async Task<T?> ReadAsJsonAsync<T>(this HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            var srcEncoding = content.GetEncoding();
            if (Encoding.UTF8.Equals(srcEncoding))
            {
                using var utf8Json = await content.ReadAsStreamCoreAsync(cancellationToken).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<T>(utf8Json, options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var byteArray = await content.ReadAsByteArrayCoreAsync(cancellationToken).ConfigureAwait(false);
                var utf8Json = Encoding.Convert(srcEncoding, Encoding.UTF8, byteArray);
                return JsonSerializer.Deserialize<T>(utf8Json, options);
            }
        }

        /// <summary>
        /// 读取 json 内容为指定的类型
        /// </summary>
        /// <param name="content">http内容</param>
        /// <param name="objType">目标类型</param>
        /// <param name="options">json反序列化选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        public static async Task<object?> ReadAsJsonAsync(this HttpContent content, Type objType, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            var srcEncoding = content.GetEncoding();
            if (Encoding.UTF8.Equals(srcEncoding))
            {
                using var utf8Json = await content.ReadAsStreamCoreAsync(cancellationToken).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync(utf8Json, objType, options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var byteArray = await content.ReadAsByteArrayCoreAsync(cancellationToken).ConfigureAwait(false);
                var utf8Json = Encoding.Convert(srcEncoding, Encoding.UTF8, byteArray);
                return JsonSerializer.Deserialize(utf8Json, objType, options);
            }
        }

        /// <summary>
        /// 读取为二进制数组并转换为 utf8 编码
        /// </summary>
        /// <param name="httpContent"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static Task<byte[]> ReadAsUtf8ByteArrayAsync(this HttpContent httpContent)
        {
            return httpContent.ReadAsByteArrayAsync(Encoding.UTF8, default);
        }

        /// <summary>
        /// 读取为二进制数组并转换为指定的编码
        /// </summary>
        /// <param name="httpContent"></param>
        /// <param name="dstEncoding">目标编码</param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static Task<byte[]> ReadAsByteArrayAsync(this HttpContent httpContent, Encoding dstEncoding)
        {
            return httpContent.ReadAsByteArrayAsync(dstEncoding, default);
        }

        /// <summary>
        /// 读取为二进制数组并转换为指定的编码
        /// </summary>
        /// <param name="httpContent"></param>
        /// <param name="dstEncoding">目标编码</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static async Task<byte[]> ReadAsByteArrayAsync(this HttpContent httpContent, Encoding dstEncoding, CancellationToken cancellationToken)
        {
            var encoding = httpContent.GetEncoding();
            var byteArray = await httpContent.ReadAsByteArrayCoreAsync(cancellationToken).ConfigureAwait(false);
            return encoding.Equals(dstEncoding) ? byteArray : Encoding.Convert(encoding, dstEncoding, byteArray);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task<byte[]> ReadAsByteArrayCoreAsync(this HttpContent httpContent, CancellationToken cancellationToken)
        {
#if NET5_0_OR_GREATER
            return httpContent.ReadAsByteArrayAsync(cancellationToken);
#else
            return httpContent.ReadAsByteArrayAsync();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task<Stream> ReadAsStreamCoreAsync(this HttpContent httpContent, CancellationToken cancellationToken)
        {
#if NET5_0_OR_GREATER
            return httpContent.ReadAsStreamAsync(cancellationToken);
#else
            return httpContent.ReadAsStreamAsync();
#endif
        }


        /// <summary>
        /// 获取编码信息
        /// </summary>
        /// <param name="httpContent"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(this HttpContent httpContent)
        {
            var contentType = httpContent.Headers.ContentType;
            if (contentType == null)
            {
                return Encoding.UTF8;
            }

            var charSet = contentType.CharSet.AsSpan();
            if (charSet.IsEmpty)
            {
                return Encoding.UTF8;
            }

            var encoding = charSet.Trim('"');
            return encoding.Equals(Encoding.UTF8.WebName, StringComparison.OrdinalIgnoreCase)
                ? Encoding.UTF8
                : Encoding.GetEncoding(encoding.ToString());
        }
    }
}
