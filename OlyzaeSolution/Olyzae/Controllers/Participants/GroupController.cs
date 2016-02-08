using NihonUnisys.Olyzae.Framework;
using NihonUnisys.Olyzae.Models;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    public class GroupController : AbstractParticipantGroupController
    {
        // GET: /Group/
        public ActionResult Index()
        {
            // 自分が参加しているグル―プの情報をすべて取得する。
            // ProjectGroupについては、関連するProjectも取得する。

            var groups = this.DbContext.Groups
                .Where(g => g.ParticipantUsers.Any(pug => pug.ParticipantUser.ID == this.CurrentUser.ID))
                .ToList();
            var projects =
                (from pug in this.DbContext.ParticipantUserGroups
                 where pug.ParticipantUser.ID == this.CurrentUser.ID
                 from pg in this.DbContext.Groups.OfType<ProjectGroup>()
                 where pg.ID == pug.Group.ID
                 from p in this.DbContext.Projects
                 where p.ID == pg.Project.ID
                 select p).ToList();

            return View(groups);
        }

        // GET: /Group/Details?groupId=5
        public ActionResult Details(int? groupId)
        {
            // 基底クラスの共通処理により、Groupプロパティには必ず値が入っている。
            var group = this.Group;
            Debug.Assert(group != null, "this.Group is null");

            if (!IsAccessibleProject(group))
            {
                // 参加者がアクセスできない公開範囲の場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var projectGroup = group as ProjectGroup;

            if (projectGroup != null)
            {
                ViewBag.ProjectName = projectGroup.Project.Name;
            }

            var query = this.DbContext.ParticipantUserGroups
                .Where(pug => pug.Group.ID == group.ID)
                .Include(pug => pug.ParticipantUser);

            List<string> projectMembers = query
                .Select(pug => pug.ParticipantUser.DisplayName)
                .ToList();

            ViewBag.ProjectMembers = projectMembers;

            return View(group);
        }

        public ActionResult Members(int? groupId)
        {
            // 基底クラスの共通処理により、Groupプロパティには必ず値が入っている。
            var group = this.Group;
            Debug.Assert(group != null, "this.Group is null");

            if (!IsAccessibleProject(group))
            {
                // 参加者がアクセスできない公開範囲の場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var query = this.DbContext.ParticipantUserGroups
                .Where(pug => pug.Group.ID == group.ID)
                .Include(pug => pug.ParticipantUser);
            LogUtility.DebugWriteQuery(query);
            var relationships = query.ToList();
            var members = group.ParticipantUsers
                .Select(pug => pug.ParticipantUser)
                .ToList();

            return View(members);
        }


        // GET: /Group/Leave?groupId=1
        /// <summary>
        /// 退会の確認画面を表示します。
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public ActionResult Leave(int? groupId)
        {
            // 基底クラスの共通処理により、Groupプロパティには必ず値が入っている。
            var group = this.Group;

            if (!IsAccessibleProject(group))
            {
                // 参加者がアクセスできない公開範囲の場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Debug.Assert(group != null, "this.Group is null");

            return View(group);
        }

        // POST: /Group/Leave?groupId=1
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        /// <summary>
        /// 退会処理を実行します。
        /// TODO プライベートグループを作成した本人が退会できるのか？
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("Leave")]
        [ValidateAntiForgeryToken]
        public ActionResult LeaveConfirmed(int? groupId)
        {
            // 基底クラスの共通処理により、Groupプロパティには必ず値が入っている。
            var group = this.Group;
            Debug.Assert(group != null, "this.Group is null");

            if (!IsAccessibleProject(group))
            {
                // 参加者がアクセスできない公開範囲の場合はエラー
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var data = this.DbContext.ParticipantUserGroups
                .FirstOrDefault(pug => pug.ParticipantUser.ID == this.CurrentUser.ID && pug.Group.ID == groupId);
            this.DbContext.ParticipantUserGroups.Remove(data);
            this.DbContext.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: /Group/Create
        public ActionResult Create()
        {
            var groups = this.DbContext.Groups.ToList();
            SetGroups(groups);
            return View(groups);
        }

        private bool IsAccessibleProject(Group group)
        {
            ProjectGroup projectGroup = group as ProjectGroup;

            if (projectGroup == null)
            {
                return true;
            }

            if (projectGroup.Accessibility == ProjectGroupAccessibility.Public)
            {
                return true;
            }

            return false;
        }

        private void SetGroups(List<Group> groups)
        {
            var relationships = this.DbContext.ParticipantUserGroups
                .Where(pug => pug.ParticipantUser.ID == this.CurrentUser.ID)
                .ToList();
            List<int> joinGroupIds = relationships
                .Select(pug => pug.Group.ID)
                .ToList();
            ViewBag.JoinGroupIds = joinGroupIds;
        }

        // POST: /Group/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "GroupId,GroupName")] Group group)
        {
            var groups = this.DbContext.Groups.ToList();
            SetGroups(groups);
            if (!ModelState.IsValid)
            {
                return View(groups);
            }
            this.DbContext.Groups.Add(group);


            ParticipantUserGroup participantUserGroup = new ParticipantUserGroup();
            participantUserGroup.ParticipantUser = this.DbContext.Users.OfType<ParticipantUser>().First(u => u.ID == this.CurrentUser.ID);
            group.ParticipantUsers.Add(participantUserGroup);

            this.DbContext.SaveChanges();
            return RedirectToAction("Create");
        }
    }
}
