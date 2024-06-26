﻿using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WebApiClientCore.HttpContents;
using WebApiClientCore.Internals;

namespace WebApiClientCore.Attributes
{
    /// <summary>
    /// 表示 json 内容的结果特性
    /// </summary>
    public class JsonReturnAttribute : ApiReturnAttribute
    {
        /// <summary>
        /// text/json
        /// </summary>
        private static readonly string textJson = "text/json";

        /// <summary>
        /// 问题描述 json
        /// </summary>
        private static readonly string problemJson = "application/problem+json";

        /// <summary>
        /// json内容的结果特性
        /// </summary>
        public JsonReturnAttribute()
            : base(new MediaTypeWithQualityHeaderValue(JsonContent.MediaType))
        {
        }

        /// <summary>
        /// json内容的结果特性
        /// </summary>
        /// <param name="acceptQuality">accept的质比</param>
        public JsonReturnAttribute(double acceptQuality)
            : base(new MediaTypeWithQualityHeaderValue(JsonContent.MediaType, acceptQuality))
        {
        }

        /// <summary>
        /// 指示响应的ContentType与AcceptContentType是否匹配
        /// 返回 false 则调用下一个ApiReturnAttribute来处理响应结果
        /// </summary>
        /// <param name="responseContentType">响应的ContentType</param>
        /// <returns></returns>
        protected override bool IsMatchAcceptContentType(MediaTypeHeaderValue responseContentType)
        {
            return base.IsMatchAcceptContentType(responseContentType)
                || MediaTypeUtil.IsMatch(textJson, responseContentType.MediaType)
                || MediaTypeUtil.IsMatch(problemJson, responseContentType.MediaType);
        }

        /// <summary>
        /// 设置强类型模型结果值
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns></returns> 
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("Trimming", "IL3050:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public override async Task SetResultAsync(ApiResponseContext context)
        {
            var resultType = context.ActionDescriptor.Return.DataType.Type;
            context.Result = await context.JsonDeserializeAsync(resultType).ConfigureAwait(false);
        }
    }
}
