using NihonUnisys.Olyzae.Models;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace NihonUnisys.Olyzae.Controllers.Participants
{
    [Authorize(Roles = "ParticipantUser")]
    public class QuestionnaireController : AbstractParticipantProjectController
    {
        // GET: /Questionnaire/project/5
        public ActionResult Index()
        {
            DateTime now = this.ExecutionContext.Now;

            // カレントユーザに割り当てられているアンケート（回答）を取得
            var answer = this.DbContext.Answers
                .Where(ans => ans.Questionnaire.Questioned < now
                    && ans.ParticipantUserProject.Project.ID == this.Project.ID
                    && ans.ParticipantUserProject.ParticipantUser.ID == this.CurrentUser.ID)
                .Include(ans => ans.Questionnaire)
                .ToList();

            return View(answer);
        }

        // GET: /Questionnaire/project/1/Answer?questionnaireId=5
        public ActionResult Answer(int? questionnaireId)
        {
            // アンケート回答を取得する
            var answer = this.DbContext.Answers
                .Where(ans => ans.Questionnaire.ID == questionnaireId
                    && ans.ParticipantUserProject.ParticipantUser.ID == this.CurrentUser.ID
                    && ans.ParticipantUserProject.Project.ID == this.Project.ID)
                .Include(ans => ans.Questionnaire)
                .FirstOrDefault();

            // 空の回答が存在しなければ、ログインユーザにはアンケートが割り当てられていない
            if (answer == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 回答済みの場合は BadRequest（TODO: 課題一覧画面に戻すか、エラー画面を出す）
            if (answer.Answered.HasValue)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 質問は割り当てられたが、回答可能日時を過ぎていない場合は BadRequest
            // TODO:サーバ時間は考慮済みか？
            var questionnaire = answer.Questionnaire;
            if (questionnaire.Questioned.HasValue && questionnaire.Questioned > this.ExecutionContext.Now)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // モデルを作成し、回答画面を表示
            AnswerQuestionnaireViewModel model = new AnswerQuestionnaireViewModel()
            {
                Questionnaire = answer.Questionnaire,
                Answer = answer
            };

            return View(model);
        }

        // POST: /Questionnaire/project/1/Answer?questionnaireId=5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Answer([Bind(Include = "Answer, Answer.ID, Answer.BodyJSON")] AnswerQuestionnaireViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Project = this.Project;
                return View(model);
            }

            model.Answer.Answered = this.ExecutionContext.Now;

            this.DbContext.Entry(model.Answer).State = EntityState.Modified;
            this.DbContext.SaveChanges();

            // プロジェクトの課題一覧に戻る（TODO:確認画面などの作成）
            return RedirectToAction("Index", "Questionnaire", new { projectId = this.Project.ID });
        }
    }
}