using NihonUnisys.Olyzae.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NihonUnisys.Olyzae.Models;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    [Authorize(Roles = "ParticipantUser")]
    public class ProjectInfoController : Controller
    {
        private Entities db = new Entities();
        private ExecutionContext ctx = ExecutionContext.Create();

        // GET: /Project/

        /// <summary>
        /// ログインしている参加者がこれまでに参加したプロジェクトを一覧表示します。
        /// （Referencesタブ）
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;

            var user = ExecutionContext.Create().CurrentUser as ParticipantUser;
            var model = (from p in db.Projects
                         from pup in p.ParticipantUsers
                         where pup.ParticipantUser.ID == user.ID
                         select p)
                        .ToList();

            return View(model);
        }

        // GET: /ProjectInfo/project/1/Details
        [AllowAnonymous]
        public ActionResult Details(int? projectId)
        {
            if (projectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;

            Project project = db.Projects
                .Where(x => x.ID == projectId)
                .Include(x => x.Company)
                .Include(x => x.ProjectPage)
                .Include(x => x.ProjectPage.PageContents)
                .Include(x => x.Duration)
                .Include(x => x.ParticipantUsers)
                .Include(x => x.ParticipantUsers.Select(pup => pup.ParticipantUser))
                .FirstOrDefault();

            if (project == null)
            {
                return HttpNotFound();
            }

            return View(project);
        }

        // GET: /ProjectInfo/project/1/ShowImage/5
        [AllowAnonymous]
        public ActionResult ShowImage(int? projectId, int? id)
        {
            if (projectId == null || id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;

            var document =
                (from pc in db.PageContents
                 from d in db.Documents
                 where pc.ProjectPage.Project.ID == projectId.Value
                    && pc.ID == id.Value
                    && d.ID == pc.DocumentID
                 select d).FirstOrDefault();

            if (document == null)
            {
                return HttpNotFound();
            }

            return this.File(document);
        }

        // POST: /ProjectInfo/Apply/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Apply(int? projectId)
        {
            if (projectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;

            Project project = db.Projects
                .Where(x => x.ID == projectId)
                .Include(x => x.Duration) // IsAcceptingApplicationの呼び出しに必要
                .Include(x => x.ParticipantUsers) // IsAcceptedの呼び出しに必要
                .Include(x => x.ParticipantUsers.Select(pup => pup.ParticipantUser)) // IsAcceptedの呼び出しに必要
                .FirstOrDefault();

            if (project == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                var user = ExecutionContext.Create().CurrentUser as ParticipantUser;
                db.Users.Attach(user);

                if ((project.IsAcceptingApplication() == true)
                    && (!project.HasReachedTheQuota())
                    && (project.IsAccepted(user) == false))
                {
                    ParticipantUserProject relation = new ParticipantUserProject();
                    relation.ParticipantUser = user;
                    relation.Project = project;
                    relation.Project.ProjectApply = relation.Project.ProjectApply + 1;
                    db.ParticipantUserProjects.Add(relation);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Details", new { id = projectId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
