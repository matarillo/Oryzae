using NihonUnisys.Olyzae.Framework;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers
{
    public static class ControllerExtensions
    {
        public static FileResult File(this Controller self, Models.Document document)
        {
            return File(self, document.BinaryData, MimeMapping.GetMimeMapping(document.FileExtension), document.Uploaded, document.ID);
        }

        public static FileResult File(this Controller self, byte[] data, string contentType, DateTime lastModified, Guid guid)
        {
            var ms = new MemoryStream(data);
            return new FileStreamWithLastModifiedResult(ms, contentType, lastModified, guid);
        }
    }
}