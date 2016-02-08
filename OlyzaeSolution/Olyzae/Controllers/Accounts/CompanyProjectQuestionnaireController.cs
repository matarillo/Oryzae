using NihonUnisys.Olyzae.Models;
using System.Data;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Accounts
{
    public class CompanyProjectQuestionnaireController : AbstractCompanyProjectController
    {
        // GET: /CompanyProjectQuestionnaire/
        public ActionResult Index()
        {
            ViewBag.Project = this.Project;
            return View(this.DbContext.Questionnaires.Where(questionnaire => questionnaire.Project.ID == this.Project.ID).ToList());
        }

        // GET: /CompanyProjectQuestionnaire/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Questionnaire questionnaire = this.DbContext.Questionnaires.Find(id);
            if (questionnaire == null)
            {
                return HttpNotFound();
            }
            ViewBag.Project = this.Project;
            return View(questionnaire);
        }

        // GET: /CompanyProjectQuestionnaire/Create
        public ActionResult Create()
        {
            ViewBag.Project = this.Project;
            return View();
        }

        // POST: /CompanyProjectQuestionnaire/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include="ID,Name,Questioned,BodyJSON")] Questionnaire questionnaire)
        {
            if (ModelState.IsValid)
            {
                // プロジェクトの参加者全員を取得する
                this.DbContext.Projects.Attach(this.Project);
                this.DbContext.Entry(this.Project).Collection(p => p.ParticipantUsers).Load();

                // アンケートオブジェクトを追加
                questionnaire.Project = this.Project;
                this.DbContext.Questionnaires.Add(questionnaire);

                // 参加者に紐づく回答オブジェクトを追加
                foreach (var pup in this.Project.ParticipantUsers)
                {
                    var answer = new Answer { Questionnaire = questionnaire, ParticipantUserProject = pup };
                    this.DbContext.Answers.Add(answer);
                }

                this.DbContext.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Project = this.Project;
            
            return View(questionnaire);
        }

        // GET: /CompanyProjectQuestionnaire/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Questionnaire questionnaire = this.DbContext.Questionnaires.Find(id);
            if (questionnaire == null)
            {
                return HttpNotFound();
            }
            ViewBag.Project = this.Project;
            return View(questionnaire);
        }

        // POST: /CompanyProjectQuestionnaire/Edit/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include="ID,Name,Questioned,BodyJSON")] Questionnaire questionnaire)
        {
            if (ModelState.IsValid)
            {
                this.DbContext.Entry(questionnaire).State = EntityState.Modified;
                this.DbContext.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Project = this.Project;
            return View(questionnaire);
        }

        // GET: /CompanyProjectQuestionnaire/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Questionnaire questionnaire = this.DbContext.Questionnaires.Find(id);
            if (questionnaire == null)
            {
                return HttpNotFound();
            }
            ViewBag.Project = this.Project;
            return View(questionnaire);
        }

        // POST: /CompanyProjectQuestionnaire/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Questionnaire questionnaire = this.DbContext.Questionnaires.Find(id);
            this.DbContext.Questionnaires.Remove(questionnaire);
            this.DbContext.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.DbContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
