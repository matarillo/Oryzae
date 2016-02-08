using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{
    public class AnswerQuestionnaireViewModel
    {
        public Questionnaire Questionnaire { get; set; }

        public Answer Answer { get; set; }
    }
}