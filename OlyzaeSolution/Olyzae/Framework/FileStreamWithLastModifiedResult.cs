using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Framework
{
    public class FileStreamWithLastModifiedResult : FileStreamResult
    {
        internal DateTime lastModified;
        internal Guid guid;
        private const int _bufferSize = 0x1000;

        public FileStreamWithLastModifiedResult(MemoryStream memoryStream, string contentType, DateTime lastModified, Guid guid)
            : base(memoryStream, contentType)
        {
            this.lastModified = lastModified;
            this.guid = guid;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            bool notModified = IsNotModified(context.HttpContext.Request);
            if (notModified)
            {
                var statusCode = new HttpStatusCodeResult(304, "Not Modified");
                statusCode.ExecuteResult(context);
            }
            else
            {
                context.HttpContext.Response.Cache.SetLastModified(lastModified);
                // HttpCachePolicyは、CacheabilityがPrivate(デフォルト値)に設定されていると、
                // SetETag()でETagヘッダを追加しません。（既知の不具合）
                context.HttpContext.Response.AddHeader("ETag", guid.ToString());
                base.ExecuteResult(context);
            }
        }

        public bool IsNotModified(HttpRequestBase request)
        {
            const bool modified = false;
            const bool notModified = true;

            // check If-None-Match header
            var previousETag = request.Headers["If-None-Match"];
            if (previousETag != null)
            {
                return (previousETag == guid.ToString()) ? notModified : modified;
            }

            // check If-Modified-Since header
            var modifiedSinceGMT = request.Headers["If-Modified-Since"];
            if (modifiedSinceGMT != null)
            {
                var modifiedSince = DateTime.Parse(modifiedSinceGMT).ToLocalTime();
                if (modifiedSince >= lastModified)
                {
                    return notModified;
                }
            }

            return modified;
        }
    }
}