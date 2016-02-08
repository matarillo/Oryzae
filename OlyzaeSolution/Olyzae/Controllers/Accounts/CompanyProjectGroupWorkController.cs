using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    public class CompanyProjectGroupWorkController : AbstractCompanyProjectController
    {
        // GET: /CompanyProjectGroupWork/
        public ActionResult Index(int? selectedThemeId)
        {
            ViewBag.Project = this.Project;
            ViewBag.ThemeList = CreateThemeDropDownList(selectedThemeId);

            if (selectedThemeId.HasValue)
            {
                // 選択されたテーマに紐づく評価を取得
                // プロジェクトに紐づくテーマのみを対象にする。
                // テーマが取得できなかった場合はデフォルトのビューを返す
                var db = this.DbContext;
                var themeQuery = db.Themes
                    .Where(t => t.ID == selectedThemeId.Value && t.Project.ID == this.Project.ID)
                    .Include(t => t.GroupWork)
                    .Include(t => t.GroupWork.Select(gw => gw.ProjectGroup));
                LogUtility.DebugWriteQuery(themeQuery);
                var theme = themeQuery.FirstOrDefault();
                if (theme == null)
                {
                    return View();
                }
                ViewBag.Theme = theme;
                ViewBag.SelectedThemeId = selectedThemeId.Value;

                var groupWorks = theme.GroupWork;

                Dictionary<int, string> projectGroupNames = new Dictionary<int, string>();

                foreach (var item in groupWorks)
                {
                    projectGroupNames[item.ProjectGroup.ID] = item.ProjectGroup.GroupName;
                }

                ViewBag.ProjectGroupNames = projectGroupNames;

                return View(groupWorks.ToList());
            }

            return View();
        }

        private List<SelectListItem> CreateThemeDropDownList(int? themeId)
        {
            var db = this.DbContext;

            List<SelectListItem> themeList = new List<SelectListItem>();

            List<Theme> themes = db.Themes.Where(theme => theme.Project.ID == this.Project.ID).ToList();

            foreach (Theme theme in themes)
            {
                bool selected = themeId.HasValue && themeId.Value == theme.ID;
                themeList.Add(new SelectListItem { Text = theme.Name, Value = theme.ID.ToString(), Selected = selected });
            }

            return themeList;
        }

        // GET: /CompanyProjectGroupWork/Details/5
        public ActionResult Details(int? themeId, int? projectGroupId)
        {
            if (themeId == null || projectGroupId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var db = this.DbContext;
            var groupworkQuery = db.GroupWorks
                .Where(gw => gw.ThemeID == themeId && gw.ProjectGroupID == projectGroupId && gw.Theme.Project.ID == this.Project.ID)
                .Include(gw => gw.Theme)
                .Include(gw => gw.ProjectGroup);
            LogUtility.DebugWriteQuery(groupworkQuery);

            GroupWork groupwork = groupworkQuery.FirstOrDefault();
            if (groupwork == null)
            {
                return HttpNotFound();
            }

            ViewBag.Project = this.Project;
            ViewBag.Theme = groupwork.Theme;

            return View(groupwork);
        }

        // GET: /CompanyProjectGroupWork/Create
        public ActionResult Create(int? themeId)
        {
            if (themeId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var db = this.DbContext;
            var theme = db.Themes
                .Where(t => t.ID == themeId && t.Project.ID == this.Project.ID)
                .FirstOrDefault();

            if (theme == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.Project = this.Project;

            // プロジェクトに紐づくグループを取得
            ViewBag.ProjectGroups = CreateProjectGroupList(null);
            return View(new GroupWork { Theme = theme });
        }

        private List<SelectListItem> CreateProjectGroupList(int? projectGroupId)
        {
            var db = this.DbContext;
            db.Projects.Attach(this.Project);
            var query = db.Entry(this.Project).Collection(p => p.ProjectGroups).Query().Where(x => x.Accessibility == ProjectGroupAccessibility.Public);
            query.Load();

            List<SelectListItem> list = new List<SelectListItem>();

            foreach (var projectGroup in query)
            {
                bool selected = projectGroupId.HasValue && projectGroupId.Value == projectGroup.ID;
                list.Add(new SelectListItem { Text = projectGroup.GroupName, Value = projectGroup.ID.ToString(), Selected = selected });
            }

            return list;
        }

        // POST: /CompanyProjectGroupWork/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int? themeId, int? projectGroupId, GroupWorkStatus status, string evaluationJSON)
        {
            if (!themeId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!projectGroupId.HasValue)
            {
                ModelState.AddModelError("ProjectGroupId", "グループを選択してください。");
            }

            if (string.IsNullOrEmpty(evaluationJSON))
            {
                ModelState.AddModelError("DocumentID", "評価を入力してください。");
            }

            var db = this.DbContext;
            var themeQuery = db.Themes
                .Where(t => t.ID == themeId.Value && t.Project.ID == this.Project.ID);
            LogUtility.DebugWriteQuery(themeQuery);

            var theme = themeQuery.FirstOrDefault();
            if (theme == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            GroupWork groupwork = new GroupWork
            {
                EvaluationJSON = evaluationJSON,
                Theme = theme,
                Status = status
            };

            groupwork.ProjectGroup =
                db.Groups.OfType<ProjectGroup>().FirstOrDefault(pg => pg.ID == projectGroupId.Value && pg.Accessibility == ProjectGroupAccessibility.Public);

            if (groupwork.ProjectGroup.Accessibility != ProjectGroupAccessibility.Public)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var groupWorkForCheck = db.GroupWorks.FirstOrDefault(gw => gw.ThemeID == themeId && gw.ProjectGroupID == projectGroupId);

            if (groupWorkForCheck != null)
            {
                ModelState.AddModelError("ProjectGroupId", "選択したグループの評価がすでに存在します。");
            }

            ViewBag.Project = this.Project;
            ViewBag.ProjectGroups = CreateProjectGroupList(projectGroupId);

            if (!ModelState.IsValid)
            {
                return View(groupwork);
            }

            db.Projects.Attach(this.Project);
            db.GroupWorks.Add(groupwork);
            db.Themes.Attach(groupwork.Theme);
            db.Groups.Attach(groupwork.ProjectGroup);

            db.SaveChanges();

            return RedirectToAction("Index", new { selectedThemeId = themeId.Value });
        }

        // GET: /CompanyProjectGroupWork/Edit/5
        public ActionResult Edit(int? themeId, int? projectGroupId)
        {
            if (themeId == null || projectGroupId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var db = this.DbContext;
            var groupworkQuery = db.GroupWorks
                .Include(gw => gw.Theme)
                .Include(gw => gw.ProjectGroup)
                .Where(gw =>
                    gw.ThemeID == themeId
                    && gw.ProjectGroupID == projectGroupId
                    && gw.Theme.Project.ID == this.Project.ID
                    && gw.ProjectGroup.Accessibility == ProjectGroupAccessibility.Public);
            LogUtility.DebugWriteQuery(groupworkQuery);

            GroupWork groupwork = groupworkQuery.FirstOrDefault();

            if (groupwork == null)
            {
                return HttpNotFound();
            }

            ViewBag.Project = this.Project;
            return View(groupwork);
        }

        // POST: /CompanyProjectGroupWork/Edit/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ThemeID,ProjectGroupID,EvaluationJSON,Status")] GroupWork groupwork)
        {
            if (!ModelState.IsValid)
            {
                return View(groupwork);
            }

            var db = this.DbContext;
            var groupworkQuery = db.GroupWorks
                .Where(gw => gw.ThemeID == groupwork.ThemeID
                    && gw.ProjectGroupID == groupwork.ProjectGroupID
                    && gw.Theme.Project.ID == this.Project.ID
                    && gw.ProjectGroup.Accessibility == ProjectGroupAccessibility.Public);
            LogUtility.DebugWriteQuery(groupworkQuery);

            GroupWork targetGroupwork = groupworkQuery.FirstOrDefault();
            if (targetGroupwork != null)
            {
                targetGroupwork.EvaluationJSON = groupwork.EvaluationJSON;
                targetGroupwork.Status = groupwork.Status;
                db.SaveChanges();
            }

            return RedirectToAction("Index", new { selectedThemeId = groupwork.ThemeID });
        }

        // GET: /CompanyProjectGroupWork/Delete/5
        public ActionResult Delete(int? themeId, int? projectGroupId)
        {
            if (themeId == null || projectGroupId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var db = this.DbContext;
            var groupworkQuery = db.GroupWorks
                .Include(gw => gw.ProjectGroup)
                .Include(gw => gw.Theme)
                .Where(gw =>
                    gw.ThemeID == themeId
                    && gw.ProjectGroupID == projectGroupId
                    && gw.Theme.Project.ID == this.Project.ID
                    && gw.ProjectGroup.Accessibility == ProjectGroupAccessibility.Public);
            LogUtility.DebugWriteQuery(groupworkQuery);

            GroupWork groupwork = groupworkQuery.FirstOrDefault();
            if (groupwork == null)
            {
                return HttpNotFound();
            }

            ViewBag.ProjectGroupName = groupwork.ProjectGroup.GroupName;
            ViewBag.ThemeId = groupwork.Theme.ID;

            return View(groupwork);
        }

        // POST: /CompanyProjectGroupWork/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int? themeId, int? projectGroupId)
        {
            ViewBag.Project = this.Project;

            var db = this.DbContext;
            var groupworkQuery = db.GroupWorks
                .Include(gw => gw.ProjectGroup)
                .Where(gw =>
                    gw.ThemeID == themeId
                    && gw.ProjectGroupID == projectGroupId
                    && gw.Theme.Project.ID == this.Project.ID
                    && gw.ProjectGroup.Accessibility == ProjectGroupAccessibility.Public);
            LogUtility.DebugWriteQuery(groupworkQuery);

            GroupWork groupwork = groupworkQuery.FirstOrDefault();
            if (groupwork != null)
            {
                db.GroupWorks.Remove(groupwork);
                db.SaveChanges();
            }

            return RedirectToAction("Index", new { selectedThemeId = themeId });
        }
    }
}
