using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    /// <summary>
    /// 参加者のプロジェクトメニューから「課題」を選択して遷移する画面のコントローラー。
    /// </summary>
    /// <remarks>
    /// 基底クラスの処理によって、プロジェクト情報がプロパティに格納されます。
    /// また、プロジェクト情報はViewBag.Projectにも格納されます。
    /// projectIdはアクションメソッドの引数には不要ですが、ルーティング情報としては必要です。
    /// </remarks>
    [Authorize(Roles = "ParticipantUser")]
    public class ThemeController : AbstractParticipantProjectController
    {
        public ActionResult Index()
        {
            // プロジェクトの課題のうち、開始日時を過ぎたものを取得する。
            var themesQuery = this.DbContext.Themes
                .Where(t => t.Project.ID == this.Project.ID)
                .Include(t => t.Duration)
                .Where(t => t.Duration.From == null || t.Duration.From <= this.ExecutionContext.Now);
            LogUtility.DebugWriteQuery(themesQuery);

            var themes = themesQuery.ToList();
            return View(themes);
        }

        public ActionResult Details(int? themeId)
        {
            if (themeId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定されたプロジェクトの課題が、開始日時を過ぎていれば取得する。
            // グループの提出状況も取得する。
            var themeQuery = this.DbContext.Themes
                .Where(t => t.ID == themeId && t.Project.ID == this.Project.ID)
                .Include(t => t.Duration)
                .Where(t => t.Duration.From == null || t.Duration.From <= this.ExecutionContext.Now)
                .Include(t => t.GroupWork);
            LogUtility.DebugWriteQuery(themeQuery);

            var theme = themeQuery.FirstOrDefault();
            if (theme == null)
            {
                return HttpNotFound();
            }

            // 提出物のアップロード日時を取得する。(バイナリは取得しない）
            var attachments = theme.GroupWork
                .Where(gw => gw.AttachedDocumentID.HasValue)
                .Select(gw => gw.AttachedDocumentID.Value)
                .ToArray();
            var documentsQuery = this.DbContext.Documents
                .Where(d => attachments.Contains(d.ID))
                .Select(d => new{ ID = d.ID, Uploaded = d.Uploaded });
            LogUtility.DebugWriteQuery(documentsQuery);
            var documents = documentsQuery.ToDictionary(d => d.ID, d => d.Uploaded);
            ViewBag.Documents = documents;

            // プロジェクトに紐づくグループをすべて取得する。
            var groupsQuery = this.DbContext.Groups
                .OfType<ProjectGroup>()
                .Where(pg => pg.Project.ID == this.Project.ID);
            LogUtility.DebugWriteQuery(groupsQuery);
            var groups = groupsQuery.ToList();
            groups.Sort((x, y) => x.ID - y.ID);
            ViewBag.Groups = groups;

            // 自分が参加しているグループを取得する。
            var relationsQuery =
                from pg in this.DbContext.Groups.OfType<ProjectGroup>()
                where pg.Project.ID == this.Project.ID
                from pug in pg.ParticipantUsers
                where pug.ParticipantUser.ID == this.CurrentUser.ID
                select pug;
            LogUtility.DebugWriteQuery(relationsQuery);
            var relations = relationsQuery.ToList();

            return View(theme);
        }

        public ActionResult GroupWork(int? themeId, int? groupId)
        {
            if (themeId == null || groupId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var theme = GetTheme(themeId.Value);
            if (theme == null)
            {
                return HttpNotFound();
            }

            var group = GetGroup(groupId.Value);
            if (group == null)
            {
                return RedirectToAction("Details", new { projectId = this.Project.ID, themeId = themeId });
            }

            var tuple = GetGroupWorkAndUploadedDate(themeId.Value, groupId.Value);
            var groupWork = tuple.Item1;
            var uploadedDate = tuple.Item2;

            ViewBag.Theme = theme;
            ViewBag.Group = group;
            ViewBag.GroupWork = groupWork;
            ViewBag.Uploaded = uploadedDate;
            return View(groupWork);
        }

        /// <summary>
        /// 指定されたプロジェクトの課題が、開始日時を過ぎていれば取得する。
        /// </summary>
        /// <param name="themeId">課題ID。</param>
        /// <returns></returns>
        internal Theme GetTheme(int themeId)
        {
            var themeQuery = this.DbContext.Themes
                .Where(t => t.ID == themeId && t.Project.ID == this.Project.ID)
                .Include(t => t.Duration)
                .Where(t => t.Duration.From == null || t.Duration.From <= this.ExecutionContext.Now);
            LogUtility.DebugWriteQuery(themeQuery);
            var theme = themeQuery.FirstOrDefault();
            return theme;
        }

        /// <summary>
        /// 指定されたプロジェクトグループに、自分が参加していれば取得する。
        /// </summary>
        /// <param name="groupId">グループID。</param>
        /// <returns></returns>
        internal ProjectGroup GetGroup(int groupId)
        {
            var groupQuery =
                from pg in this.DbContext.Groups.OfType<ProjectGroup>()
                where pg.ID == groupId && pg.Project.ID == this.Project.ID
                from pug in this.DbContext.ParticipantUserGroups
                where pug.Group.ID == pg.ID && pug.ParticipantUser.ID == this.CurrentUser.ID
                select pg;
            LogUtility.DebugWriteQuery(groupQuery);
            var group = groupQuery.FirstOrDefault();
            return group;
        }

        /// <summary>
        /// 指定されたプロジェクトの課題に対する、指定したグループの提出状況を取得する。
        /// また、もしあれば、提出物のアップロード日時も取得する。（左外部結合。バイナリは取得しない。）
        /// </summary>
        /// <param name="themeId"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        internal Tuple<GroupWork, DateTime?> GetGroupWorkAndUploadedDate(int themeId, int groupId)
        {
            var groupWorkQuery =
                from gw in this.DbContext.GroupWorks
                where gw.Theme.ID == themeId && gw.ProjectGroup.ID == groupId
                join d in this.DbContext.Documents on gw.AttachedDocumentID equals d.ID into documents
                select new { GroupWork = gw, Uploaded = documents.Select(d => (DateTime?)d.Uploaded).FirstOrDefault() };
            LogUtility.DebugWriteQuery(groupWorkQuery);
            var result = groupWorkQuery.FirstOrDefault();
            var groupWork = (result != null) ? result.GroupWork : null;
            var uploaded = (result != null) ? result.Uploaded : null;
            return Tuple.Create(groupWork, uploaded);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GroupWork(int? themeId, int? groupId, bool? submitted, HttpPostedFileBase uploadedFile)
        {
            if (themeId == null || groupId == null || submitted == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var theme = GetTheme(themeId.Value);
            if (theme == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var group = GetGroup(groupId.Value);
            if (group == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tuple = GetGroupWorkAndUploadedDate(themeId.Value, groupId.Value);
            var groupWork = tuple.Item1;

            // 評価済みの場合は更新不可
            if (groupWork != null && groupWork.Status == GroupWorkStatus.Evaluated)
            {
                return RedirectToAction("Details", new { themeId = themeId });
            }

            if (groupWork == null)
            {
                groupWork = new GroupWork { Theme = theme, ProjectGroup = group, EvaluationJSON = "{}" };
                this.DbContext.GroupWorks.Add(groupWork);
            }
                
            AttachFile(groupWork, uploadedFile);
            groupWork.Status = (submitted == true) ? GroupWorkStatus.Submitted : GroupWorkStatus.Default;
            this.DbContext.SaveChanges();

            return RedirectToAction("Details", new { themeId = themeId });
        }

        internal void AttachFile(GroupWork groupwork, HttpPostedFileBase uploadedFile)
        {
            if (groupwork == null || uploadedFile == null)
            {
                return;
            }

            // TODO: 見直し。プロトでは特に検証せずDBに保存する。
            var fileName = Path.GetFileName(uploadedFile.FileName);
            var extension = Path.GetExtension(fileName);
            var binaryData = GetBytes(uploadedFile.InputStream);

            if (binaryData.Length > 0)
            {
                var document = new Document
                {
                    ID = Guid.NewGuid(),
                    Uploaded = this.ExecutionContext.Now,
                    User = this.CurrentUser,
                    FileExtension = extension,
                    BinaryData = binaryData,
                };
                this.DbContext.Documents.Add(document);
                groupwork.AttachedFileName = fileName;
                groupwork.AttachedDocumentID = document.ID;
                groupwork.Status = GroupWorkStatus.Submitted;
            }
        }

        private static byte[] GetBytes(Stream stream)
        {
            if (stream == null)
            {
                return new byte[0];
            }
            var ms = stream as MemoryStream;
            if (ms == null)
            {
                ms = new MemoryStream();
                stream.CopyTo(ms);
            }
            return ms.ToArray();
        }


        public ActionResult Download(int? themeId, int? groupId)
        {
            if (themeId == null || groupId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定された課題およびグループが自分が参加しているプロジェクトに属していれば
            // 別のグループの課題であってもダウンロード可能。
            var db = this.DbContext;
            var groupWorkQuery = this.DbContext.GroupWorks
                .Where(gw => gw.Theme.Project.ID == this.Project.ID
                    && gw.ProjectGroup.Project.ID == this.Project.ID
                    && gw.AttachedDocumentID.HasValue);
            LogUtility.DebugWriteQuery(groupWorkQuery);
            var groupWork = groupWorkQuery.FirstOrDefault();
            if (groupWork == null)
            {
                // 指定された課題およびグループが正しくないか、またはファイルがアップロードされていない
                return RedirectToAction("Details", new { themeId = themeId });
            }

            var document = db.Documents.FirstOrDefault(d => d.ID == groupWork.AttachedDocumentID);
            if (document == null)
            {
                // not found
                return RedirectToAction("Details", new { themeId = themeId });
            }

            return File(document.BinaryData, MediaTypeNames.Application.Octet, groupWork.AttachedFileName);
        }
    }
}