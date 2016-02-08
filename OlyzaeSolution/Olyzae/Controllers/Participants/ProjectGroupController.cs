using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NihonUnisys.Olyzae.Models;
using System.Diagnostics;
using System.Data.Objects;
using NihonUnisys.Olyzae.Framework;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    /// <summary>
    /// 参加者のプロジェクトメニューから「グループ」を選択して遷移する画面のコントローラー。
    /// </summary>
    [Authorize(Roles = "ParticipantUser")]
    public class ProjectGroupController : AbstractParticipantProjectController
    {
        // GET: /ProjectGroup/project/1
        public ActionResult Index(int? projectId)
        {
            // ユーザーとプロジェクトの取得は親クラスで実行ずみ。

            // 現在のプロジェクトのグループのうち、AccessibilityがPublicのものをすべて取得する。
            // 自分がグループに参加中かどうかを判定するために、関連オブジェクトも合わせて取得する。
            // プロジェクトのグループを全て取得するので、currentUser -> group はnullにならない。
            var projectGroups = this.DbContext
                .Groups
                .OfType<ProjectGroup>()
                .Where(pg => pg.Project.ID == projectId && pg.Accessibility == ProjectGroupAccessibility.Public)
                .Include(pg => pg.ParticipantUsers);
            LogUtility.DebugWriteQuery(projectGroups);

            if (projectGroups == null)
            {
                return HttpNotFound();
            }

            ViewBag.CurrentUser = this.CurrentUser;
            ViewBag.Project = this.Project;

            return View(projectGroups.ToList());
        }

        // GET: /ProjectGroup/project/1/Details/5
        public ActionResult Details(int? projectId, int? groupId)
        {
            // ユーザーとプロジェクトの取得は親クラスで実行ずみ。

            if (groupId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定したグループが現在のプロジェクトのグループであること、かつ、AccessibilityがPublicであることを確認し、
            // そのグループに参加しているユーザーも含めて取得する。
            var projectGroups = this.DbContext
                .Groups
                .OfType<ProjectGroup>()
                .Where(pg => pg.ID == groupId && pg.Project.ID == projectId && pg.Accessibility == ProjectGroupAccessibility.Public)
                .Include(pg => pg.ParticipantUsers)
                .Include(pg => pg.ParticipantUsers.Select(pug => pug.ParticipantUser));
            LogUtility.DebugWriteQuery(projectGroups);

            var projectGroup = projectGroups.FirstOrDefault();
            if (projectGroup == null)
            {
                return HttpNotFound();
            }

            ViewBag.CurrentUser = this.CurrentUser;
            return View(projectGroup);
        }

        // GET: /ProjectGroup/Create
        public ActionResult Create(int? projectId)
        {
            // ユーザーとプロジェクトの取得は親クラスで実行ずみ。

            ViewBag.ProjectName = this.Project.Name;
            ViewBag.ProjectId = this.Project.ID;

            return View();
        }

        // POST: /ProjectGroup/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int? projectId, [Bind(Include = "GroupName, Accessibility")]ProjectGroup projectGroup)
        {
            // ユーザーとプロジェクトの取得は親クラスで実行ずみ。

            if (!ModelState.IsValid)
            {
                return View(projectGroup);
            }

            projectGroup.Project = this.Project;

            ParticipantUserGroup pug = new ParticipantUserGroup();
            pug.Group = projectGroup;
            pug.ParticipantUser = this.CurrentUser;

            this.DbContext.Groups.Add(projectGroup);
            this.DbContext.ParticipantUserGroups.Add(pug);
            this.DbContext.Users.Attach(pug.ParticipantUser);
            this.DbContext.SaveChanges();
            return RedirectToAction("Index", new { projectId = projectId });
        }

        public ActionResult Leave(int? projectId, int? groupId)
        {
            // ユーザーとプロジェクトの取得は親クラスで実行ずみ。

            if (groupId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定したグループが現在のプロジェクトのグループであり、
            // かつ、公開範囲が「公開」であり、
            // かつ、現在のユーザーがそのグループに参加しているかどうかを確認する。
            var relashonship = this.DbContext
                .Entry(this.Project)
                .Collection(p => p.ProjectGroups)
                .Query()
                .Where(g => g.ID == groupId && g.Accessibility == ProjectGroupAccessibility.Public)
                .SelectMany(g => g.ParticipantUsers)
                .FirstOrDefault(pug => pug.ParticipantUser.ID == this.CurrentUser.ID);

            // 参加している場合のみグループを抜ける
            if (relashonship != null)
            {
                this.DbContext.ParticipantUserGroups.Remove(relashonship);
                this.DbContext.SaveChanges();
            }

            return RedirectToAction("Index", new { projectId = projectId });
        }

        public ActionResult Enter(int? projectId, int? groupId)
        {
            // ユーザーとプロジェクトの取得は親クラスで実行ずみ。

            if (groupId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 指定したグループが現在のプロジェクトのグループであり、かつ、公開範囲が「公開」であることを確認する。
            var group = this.DbContext
                .Entry(this.Project)
                .Collection(p => p.ProjectGroups)
                .Query()
                .Where(g => g.ID == groupId && g.Accessibility == ProjectGroupAccessibility.Public)
                .Include(g => g.ParticipantUsers)
                .Include(g => g.ParticipantUsers.Select(pug => pug.ParticipantUser))
                .FirstOrDefault();

            if (group == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var relashonship = this.DbContext
                .Entry(this.Project)
                .Collection(p => p.ProjectGroups)
                .Query()
                .Where(g => g.ID == groupId)
                .SelectMany(g => g.ParticipantUsers)
                .FirstOrDefault(pug => pug.ParticipantUser.ID == this.CurrentUser.ID);

            // 現在のユーザーがそのグループに参加していない場合のみグループに参加する
            if (group.ParticipantUsers.All(pug => pug.ParticipantUser.ID != this.CurrentUser.ID))
            {
                ParticipantUserGroup pug = new ParticipantUserGroup();
                pug.Group = group;
                pug.ParticipantUser = this.CurrentUser;
                this.DbContext.ParticipantUserGroups.Add(pug);
                this.DbContext.SaveChanges();
            }

            return RedirectToAction("Index", new { projectId = projectId });
        }
    }
}
