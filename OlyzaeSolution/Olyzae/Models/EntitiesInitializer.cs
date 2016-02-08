using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;

namespace NihonUnisys.Olyzae.Models
{
    // DropCreateDatabaseAlways<Entities> or DropCreateDatabaseIfModelChanges<Entities>
    public class EntitiesInitializer : DropCreateDatabaseIfModelChanges<Entities>
    {
        protected override void Seed(Entities context)
        {
            // 初期データ投入コード
            var guids = new List<Guid>
            {
                Guid.Parse("B05FE195-3827-4CD4-8A88-05F6D03E9982"),
                Guid.Parse("1E330743-48F2-497F-9F1D-9D1F7C131D52"),
                Guid.Parse("FAE87C0B-DF68-430B-94A8-D94FE2E90F72"),
                Guid.Parse("67E8101E-24E3-4AB9-9639-55C4A734C5E6"),
                Guid.Parse("DF02BD9B-AF5F-4FB1-94E4-E49D05FE00A0"),
                Guid.Parse("F2466741-4382-4DD6-8E2B-757B685335AC"),
                Guid.Parse("5153FA3B-08ED-43B0-9174-DA51DCBDE712"),
                Guid.Parse("E7530719-F106-47D6-A242-500C8F3CE8EA"),
                Guid.Parse("EF6E91D0-1472-412F-AF6F-63AD05F643D6"),
                Guid.Parse("C81D81D6-950C-49F3-90E6-F0B91D5012C8"),
                Guid.Parse("A9DB1DEC-8A15-4AD4-9556-D20A97C88F47"),
                Guid.Parse("00AAF082-BC6E-4819-A376-406E2F25CA31"),
            };
            
            var companies = new List<Company>
            {
                new Company { /*ID=1,*/ CompanyName="青空商事" },
            };
            foreach (var company in companies) context.Companies.Add(company);

            var users = new List<User>
            {
                new AccountUser { /*ID=1,*/ UserName="account1", Password=User.GenerateHashedPassword("P@ssw0rd"), UserType=1, DisplayName="青空太郎", Organization="人事部", Company=companies[0] },
                new ParticipantUser { /*ID=2,*/ UserName="user1", Password=User.GenerateHashedPassword("P@ssw0rd"), UserType=3, DisplayName="学生花子",OnlineName="hanako",Kana="ガクセイハナコ", Gender=2,BirthDay=new DateTime(1998, 1, 1),AcademicRecord="AAAA学園高校",Departments="-",AcademicYear=2,EMailAddress="hanako@mail.com",PhoneNumber="09012345678",Zip="1350061",State=13,City="江東区",StreetAddress1="豊洲",StreetAddress2="ビル",ReturningHomeZip=null,ReturningHomeState=null,ReturningHomeCity=null,ReturningHomeStreetAddress1=null,ReturningHomeStreetAddress2=null,Academic=34,Happpiness="家族",CareerAnchors=2,Mentor="ダーウィン" ,Answer="3", ProfileImageDocumentID=guids[10]  },
                new ParticipantUser { /*ID=3,*/ UserName="user2", Password=User.GenerateHashedPassword("P@ssw0rd"), UserType=3, DisplayName="大学一生",OnlineName="isse",Kana="ダイガクイッセイ", Gender=1,BirthDay=new DateTime(1996, 2, 2),AcademicRecord="ZZZZ大学",Departments="工学部",AcademicYear=6,EMailAddress="isse@mail.com",PhoneNumber="09012345678",Zip="1350061",State=13,City="江東区",StreetAddress1="豊洲",StreetAddress2="ビル",ReturningHomeZip=null,ReturningHomeState=null,ReturningHomeCity=null,ReturningHomeStreetAddress1=null,ReturningHomeStreetAddress2=null,Academic=45,Happpiness="家族",CareerAnchors=4,Mentor="ダーウィン" ,Answer="5", ProfileImageDocumentID=guids[11] },
                new ParticipantUser { /*ID=4,*/ UserName="user3", Password=User.GenerateHashedPassword("P@ssw0rd"), UserType=3, DisplayName="出木杉英才" , OnlineName="hidetoshi",Kana="デキスギヒデトシ", Gender=1,BirthDay=new DateTime(1994, 3, 3),AcademicRecord="XXXX大学",Departments="理学部",AcademicYear=6,EMailAddress="hideo@mail.com",PhoneNumber="09012345678",Zip="1350061",State=13,City="江東区",StreetAddress1="豊洲",StreetAddress2="ビル",ReturningHomeZip=null,ReturningHomeState=null,ReturningHomeCity=null,ReturningHomeStreetAddress1=null,ReturningHomeStreetAddress2=null,Academic=32,Happpiness="家族",CareerAnchors=2,Mentor="ダーウィン" ,Answer="1,2", ProfileImageDocumentID=null },
                new ParticipantUser { /*ID=5,*/ UserName="user4", Password=User.GenerateHashedPassword("P@ssw0rd"), UserType=3, DisplayName="野比のび太" , OnlineName="nobita",Kana="ノビノビタ", Gender=1,BirthDay=new DateTime(1995, 4, 4),AcademicRecord="XXXX大学",Departments="理学部",AcademicYear=7,EMailAddress="nobita@mail.com",PhoneNumber="09012345678",Zip="1350061",State=13,City="江東区",StreetAddress1="豊洲",StreetAddress2="ビル",ReturningHomeZip=null,ReturningHomeState=null,ReturningHomeCity=null,ReturningHomeStreetAddress1=null,ReturningHomeStreetAddress2=null,Academic=28,Happpiness="家族",CareerAnchors=8,Mentor="ダーウィン" ,Answer="6", ProfileImageDocumentID=null },
                new MentorUser { /*ID=6,*/ UserName="mentor1", Password=User.GenerateHashedPassword("P@ssw0rd"), UserType=2, DisplayName="ドラえもん" },
                new MentorUser { /*ID=7,*/ UserName="mentor2", Password=User.GenerateHashedPassword("P@ssw0rd"), UserType=2, DisplayName="みのめんた" },
                new User { /*ID=8,*/ UserName="admin1", Password=User.GenerateHashedPassword("P@ssw0rd"), UserType=0, DisplayName="システム管理者" },
            };
            foreach (var user in users) context.Users.Add(user);

            var durations = new List<Duration>
            {
                new Duration { /*ID=1,*/ From=new DateTime(2013, 4, 1), To=new DateTime(2015, 4, 30) },
                new Duration { /*ID=2,*/ From=new DateTime(2013, 5, 1), To=new DateTime(2015, 5, 31) },
                new Duration { /*ID=3,*/ From=new DateTime(2014, 1, 1), To=new DateTime(2015, 1, 31) },
            };
            foreach (var duration in durations) context.Durations.Add(duration);

            var projects = new List<Project>
            {
                new Project { /*ID=1,*/ Name="プロジェクト1", Description="あああ", Icon=guids[0], ProjectDate=new DateTime(2013, 3, 30), Category=ProjectCategory.Career, Status=ProjectStatus.Accepting, Company=companies[0], Duration=durations[0] },
                new Project { /*ID=2,*/ Name="プロジェクト2", Description="いいい", Icon=guids[1], ProjectDate=new DateTime(2013, 4, 22), Category=ProjectCategory.Career, Status=ProjectStatus.Accepting, Company=companies[0], Duration=durations[1] },
                new Project { /*ID=3,*/ Name="プロジェクト3", Description="ううう", Icon=guids[2], ProjectDate=new DateTime(2013, 12, 25), Category=ProjectCategory.Internship, Status=ProjectStatus.Accepting, Company=companies[0], Duration=durations[2] },
            };
            foreach (var project in projects) context.Projects.Add(project);

            var participantUserProjects = new List<ParticipantUserProject>
            {
                new ParticipantUserProject { /*ID=1,*/ Project=projects[0], ParticipantUser=users[1] as ParticipantUser },
                new ParticipantUserProject { /*ID=2,*/ Project=projects[0], ParticipantUser=users[2] as ParticipantUser },
                new ParticipantUserProject { /*ID=3,*/ Project=projects[0], ParticipantUser=users[3] as ParticipantUser },
            };
            foreach (var participantUserProject in participantUserProjects) context.ParticipantUserProjects.Add(participantUserProject);

            var questionnaires = new List<Questionnaire>
            {
                new Questionnaire { /*ID=1,*/ Name="アンケート1", Questioned=new DateTime(2014, 6, 1), BodyJSON="{\"title\":\"\",\"header\":\"ヘッダ\",\"footer\":\"フッタ\",\"items\":[{\"text\":\"セクション１\",\"items\":[{\"type\":\"text\",\"multiline\":false,\"text\":\"自由回答（ショート）の質問です\"},{\"type\":\"text\",\"multiline\":true,\"text\":\"自由回答（ロング）の質問です\"},{\"type\":\"choices\",\"multiselection\":false,\"withTextBox\":false,\"text\":\"選択回答（自由回答欄なし）の質問です。\",\"choices\":[\"選択肢1\",\"選択肢2\",\"選択肢3\"],\"other\":\"\"},{\"type\":\"choices\",\"multiselection\":false,\"withTextBox\":true,\"text\":\"選択解答（自由回答欄あり）の質問です。\",\"choices\":[\"選択肢1\",\"選択肢2\",\"選択肢3\"],\"other\":\"\"},{\"type\":\"choicesGroup\",\"multiselection\":false,\"text\":\"選択解答（複数質問）の質問です。\",\"questions\":[\"質問1\",\"質問2\"],\"choices\":[\"選択肢1\",\"選択肢2\",\"選択肢3\"]},{\"type\":\"choices\",\"multiselection\":true,\"withTextBox\":false,\"text\":\"複数選択回答（自由回答欄なし）の質問です。\",\"choices\":[\"選択肢1\",\"選択肢2\",\"選択肢3\"],\"other\":\"\"},{\"type\":\"choices\",\"multiselection\":true,\"withTextBox\":true,\"text\":\"複数選択回答（自由回答欄あり）の質問です。\",\"choices\":[\"選択肢1\",\"選択肢2\",\"選択肢3\"],\"other\":\"\"},{\"type\":\"choicesGroup\",\"multiselection\":true,\"text\":\"複数選択回答（複数質問）の質問です。\",\"questions\":[\"質問1\",\"質問2\"],\"choices\":[\"選択肢1\",\"選択肢2\",\"選択肢3\"]}]},{\"text\":\"セクション2\",\"items\":[{\"type\":\"text\",\"multiline\":false,\"text\":\"複数セクションのテストは上手く行っていますか？\"}]},{\"type\":\"text\",\"multiline\":false,\"text\":\"セクションに属さない質問は上手く表示できますか？\"}]}", Project=projects[0] },
                new Questionnaire { /*ID=2,*/ Name="アンケート2", Questioned=new DateTime(2014, 6, 1), BodyJSON="{\"footer\":\"フッター\",\"header\":\"ヘッダー\",\"items\":[{\"__type\":\"Section:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"セクション1\",\"items\":[{\"__type\":\"TextBox:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"テキストボックス1\",\"answer\":null},{\"__type\":\"TextArea:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"テキストエリア2\",\"answer\":null}]},{\"__type\":\"Section:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"セクション2\",\"items\":[{\"__type\":\"RadioButtons:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"ラジオボタン1\",\"choices\":[\"選択肢1\",\"選択肢2\"],\"columns\":2,\"answer\":null},{\"__type\":\"RadioButtonsWithTextBox:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"ラジオボタン2\",\"choices\":[\"選択肢1\",\"選択肢2\",\"その他\"],\"columns\":3,\"answer\":null,\"other\":null},{\"__type\":\"CheckBoxes:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"チェックボックス1\",\"choices\":[\"選択肢1\",\"選択肢2\"],\"columns\":2,\"answer\":null},{\"__type\":\"CheckBoxesWithTextBox:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"チェックボックス2\",\"choices\":[\"選択肢1\",\"選択肢2\",\"その他\"],\"columns\":3,\"answer\":null,\"other\":null}]},{\"__type\":\"Section:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"セクション3\",\"items\":[{\"__type\":\"RadioButtonsGroup:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"ラジオボタングループ1\",\"choices\":[\"選択肢1\",\"選択肢2\"],\"columns\":2,\"answers\":null,\"questions\":[\"質問1\",\"質問2\"],\"rows\":2},{\"__type\":\"CheckBoxesGroup:#NihonUnisys.Olyzae.Models.Questionnaires\",\"required\":null,\"text\":\"チェックボックスグループ2\",\"choices\":[\"選択肢1\",\"選択肢2\"],\"columns\":2,\"answers\":null,\"questions\":[\"質問1\",\"質問2\"],\"rows\":2}]}],\"title\":\"テストアンケート\"}", Project=projects[0] },
            };
            foreach (var questionnaire in questionnaires) context.Questionnaires.Add(questionnaire);

            var answers = new List<Answer>
            {
                new Answer {/*ID=1,*/ Answered=null, BodyJSON=null, Questionnaire=questionnaires[0], ParticipantUserProject=participantUserProjects[0]},
            };
            foreach (var answer in answers) context.Answers.Add(answer);

            var ctx = HttpContext.Current;
            if (ctx != null)
            {
                var documents = new List<Document>
                {
                    new Document { ID=guids[0], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon01.png")) },
                    new Document { ID=guids[1], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon02.png")) },
                    new Document { ID=guids[2], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon03.png")) },
                    new Document { ID=guids[3], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon04.png")) },
                    new Document { ID=guids[4], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon05.png")) },
                    new Document { ID=guids[5], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon06.png")) },
                    new Document { ID=guids[6], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon07.png")) },
                    new Document { ID=guids[7], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon08.png")) },
                    new Document { ID=guids[8], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon09.png")) },
                    new Document { ID=guids[9], User=users[0], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/icon10.png")) },
                    new Document { ID=guids[10], User=users[1], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/profileImage2_medium_5columns.png")) },
                    new Document { ID=guids[11], User=users[2], Uploaded=new DateTime(2014, 1, 1), FileExtension=".png", BinaryData=File.ReadAllBytes(ctx.Server.MapPath("~/Content/Images/Development/profileImage3_medium_5columns.png")) },
                };
                foreach (var document in documents) context.Documents.Add(document);
            }
        }
    }
}