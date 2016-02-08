using NihonUnisys.Olyzae.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Objects;
using System.Data;
using System.Diagnostics;
using NihonUnisys.Olyzae.Framework;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    public class CompanyProjectUserController : AbstractCompanyProjectController
    {
        private ExecutionContext ctx = ExecutionContext.Create();

        //
        // GET: /CompanyProjectUser/project/1/Index

        public ActionResult Index()
        {
            return View(null, null, null, default(int), default(int), null);
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var db = this.DbContext;

            User user = db.Users.Find(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            var projects = db.Projects
                .Where(p => p.ParticipantUsers.Any(pup => pup.ParticipantUser.ID == id))
                .Where(p => p.Company.ID == this.Project.Company.ID)
                .ToList();

            var projectGroup = db.Groups.OfType<ProjectGroup>()
                .Where(pg => pg.Project.Company.ID == this.Project.Company.ID)
                .Where(pg => pg.ParticipantUsers.Any(pug => pug.ParticipantUser.ID == id))
                .ToList();

            ViewBag.Projects = projects;
            ViewBag.ProjectGroup = projectGroup;


            return View(user);
        }

        internal ActionResult View(
            string viewName,
            IList<ParticipantUser> users,
            IList<ProjectGroup> groups,
            int selectedUserId,
            int selectedGroupId,
            string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ModelState.AddModelError("", errorMessage);
            }

            var db = this.DbContext;

            ViewBag.Users = users ?? GetUsers(db, this.Project);
            ViewBag.Groups = groups ?? GetGroups(db, this.Project);
            GetRelationships(db, this.Project); // ユーザーとグループを紐づける

            ViewBag.SelectedUserId = selectedUserId;
            ViewBag.SelectedGroupId = selectedGroupId;

            var emptyGroup = new ProjectGroup();
            ViewBag.CreateGroup = emptyGroup;
            ViewBag.EditGroup = emptyGroup;
            ViewBag.DeleteGroup = emptyGroup;

            return (string.IsNullOrEmpty(viewName))
                ? View(this.Project)
                : View(viewName, this.Project);
        }

        // POST: /CompanyProjectUser/project/1/CreateGroup
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateGroup([Bind(Prefix = "CreateGroup", Include = "GroupName, Accessibility")] ProjectGroup group, string returnUrl)
        {
            return ManageGroupAndRedirect(returnUrl, () =>
            {
                var db = this.DbContext;
                group.Project = this.Project;
                db.Groups.Add(group);
                db.SaveChanges();
            });
        }

        // POST: /CompanyProjectUser/project/1/EditGroup/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditGroup([Bind(Prefix = "EditGroup", Include = "ID,GroupName")] ProjectGroup group, string returnUrl)
        {
            return ManageGroupAndRedirect(returnUrl, () =>
            {
                var db = this.DbContext;
                var targetGroup = FindGroup(db, this.Project, group.ID);
                if (targetGroup != null)
                {
                    targetGroup.GroupName = group.GroupName;
                    db.SaveChanges();
                }
            });
        }

        // POST: /CompanyProjectUser/project/1/DeleteGroup/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteGroup(int? groupIdToDelete, string returnUrl)
        {
            // 仮引数をProjectGroup型ではなく、int?型のgroupIdToDeleteにしているのは、
            // ProjectGroupを受け取った場合、ModelState.isValidがfalseを返す
            // (GroupNameの必須チェックでNGになる)ため。

            return ManageGroupAndRedirect(returnUrl, () =>
            {
                var db = this.DbContext;
                var targetGroup = (groupIdToDelete.HasValue) ? FindGroup(db, this.Project, groupIdToDelete.Value) : null;
                if (targetGroup != null)
                {
                    db.Groups.Remove(targetGroup);
                    db.SaveChanges();
                }
            });
        }

        internal ActionResult ManageGroupAndRedirect(string returnUrl, Action action)
        {
            if (ModelState.IsValid)
            {
                action();
            }

            if ((!string.IsNullOrEmpty(returnUrl))
                && (!string.Equals(returnUrl, "/", System.StringComparison.OrdinalIgnoreCase))
                && (Url.IsLocalUrl(returnUrl)))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        //
        // GET: /CompanyProjectUser/project/1/AssignUser/2
        /// <summary>
        /// 指定したユーザーが参加しているグループを編集するために、
        /// 現在参加しているグループをすべて取得する。
        /// </summary>
        /// <param name="id">ユーザーのID。</param>
        /// <returns></returns>
        public ActionResult AssignUser(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }
            var db = this.DbContext;
            var selectedUser = FindUser(db, this.Project, id.Value);
            if (selectedUser == null)
            {
                return RedirectToAction("Index");
            }
            return View(null, null, null, selectedUser.ID, default(int), null);
        }

        /// <summary>
        /// 指定したユーザーが参加しているグループを編集する。
        /// 入力された情報を元に、グループに参加させたり脱退させたりする。
        /// </summary>
        /// <param name="id">ユーザーのID。</param>
        /// <param name="containsUser">キーがグループIDで、値が参加・不参加を表すブール値である辞書。</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignUser(int? id, Dictionary<int, bool> containsUser)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }
            var db = this.DbContext;
            var selectedUser = FindUser(db, this.Project, id.Value);
            if (selectedUser == null)
            {
                return RedirectToAction("Index");
            }
            var groups = GetGroups(db, this.Project);
            GetRelationships(db, this.Project);
            foreach (var g in groups)
            {
                if (!containsUser.ContainsKey(g.ID))
                {
                    continue;
                }
                // プロジェクトのグループを全て取得しているので、selectedUser -> group はnullにならない。
                var relationship = selectedUser.Groups.FirstOrDefault(pug => pug.Group.ID == g.ID);
                if ((relationship == null) && (containsUser[g.ID]))
                {
                    relationship = new ParticipantUserGroup();
                    relationship.Group = g;
                    relationship.ParticipantUser = selectedUser;
                    db.ParticipantUserGroups.Add(relationship);
                }
                else if ((relationship != null) && (!containsUser[g.ID]))
                {
                    db.ParticipantUserGroups.Remove(relationship);
                }
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // GET: /CompanyProjectUser/project/1/AssignGroup/2
        /// <summary>
        /// 指定したグループに参加しているユーザーを編集するために、
        /// 現在参加しているユーザーをすべて取得する。
        /// </summary>
        /// <param name="id">グループのID。</param>
        /// <returns></returns>
        public ActionResult AssignGroup(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }
            var db = this.DbContext;
            var selectedGroup = FindGroup(db, this.Project, id.Value);
            if (selectedGroup == null)
            {
                return RedirectToAction("Index");
            }
            return View(null, null, null, default(int), selectedGroup.ID, null);
        }

        /// <summary>
        /// 指定したグループに参加しているユーザーを編集する。
        /// 入力された情報を元に、ユーザーを参加させたり脱退させたりする。
        /// </summary>
        /// <param name="id">グループのID。</param>
        /// <param name="belongsToGroup">キーがユーザーIDで、値が参加・不参加を表すブール値である辞書。</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignGroup(int? id, Dictionary<int, bool> belongsToGroup)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }
            var db = this.DbContext;
            var selectedGroup = FindGroup(db, this.Project, id.Value);
            if (selectedGroup == null)
            {
                return RedirectToAction("Index");
            }
            var users = GetUsers(db, this.Project);
            GetRelationships(db, this.Project);
            foreach (var u in users)
            {
                if (!belongsToGroup.ContainsKey(u.ID))
                {
                    continue;
                }
                // プロジェクトの参加ユーザーを全て取得しているので、selectedGroup -> user はnullにならない。
                var relationship = selectedGroup.ParticipantUsers.FirstOrDefault(pug => pug.ParticipantUser.ID == u.ID);
                if ((relationship == null) && (belongsToGroup[u.ID]))
                {
                    relationship = new ParticipantUserGroup();
                    relationship.Group = selectedGroup;
                    relationship.ParticipantUser = u;
                    db.ParticipantUserGroups.Add(relationship);
                }
                else if ((relationship != null) && (!belongsToGroup[u.ID]))
                {
                    db.ParticipantUserGroups.Remove(relationship);
                }
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        internal static IList<ParticipantUser> GetUsers(Entities db, Project project)
        {
            // projectを元に、関連するParticipantUserを取得する。
            // クエリを単純にするため、参加しているグループの情報は取得しない。
            // グループの情報が必要な場合は、GetGroups()とGetRelationships()を呼び出すこと。
            // そうすれば、ParticipantUser -> ParticipantUserGroup の関連と、
            // ParticipantUserGroup -> Group の関連は、Entity Frameworkが自動的に設定してくれる。
            var usersQuery = from pup in db.ParticipantUserProjects
                             where pup.Project.ID == project.ID
                             select pup.ParticipantUser;
            LogUtility.DebugWriteQuery(usersQuery);
            var users = usersQuery.ToList();
            return users;
        }

        internal static IList<ProjectGroup> GetGroups(Entities db, Project project)
        {
            // projectを元に、関連するProjectGroupsを取得する。
            // クエリを単純にするため、ユーザーの情報は取得しない。
            // ユーザーの情報が必要な場合は、GetUsers()とGetRelationships()を呼び出すこと。
            // そうすれば、ParticipantUser -> ParticipantUserGroup の関連と、
            // ParticipantUserGroup -> Group の関連は、Entity Frameworkが自動的に設定してくれる。
            var groupsQuery = from pg in db.Groups.OfType<ProjectGroup>()
                              where pg.Project.ID == project.ID
                              select pg;
            LogUtility.DebugWriteQuery(groupsQuery);
            var groups = groupsQuery.ToList();
            return groups;
        }

        internal static IList<ParticipantUserGroup> GetRelationships(Entities db, Project project)
        {
            // projectを元に、ParticipantUserとProjectGroupの関連を取得する。
            var relationshipQuery = from pg in db.Groups.OfType<ProjectGroup>()
                                    where pg.Project.ID == project.ID
                                    from pug in pg.ParticipantUsers
                                    select pug;
            LogUtility.DebugWriteQuery(relationshipQuery);
            var relationships = relationshipQuery.ToList();
            return relationships;
        }

        internal static ParticipantUser FindUser(Entities db, Project project, int id)
        {
            var targetUser = db.Entry(project)
                .Collection(p => p.ParticipantUsers)
                .Query()
                .Include(u => u.ParticipantUser.Groups)
                .Select(x => x.ParticipantUser)
                .FirstOrDefault(u => u.ID == id);
            return targetUser;
        }

        internal static ProjectGroup FindGroup(Entities db, Project project, int id)
        {
            var targetGroupQuery = db.Groups
                .OfType<ProjectGroup>()
                .Where(pg => pg.ID == id && pg.Project.ID == project.ID)
                .Include(pg => pg.ParticipantUsers);
            LogUtility.DebugWriteQuery(targetGroupQuery);
            return targetGroupQuery.FirstOrDefault();
        }
    }
}